namespace FaresPortfolio.Pages;

// Per-page <head> metadata, set by each page model and rendered by the shared layout.
public sealed record PageMeta
{
    public const string SiteUrl = "https://faresm.dev";

    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string OgType { get; init; } = "website";
    public string? OgImage { get; init; }

    // Site-relative path such as "/" or "/projects/minishell-42prague"; null for pages that shouldn't be indexed.
    public string? CanonicalPath { get; init; }

    public string? CanonicalUrl => CanonicalPath is null ? null : SiteUrl + CanonicalPath;
}
