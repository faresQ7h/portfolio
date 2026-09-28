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

public enum ProjectMediaKind
{
    Diagram,
    Terminal,
    Image
}

// What represents a project visually: an inline diagram, a terminal session, or a screenshot.
public sealed record ProjectMedia(ProjectMediaKind Kind, string? Diagram = null, TerminalSession? Terminal = null, ProjectImage? Image = null);

// Resolves project media from wwwroot/assets. A screenshot entry ending in "/" refers to a whole
// folder, so galleries stay in sync with the filesystem without hardcoding filenames.
public sealed class ProjectMediaService(IWebHostEnvironment environment)
{
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

    // The media shown for a project: its diagram, else its real terminal session, else its first
    // screenshot, else a short manifest generated from the project's own data.
    public ProjectMedia GetPrimaryMedia(Project project)
    {
        if (!string.IsNullOrEmpty(project.Diagram)) return new ProjectMedia(ProjectMediaKind.Diagram, Diagram: project.Diagram);
        if (project.Terminal is { Lines.Count: > 0 } terminal) return new ProjectMedia(ProjectMediaKind.Terminal, Terminal: terminal);

        var screenshot = GetScreenshots(project).FirstOrDefault();
        return screenshot is not null
            ? new ProjectMedia(ProjectMediaKind.Image, Image: screenshot)
            : new ProjectMedia(ProjectMediaKind.Terminal, Terminal: ProjectManifest.For(project));
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
