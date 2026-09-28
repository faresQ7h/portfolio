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
    // Name of an inline SVG diagram shown instead of screenshots (see Pages/Shared/_ProjectDiagram.cshtml).
    public string Diagram { get; init; } = string.Empty;
    // A real terminal session from the project (captured by running it, or typed out from its screenshot).
    public TerminalSession? Terminal { get; init; }
    public IReadOnlyList<string> Screenshots { get; init; } = [];
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

public sealed class TerminalSession
{
    public string Path { get; init; } = "~";
    public string Prompt { get; init; } = "$";
    // Where the session came from, shown under it on the project page.
    public string Caption { get; init; } = string.Empty;
    public IReadOnlyList<TerminalLine> Lines { get; init; } = [];
}

public sealed class TerminalLine
{
    public const string Command = "command";
    public const string Output = "output";
    public const string Error = "error";
    public const string Input = "input";
    public const string Note = "note";

    // command | output | error | input (a program's prompt followed by what was typed) | note
    public string Kind { get; init; } = Output;
    public string Text { get; init; } = string.Empty;
    // For "input" lines: what was typed after the prompt in Text.
    public string Value { get; init; } = string.Empty;
}
