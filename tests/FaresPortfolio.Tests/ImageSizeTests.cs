using System.Buffers.Binary;
using System.Text;
using FaresPortfolio.Services;

namespace FaresPortfolio.Tests;

public sealed class ImageSizeTests
{
    private const int Width = 720;
    private const int Height = 420;

    [Fact]
    public void Reads_png_header()
    {
        var bytes = new byte[32];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(8), 13);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16), Width);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20), Height);

        AssertSize(bytes, ".png");
    }

    [Fact]
    public void Reads_gif_header()
    {
        var bytes = new byte[16];
        "GIF89a"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6), Width);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8), Height);

        AssertSize(bytes, ".gif");
    }

    [Fact]
    public void Reads_jpeg_frame_header_after_other_segments()
    {
        using var jpeg = new MemoryStream();
        jpeg.Write([0xFF, 0xD8]);                                  // SOI
        jpeg.Write([0xFF, 0xE0, 0x00, 0x10]);                      // APP0, 16 bytes incl. length
        jpeg.Write(new byte[14]);
        jpeg.Write([0xFF, 0xDB, 0x00, 0x04, 0x00, 0x00]);          // DQT stub
        jpeg.Write([0xFF, 0xC0, 0x00, 0x11, 0x08]);                // SOF0, precision 8
        var frameSize = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(frameSize, Height);
        BinaryPrimitives.WriteUInt16BigEndian(frameSize.AsSpan(2), Width);
        jpeg.Write(frameSize);
        jpeg.Write(new byte[10]);

        AssertSize(jpeg.ToArray(), ".jpg");
    }

    [Fact]
    public void Reads_webp_extended_header()
    {
        var bytes = WebPHeader("VP8X");
        WriteUInt24(bytes, 24, Width - 1);
        WriteUInt24(bytes, 27, Height - 1);

        AssertSize(bytes, ".webp");
    }

    [Fact]
    public void Reads_webp_lossless_header()
    {
        var bytes = WebPHeader("VP8L");
        bytes[20] = 0x2F;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(21), (uint)((Width - 1) | ((Height - 1) << 14)));

        AssertSize(bytes, ".webp");
    }

    [Fact]
    public void Reads_webp_lossy_header()
    {
        var bytes = WebPHeader("VP8 ");
        bytes[23] = 0x9D;
        bytes[24] = 0x01;
        bytes[25] = 0x2A;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(26), Width);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(28), Height);

        AssertSize(bytes, ".webp");
    }

    [Theory]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 720 420"></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" width="720px" height="420" viewBox="0 0 10 10"></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%" viewBox="0,0,720,420"></svg>""")]
    public void Reads_svg_size_from_attributes_or_viewbox(string svg)
    {
        AssertSize(Encoding.UTF8.GetBytes(svg), ".svg");
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".svg")]
    public void Returns_null_for_unrecognised_content(string extension)
    {
        using var stream = new MemoryStream("not an image"u8.ToArray());

        Assert.Null(ImageSize.Read(stream, extension));
    }

    private static void AssertSize(byte[] bytes, string extension)
    {
        using var stream = new MemoryStream(bytes);

        Assert.Equal(new ImageDimensions(Width, Height), ImageSize.Read(stream, extension));
    }

    private static byte[] WebPHeader(string chunk)
    {
        var bytes = new byte[32];
        "RIFF"u8.CopyTo(bytes);
        "WEBP"u8.CopyTo(bytes.AsSpan(8));
        Encoding.ASCII.GetBytes(chunk).CopyTo(bytes, 12);
        return bytes;
    }

    private static void WriteUInt24(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
    }
}
