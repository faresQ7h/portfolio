using System.Net;
using System.Text.Json;
using FaresPortfolio.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FaresPortfolio.Tests;

public sealed class ContentTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] FeaturedOrder =
        ["minishell-42prague", "faresm-dev", "hotel-booking-database", "philosophers-42prague"];

    // Kept in the skills list at the owner's request even though no project on the site uses them.
    private static readonly string[] SkillsWithoutProjectEvidence = ["C++", "Bash"];

    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Profile_api_exposes_no_phone_number()
    {
        using var profile = JsonDocument.Parse(await client.GetStringAsync("/api/profile"));

        Assert.Empty(FindProperties(profile.RootElement, "phone"));
    }

    [Fact]
    public async Task Home_page_has_no_phone_link()
    {
        var html = await client.GetStringAsync("/");

        Assert.DoesNotContain("tel:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Featured_projects_render_first_in_the_agreed_order()
    {
        var html = await client.GetStringAsync("/");
        // Skill "used in" links also point at projects, so only look inside the projects section.
        var start = html.IndexOf("id=\"projects\"", StringComparison.Ordinal);
        var end = html.IndexOf("id=\"education\"", start, StringComparison.Ordinal);
        var section = html[start..end];

        var positions = FeaturedOrder.Select(slug => section.IndexOf($"href=\"/projects/{slug}\"", StringComparison.Ordinal)).ToArray();

        Assert.All(positions, position => Assert.True(position >= 0));
        Assert.Equal(positions.Order().ToArray(), positions);

        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var projects = await data.GetProjectsAsync(CancellationToken.None);
        var firstNonFeatured = projects.Where(project => !project.Featured)
            .Min(project => section.IndexOf($"href=\"/projects/{project.Slug}\"", StringComparison.Ordinal));
        Assert.True(positions[^1] < firstNonFeatured);
    }

    [Fact]
    public async Task Unbuilt_ai_assistant_is_a_note_not_a_project()
    {
        var detail = await client.GetAsync("/projects/ai-job-search-assistant");
        var apiProjects = await client.GetStringAsync("/api/projects");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.DoesNotContain("ai-job-search-assistant", apiProjects);
        Assert.Contains("Currently building", html);
        Assert.Contains("AI Job Search Assistant", html);
    }

    [Fact]
    public async Task Every_skill_except_the_listed_exceptions_is_backed_by_a_project()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var profile = await data.GetProfileAsync(CancellationToken.None);
        var projects = await data.GetProjectsAsync(CancellationToken.None);

        var unbacked = SkillUsage.Build(profile.Skills, projects)
            .SelectMany(group => group.Items)
            .Where(skill => skill.UsedIn.Count == 0)
            .Select(skill => skill.Name)
            .ToArray();

        Assert.Equal(SkillsWithoutProjectEvidence.Order(), unbacked.Order());
    }

    [Fact]
    public async Task Skills_link_to_the_projects_that_use_them()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("used in", html);
        Assert.Contains("<a href=\"/projects/hotel-booking-database\">Hotel Booking Database</a>", html);
    }

    [Fact]
    public async Task Hero_shows_the_plain_sentence_headline_and_contact_ctas()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var profile = await data.GetProfileAsync(CancellationToken.None);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains(profile.Personal.Headline, html);
        Assert.Contains($"href=\"mailto:{profile.Personal.Email}\"", html);
        Assert.DoesNotContain(" | ", profile.Personal.Headline);
    }

    [Fact]
    public async Task Cv_button_is_hidden_while_the_cv_file_is_missing()
    {
        var html = await client.GetStringAsync("/");

        Assert.DoesNotContain("Download CV", html);
        Assert.Contains("Resume available upon request", html);
    }

    [Fact]
    public async Task Cv_button_appears_once_the_cv_file_exists()
    {
        var webRoot = Directory.CreateTempSubdirectory("portfolio-webroot-");
        try
        {
            Directory.CreateDirectory(Path.Combine(webRoot.FullName, "assets"));
            await File.WriteAllTextAsync(Path.Combine(webRoot.FullName, "assets", "Fares_Mohamed_CV.pdf"), "%PDF-1.4");

            using var withCv = factory.WithWebHostBuilder(builder => builder.UseWebRoot(webRoot.FullName));
            var html = await withCv.CreateClient().GetStringAsync("/");

            Assert.Contains("href=\"/assets/Fares_Mohamed_CV.pdf\" download>Download CV</a>", html);
            Assert.DoesNotContain("Resume available upon request", html);
        }
        finally
        {
            webRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Stats_explain_the_grading_scale_and_drop_the_repo_count()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("average (CZU scale, 1.0 = best)", html);
        Assert.DoesNotContain("GitHub repositories", html);
    }

    [Fact]
    public async Task Experience_is_folded_into_education()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.DoesNotContain("Applied practice", html);
        Assert.Contains("professor recommendation", html);
        Assert.Contains("including a Unix shell", html);
    }

    [Fact]
    public async Task Portfolio_project_page_shows_the_deployment_diagram()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/projects/faresm-dev"));

        Assert.Contains("<title id=\"pipeline-title\">Deployment pipeline for faresm.dev</title>", html);
        Assert.Contains("OIDC", html);
        Assert.Contains("https://github.com/faresQ7h/portfolio", html);
    }

    private static IEnumerable<JsonProperty> FindProperties(JsonElement element, string name)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .SelectMany(property => (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) ? [property] : Array.Empty<JsonProperty>())
                    .Concat(FindProperties(property.Value, name))),
            JsonValueKind.Array => element.EnumerateArray().SelectMany(item => FindProperties(item, name)),
            _ => []
        };
    }
}
