using FaresPortfolio.Models;
using FaresPortfolio.Services;

namespace FaresPortfolio.Pages;

public sealed record ProjectCardModel(Project Project, ProjectImage Image);

public sealed record TimelineEntry(string Period, string Title, string Subtitle, string? Text, IReadOnlyList<string> Bullets);

public sealed record GitHubButtonModel(string Href, string CssClass, string Label, string? AccessibleLabel = null);

// One "Overview" / "Key Features" style block on a project detail page: either a paragraph or a list.
public sealed record DetailBlock(string Title, string? Text, IReadOnlyList<string> Items);
