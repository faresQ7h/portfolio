using FaresPortfolio.Models;
using FaresPortfolio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaresPortfolio.Pages.Projects;

public sealed class DetailModel(IPortfolioDataService portfolio, ProjectMediaService media) : PageModel
{
    public Project Project { get; private set; } = new();
    public IReadOnlyList<ProjectImage> Screenshots { get; private set; } = [];
    public IReadOnlyList<DetailBlock> Sections { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        var project = await portfolio.GetProjectBySlugAsync(slug, cancellationToken);
        if (project is null) return NotFound();

        Project = project;
        var screenshots = media.GetScreenshots(project);
        Screenshots = screenshots.Count > 0 ? screenshots : [media.GetFallbackImage(project)];
        Sections = BuildSections(project);

        ViewData["Meta"] = new PageMeta
        {
            Title = $"{project.Name} | Fares Mohamed",
            Description = project.Description,
            OgDescription = "Architecture, technologies, challenges, and lessons learned from a software project by Fares Mohamed.",
            OgType = "article",
            CanonicalPath = $"/projects/{project.Slug}"
        };

        return Page();
    }

    // Only the sections that actually have content, so a project with a short writeup doesn't
    // render empty "Key Features" / "Lessons Learned" blocks.
    private static IReadOnlyList<DetailBlock> BuildSections(Project project)
    {
        DetailBlock[] blocks =
        [
            new("Overview", project.Overview, []),
            new("Architecture", project.Architecture, []),
            new("Key Features", null, project.KeyFeatures),
            new("Technical Challenges", null, project.Challenges),
            new("Engineering Decisions", null, project.EngineeringDecisions),
            new("Technologies Used", project.TechnologyNotes, []),
            new("Lessons Learned", null, project.LessonsLearned)
        ];

        return blocks.Where(block => block.Items.Count > 0 || !string.IsNullOrWhiteSpace(block.Text)).ToArray();
    }
}
