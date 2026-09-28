using System.Collections.Concurrent;
using FaresPortfolio.Models;
using Microsoft.Extensions.FileProviders;

namespace FaresPortfolio.Services;

public sealed record ProjectImage(string Url, ImageDimensions? Size)
{
    // Extreme aspect-ratio screenshots (very wide/short or very tall/narrow) crop badly
    // under object-fit: cover, so they are shown whole instead.
    public bool NeedsContainFit => Size is { } size && (double)size.Width / size.Height is > 2.2 or < 0.45;
}

// Resolves project media from wwwroot/assets. A screenshot entry ending in "/" refers to a whole
// folder, so galleries stay in sync with the filesystem without hardcoding filenames.
public sealed class ProjectMediaService(IWebHostEnvironment environment)
{
    public const string PlaceholderImage = "/assets/projects/placeholder.svg";

    private const string AssetsPrefix = "/assets/";
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif"];

    private readonly ConcurrentDictionary<string, (DateTimeOffset Modified, ImageDimensions? Size)> sizes = new();

    // Folder names come from the URL, so reject anything that could escape wwwroot/assets.
    public static bool IsValidFolderName(string? folder)
    {
        return !string.IsNullOrWhiteSpace(folder)
            && folder.IndexOfAny(['\0', '/', '\\']) < 0
            && folder is not "." and not "..";
    }

    // Image URLs inside wwwroot/assets/{folder}, sorted by name; null when the folder doesn't exist.
    public IReadOnlyList<string>? ListAssetFolder(string folder)
    {
        if (!IsValidFolderName(folder)) return null;

        var contents = environment.WebRootFileProvider.GetDirectoryContents($"assets/{folder}");
        if (!contents.Exists) return null;

        return contents
            .Where(file => !file.IsDirectory && AllowedExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
            .Select(file => $"{AssetsPrefix}{folder}/{file.Name}")
            .Order()
            .ToArray();
    }

    // Every screenshot of a project that actually exists on disk, with folder references expanded.
    public IReadOnlyList<ProjectImage> GetScreenshots(Project project)
    {
        var urls = new List<string>();
        foreach (var entry in project.Screenshots)
        {
            if (entry.EndsWith('/'))
            {
                if (entry.StartsWith(AssetsPrefix, StringComparison.Ordinal))
                {
                    urls.AddRange(ListAssetFolder(entry[AssetsPrefix.Length..].TrimEnd('/')) ?? []);
                }
            }
            else
            {
                urls.Add(entry);
            }
        }

        return urls.Select(Describe).OfType<ProjectImage>().ToArray();
    }

    // The image shown on a project's card: its first screenshot, else its illustration, else the placeholder.
    public ProjectImage GetCardImage(Project project)
    {
        return GetScreenshots(project).FirstOrDefault() ?? GetFallbackImage(project);
    }

    public ProjectImage GetFallbackImage(Project project)
    {
        return (string.IsNullOrEmpty(project.FallbackImage) ? null : Describe(project.FallbackImage))
            ?? Describe(PlaceholderImage)
            ?? new ProjectImage(PlaceholderImage, null);
    }

    // Null when the URL doesn't point at an existing file under wwwroot.
    public ProjectImage? Describe(string url)
    {
        var file = environment.WebRootFileProvider.GetFileInfo(url);
        if (!file.Exists || file.IsDirectory) return null;

        return new ProjectImage(url, Measure(file));
    }

    private ImageDimensions? Measure(IFileInfo file)
    {
        if (file.PhysicalPath is null) return null;

        if (sizes.TryGetValue(file.PhysicalPath, out var cached) && cached.Modified == file.LastModified)
        {
            return cached.Size;
        }

        var size = ImageSize.Read(file.PhysicalPath);
        sizes[file.PhysicalPath] = (file.LastModified, size);
        return size;
    }
}
