using FaresPortfolio.Models;
using FaresPortfolio.Services;

namespace FaresPortfolio.Tests;

public sealed class ProjectManifestTests
{
    [Fact]
    public void Manifest_prints_only_the_projects_own_data()
    {
        var project = new Project
        {
            Name = "Sample Tool",
            Technologies = ["Go", "SQLite", "HTTP"],
            SkillsDemonstrated = ["CLI Design", "Testing"],
            Status = "Completed"
        };

        var session = ProjectManifest.For(project);

        Assert.Equal("~/sample-tool", session.Path);
        Assert.Equal(2, session.Lines.Count);
        Assert.Equal(TerminalLine.Command, session.Lines[0].Kind);
        Assert.Equal("cat project", session.Lines[0].Text);
        Assert.Equal(
            "lang    Go\nstack   SQLite · HTTP\nfocus   CLI Design · Testing\nstatus  completed",
            session.Lines[1].Text);
    }

    [Fact]
    public void Manifest_skips_fields_the_project_does_not_have()
    {
        var session = ProjectManifest.For(new Project { Name = "Bare", Technologies = ["C"] });

        Assert.Equal("lang  C", session.Lines[1].Text);
    }
}
