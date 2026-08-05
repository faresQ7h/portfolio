using System.Text.Json.Serialization;

namespace FaresPortfolio.Models;

public sealed class Project
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> Technologies { get; init; } = [];
    [JsonPropertyName("githubUrl")]
    public string GitHubUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> Screenshots { get; init; } = [];
    public string FallbackImage { get; init; } = string.Empty;
    public IReadOnlyList<string> SkillsDemonstrated { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public bool Featured { get; init; }
    public string Overview { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    public IReadOnlyList<string> KeyFeatures { get; init; } = [];
    public IReadOnlyList<string> Challenges { get; init; } = [];
    public IReadOnlyList<string> EngineeringDecisions { get; init; } = [];
    public string TechnologyNotes { get; init; } = string.Empty;
    public IReadOnlyList<string> LessonsLearned { get; init; } = [];
}
