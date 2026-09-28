using FaresPortfolio.Models;
using FaresPortfolio.Services;

namespace FaresPortfolio.Pages;

public sealed record ProjectCardModel(Project Project, ProjectMedia Media);

public sealed record TimelineEntry(string Period, string Title, string Subtitle, string? Text, IReadOnlyList<string> Bullets);

public sealed record DiagramModel(string Name, bool Decorative);

// Compact terminals (project cards) show a fixed-height preview; the project page shows the whole session.
public sealed record TerminalModel(TerminalSession Session, bool Compact, bool ShowCaption = false);

public sealed record GitHubButtonModel(string Href, string CssClass, string Label, string? AccessibleLabel = null);

// One "Overview" / "Key Features" style block on a project detail page: either a paragraph or a list.
public sealed record DetailBlock(string Title, string? Text, IReadOnlyList<string> Items);
