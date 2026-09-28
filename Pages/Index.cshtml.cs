using FaresPortfolio.Models;
using FaresPortfolio.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaresPortfolio.Pages;

public sealed class IndexModel(IPortfolioDataService portfolio, ProjectMediaService media) : PageModel
{
    public const string CvRequestSubject = "CV request via faresm.dev";

    public PortfolioProfile Profile { get; private set; } = new();
    public IReadOnlyList<ProjectCardModel> FeaturedProjects { get; private set; } = [];
    public IReadOnlyList<ProjectCardModel> OtherProjects { get; private set; } = [];
    public IReadOnlyList<SkillUsageGroup> Skills { get; private set; } = [];
    public IReadOnlyList<TimelineEntry> Education { get; private set; } = [];

    public string EmailHref => $"mailto:{Profile.Personal.Email}";
    public string RequestCvHref => $"mailto:{Profile.Personal.Email}?subject={Uri.EscapeDataString(CvRequestSubject)}";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Profile = await portfolio.GetProfileAsync(cancellationToken);
        var projects = await portfolio.GetProjectsAsync(cancellationToken);

        // Featured projects first; each group keeps its order from projects.json.
        var cards = projects.Select(project => new ProjectCardModel(project, media.GetPrimaryMedia(project))).ToArray();
        FeaturedProjects = cards.Where(card => card.Project.Featured).ToArray();
        OtherProjects = cards.Where(card => !card.Project.Featured).ToArray();

        Skills = SkillUsage.Build(Profile.Skills, [.. FeaturedProjects.Concat(OtherProjects).Select(card => card.Project)]);
        Education = Profile.Education
            .Select(item => new TimelineEntry(item.Period, item.School, item.Program, item.Details, item.Highlights))
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
