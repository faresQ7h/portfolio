using FaresPortfolio.Models;

namespace FaresPortfolio.Services;

// A terminal card for projects with no screenshot or captured session: `cat project` printing only
// facts already in the project's data (language, stack, focus, status), so nothing is invented.
public static class ProjectManifest
{
    public static TerminalSession For(Project project)
    {
        var fields = new List<(string Key, string Value)>();
        if (project.Technologies.Count > 0) fields.Add(("lang", project.Technologies[0]));
        if (project.Technologies.Count > 1) fields.Add(("stack", string.Join(" · ", project.Technologies.Skip(1))));
        if (project.SkillsDemonstrated.Count > 0) fields.Add(("focus", string.Join(" · ", project.SkillsDemonstrated)));
        if (!string.IsNullOrEmpty(project.Status)) fields.Add(("status", project.Status.ToLowerInvariant()));

        var width = fields.Count == 0 ? 0 : fields.Max(field => field.Key.Length) + 2;
        var output = string.Join("\n", fields.Select(field => field.Key.PadRight(width) + field.Value));

        return new TerminalSession
        {
            Path = "~/" + project.Name.ToLowerInvariant().Replace(' ', '-'),
            Lines =
            [
                new TerminalLine { Kind = TerminalLine.Command, Text = "cat project" },
                new TerminalLine { Kind = TerminalLine.Output, Text = output }
            ]
        };
    }
}
