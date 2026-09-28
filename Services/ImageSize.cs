using System.Buffers.Binary;
using System.Globalization;
using System.Xml;

namespace FaresPortfolio.Services;

public readonly record struct ImageDimensions(int Width, int Height);

// Reads pixel dimensions straight from an image file's header (PNG, JPEG, GIF, WebP) or an SVG's
// root attributes, so server-rendered <img> tags can carry explicit width/height and avoid layout
// shift, without pulling in an imaging library.
public static class ImageSize
{
    private const int RasterHeaderLength = 30;

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ImageDimensions? Read(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Read(stream, Path.GetExtension(path));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static ImageDimensions? Read(Stream stream, string extension)
    {
        return string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase)
            ? ReadSvg(stream)
            : ReadRaster(stream);
    }

    private static ImageDimensions? ReadRaster(Stream stream)
    {
        var buffer = new byte[RasterHeaderLength];
        var length = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        var header = buffer.AsSpan(0, length);

        if (header.Length >= 24 && header[..8].SequenceEqual(PngSignature) && header[12..16].SequenceEqual("IHDR"u8))
        {
            return Positive(
                BinaryPrimitives.ReadInt32BigEndian(header[16..20]),
                BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
        }

        if (header.Length >= 10 && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)))
        {
            return Positive(
                BinaryPrimitives.ReadUInt16LittleEndian(header[6..8]),
                BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]));
        }

        if (header.Length >= RasterHeaderLength && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return ReadWebP(header);
        }

        if (header.Length >= 2 && header[0] == 0xFF && header[1] == 0xD8)
        {
            return ReadJpeg(stream);
        }

        return null;
    }

    private static ImageDimensions? ReadWebP(ReadOnlySpan<byte> header)
    {
        var chunk = header[12..16];

        if (chunk.SequenceEqual("VP8X"u8))
        {
            // Extended format: 24-bit little-endian canvas width-1 / height-1.
            var width = header[24] | header[25] << 8 | header[26] << 16;
            var height = header[27] | header[28] << 8 | header[29] << 16;
            return Positive(width + 1, height + 1);
        }

        if (chunk.SequenceEqual("VP8L"u8) && header[20] == 0x2F)
        {
            // Lossless format: 14-bit width-1 and height-1 packed after the signature byte.
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(header[21..25]);
            return Positive((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
        }

        if (chunk.SequenceEqual("VP8 "u8) && header[23] == 0x9D && header[24] == 0x01 && header[25] == 0x2A)
        {
            // Lossy format: 14-bit dimensions right after the key-frame start code.
            return Positive(
                BinaryPrimitives.ReadUInt16LittleEndian(header[26..28]) & 0x3FFF,
                BinaryPrimitives.ReadUInt16LittleEndian(header[28..30]) & 0x3FFF);
        }

        return null;
    }

    private static ImageDimensions? ReadJpeg(Stream stream)
    {
        if (!stream.CanSeek) return null;
        stream.Position = 2;

        Span<byte> segment = stackalloc byte[7];
        while (true)
        {
            var marker = NextMarker(stream);
            if (marker is null or 0xD9 or 0xDA) return null; // end of image / start of scan: no frame header found

            // Standalone markers carry no length field.
            if (marker is 0x01 or >= 0xD0 and <= 0xD7) continue;

            if (stream.ReadAtLeast(segment[..2], 2, throwOnEndOfStream: false) < 2) return null;
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(segment[..2]);
            if (segmentLength < 2) return null;

            // SOF0-SOF15 hold the frame size, except DHT (C4), JPG (C8) and DAC (CC).
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                if (stream.ReadAtLeast(segment[..5], 5, throwOnEndOfStream: false) < 5) return null;
                return Positive(
                    BinaryPrimitives.ReadUInt16BigEndian(segment[3..5]),
                    BinaryPrimitives.ReadUInt16BigEndian(segment[1..3]));
            }

            stream.Seek(segmentLength - 2, SeekOrigin.Current);
        }
    }

    private static int? NextMarker(Stream stream)
    {
        var value = stream.ReadByte();
        if (value != 0xFF) return null;

        // Any number of 0xFF fill bytes may precede the marker code.
        while (value == 0xFF) value = stream.ReadByte();
        return value < 0 ? null : value;
    }

    private static ImageDimensions? ReadSvg(Stream stream)
    {
        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            if (reader.MoveToContent() != XmlNodeType.Element) return null;

            var width = ParseLength(reader.GetAttribute("width"));
            var height = ParseLength(reader.GetAttribute("height"));
            if (width is not null && height is not null) return Positive(width.Value, height.Value);

            var viewBox = reader.GetAttribute("viewBox")?.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            if (viewBox is { Length: 4 }
                && double.TryParse(viewBox[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var viewWidth)
                && double.TryParse(viewBox[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var viewHeight))
            {
                return Positive((int)Math.Round(viewWidth), (int)Math.Round(viewHeight));
            }

            return null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    // Only unitless or px lengths are real pixel sizes; percentages and other units fall back to the viewBox.
    private static int? ParseLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed[..^2];

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? (int)Math.Round(number)
            : null;
    }

    private static ImageDimensions? Positive(int width, int height)
    {
        return width > 0 && height > 0 ? new ImageDimensions(width, height) : null;
    }
}
