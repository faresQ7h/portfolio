using FaresPortfolio.Models;

namespace FaresPortfolio.Services;

public sealed record ProjectLink(string Slug, string Name);

public sealed record SkillUsageItem(string Name, IReadOnlyList<ProjectLink> UsedIn);

public sealed record SkillUsageGroup(string Name, string Icon, IReadOnlyList<SkillUsageItem> Items);

// Derives "used in: <project>" for each skill by matching the skill name against the projects'
// technology lists, so the skills section only claims what a project on the site backs up.
public static class SkillUsage
{
    public const int MaxProjectsPerSkill = 2;

    // Projects are expected in display order (featured first), so the most prominent evidence wins.
    public static IReadOnlyList<SkillUsageGroup> Build(IEnumerable<SkillCategory> skills, IReadOnlyList<Project> projects)
    {
        return skills
            .Select(group => new SkillUsageGroup(
                group.Name,
                group.Icon,
                group.Items.Select(skill => new SkillUsageItem(skill, FindProjects(skill, projects))).ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<ProjectLink> FindProjects(string skill, IReadOnlyList<Project> projects)
    {
        return projects
            .Where(project => project.Technologies.Contains(skill, StringComparer.OrdinalIgnoreCase))
            .Take(MaxProjectsPerSkill)
            .Select(project => new ProjectLink(project.Slug, project.Name))
            .ToArray();
    }
}
