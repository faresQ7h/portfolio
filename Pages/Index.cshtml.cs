using FaresPortfolio.Models;
using FaresPortfolio.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaresPortfolio.Pages;

public sealed class IndexModel(IPortfolioDataService portfolio, ProjectMediaService media) : PageModel
{
    public PortfolioProfile Profile { get; private set; } = new();
    public IReadOnlyList<ProjectCardModel> Projects { get; private set; } = [];
    public IReadOnlyList<TimelineEntry> Education { get; private set; } = [];
    public IReadOnlyList<TimelineEntry> Experience { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Profile = await portfolio.GetProfileAsync(cancellationToken);
        var projects = await portfolio.GetProjectsAsync(cancellationToken);

        // Featured projects first; each group keeps its order from projects.json (OrderBy is stable).
        Projects = projects
            .OrderByDescending(project => project.Featured)
            .Select(project => new ProjectCardModel(project, media.GetCardImage(project)))
            .ToArray();

        Education = Profile.Education
            .Select(item => new TimelineEntry(item.Period, item.School, item.Program, item.Details, item.Highlights))
            .ToArray();
        Experience = Profile.Experience
            .Select(item => new TimelineEntry(item.Period, item.Title, item.Organization, null, item.Details))
            .ToArray();

        ViewData["IsHome"] = true;
        ViewData["Meta"] = new PageMeta
        {
            Title = "Fares Mohamed | Backend & Software Development",
            Description = "Portfolio of Fares Mohamed, an Informatics student focused on backend development, systems programming, databases, and full-stack software projects.",
            OgTitle = "Fares Mohamed | Software Engineering Portfolio",
            OgDescription = "Backend, systems programming, database, and web development projects by Fares Mohamed.",
            OgImage = "/assets/myProfile.jpeg",
            CanonicalPath = "/"
        };
    }
}
