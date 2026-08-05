using System.Text.Json.Serialization;

namespace FaresPortfolio.Models;

public sealed class PortfolioProfile
{
    public PersonalInfo Personal { get; init; } = new();
    public IReadOnlyList<StatItem> Stats { get; init; } = [];
    public IReadOnlyList<string> About { get; init; } = [];
    public IReadOnlyList<EducationItem> Education { get; init; } = [];
    public IReadOnlyList<ExperienceItem> Experience { get; init; } = [];
    public IReadOnlyList<LearningItem> CurrentlyLearning { get; init; } = [];
    public IReadOnlyList<SkillCategory> Skills { get; init; } = [];
}

public sealed class PersonalInfo
{
    public string Name { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    [JsonPropertyName("githubUrl")]
    public string GitHubUrl { get; init; } = string.Empty;

    [JsonPropertyName("linkedInUrl")]
    public string LinkedInUrl { get; init; } = string.Empty;
    public string CvUrl { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class StatItem
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed class EducationItem
{
    public string School { get; init; } = string.Empty;
    public string Program { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public IReadOnlyList<string> Highlights { get; init; } = [];
}

public sealed class ExperienceItem
{
    public string Title { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
    public IReadOnlyList<string> Details { get; init; } = [];
}

public sealed class LearningItem
{
    public string Name { get; init; } = string.Empty;
    public string Focus { get; init; } = string.Empty;
}

public sealed class SkillCategory
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public IReadOnlyList<string> Items { get; init; } = [];
}
