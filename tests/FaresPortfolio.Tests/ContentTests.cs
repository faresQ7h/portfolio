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
        ["minishell-42prague", "faresm-dev", "ai-job-search-assistant", "hotel-booking-database"];

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
    public async Task Ai_job_search_assistant_is_a_completed_personal_project()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var project = await data.GetProjectBySlugAsync("ai-job-search-assistant", CancellationToken.None);
        var home = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.NotNull(project);
        Assert.True(project.Featured);
        Assert.Equal("Completed", project.Status);
        Assert.Contains("Docker", project.Technologies);
        Assert.DoesNotContain("Currently building", home);
    }

    [Fact]
    public async Task Projects_without_a_repo_render_no_github_button()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var projects = await data.GetProjectsAsync(CancellationToken.None);
        var withoutRepo = projects.Where(project => string.IsNullOrEmpty(project.GitHubUrl)).ToArray();
        var home = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.NotEmpty(withoutRepo);
        foreach (var project in withoutRepo)
        {
            Assert.DoesNotContain($"aria-label=\"View {project.Name} on GitHub\"", home);

            var detail = await client.GetStringAsync($"/projects/{project.Slug}");
            Assert.DoesNotContain("View GitHub", detail);
            Assert.DoesNotContain("github.com", detail);
        }

        // ...while projects that do have a repo still get their button.
        Assert.Contains("aria-label=\"View Minishell on GitHub\"", home);
    }

    [Fact]
    public async Task Detail_page_hides_sections_that_have_no_content()
    {
        var html = await client.GetStringAsync("/projects/ai-job-search-assistant");

        Assert.Contains("<h2>Overview</h2>", html);
        Assert.Contains("<h2>Architecture</h2>", html);
        Assert.Contains("<h2>Key Features</h2>", html);
        Assert.DoesNotContain("<h2>Technical Challenges</h2>", html);
        Assert.DoesNotContain("<h2>Engineering Decisions</h2>", html);
        Assert.DoesNotContain("<h2>Lessons Learned</h2>", html);
    }

    [Fact]
    public async Task Docker_is_shown_as_a_demonstrated_skill_not_just_learning()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        var skillsStart = html.IndexOf("id=\"skills\"", StringComparison.Ordinal);
        var skills = html[skillsStart..html.IndexOf("id=\"projects\"", skillsStart, StringComparison.Ordinal)];

        Assert.Contains("<a href=\"/projects/ai-job-search-assistant\">AI Job Search Assistant</a>", skills);
        Assert.Contains("Deepening existing Docker skills", html);
        Assert.DoesNotContain("AI Workflow Automation", html);
    }

    [Fact]
    public async Task About_lists_spoken_languages()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("Arabic (native)", html);
        Assert.Contains("English (B2+, professional working proficiency)", html);
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

        // the pitch keeps "CI/CD-deployed" on one line, so compare with that span removed
        Assert.Contains(profile.Personal.Headline, html.Replace("<span class=\"nowrap\">", "").Replace("</span>", ""));
        Assert.Contains($"href=\"mailto:{profile.Personal.Email}\"", html);
        Assert.DoesNotContain(" | ", profile.Personal.Headline);
    }

    [Fact]
    public async Task Request_cv_button_opens_an_email_with_the_agreed_subject()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var profile = await data.GetProfileAsync(CancellationToken.None);
        var html = await client.GetStringAsync("/");
        var api = await client.GetStringAsync("/api/profile");

        Assert.Contains($"href=\"mailto:{profile.Personal.Email}?subject=CV%20request%20via%20faresm.dev\">Request CV</a>", html);
        Assert.DoesNotContain("Download CV", html);
        Assert.DoesNotContain("cvUrl", api, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stats_show_final_year_average_scale_and_graduation()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        var start = html.IndexOf("<dl class=\"stats\">", StringComparison.Ordinal);
        var stats = html[start..html.IndexOf("</dl>", start, StringComparison.Ordinal)];

        Assert.Contains("Final year", stats);
        Assert.Contains("BSc Informatics, CZU", stats);
        Assert.Contains("average (CZU scale, 1.0 = best)", stats);
        Assert.Contains("2027", stats);
        Assert.DoesNotContain("ECTS", stats);
        Assert.DoesNotContain("GitHub repositories", html);
    }

    [Fact]
    public async Task Experience_is_folded_into_education()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.DoesNotContain("Applied practice", html);
        Assert.Contains("3rd and final year", html);
        Assert.DoesNotContain("Entering my 3rd year", html);
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

    [Fact]
    public async Task Compact_cards_show_real_sessions_or_a_manifest_from_project_data()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        // captured by building and running the public repos
        Assert.Contains("./push_swap 5 1 4 2 3", html);
        Assert.Contains("./philo 1 800 200 200", html);
        Assert.Contains("800 1 died", html);
        Assert.Contains("python3 expense_traker.py", html);
        Assert.Contains("python3 miniGPT.py", html);
        // the old placeholder illustrations and the stock philosophers image are gone
        Assert.DoesNotContain("/assets/projects/", html);
        Assert.DoesNotContain("philosophersSync.png", html);
    }

    [Fact]
    public async Task Project_page_shows_the_full_session_with_its_source()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/projects/push-swap-42-project"));

        var raw = await client.GetStringAsync("/projects/push-swap-42-project");

        Assert.Contains("./push_swap $(shuf -i 1-100000 -n 100) | wc -l", html);
        Assert.Contains("<span class=\"term-out\">570</span>", raw);
        Assert.Contains("Real output from building and running the public repo.", html);
    }

    [Fact]
    public async Task Terminal_text_is_html_encoded()
    {
        var html = await client.GetStringAsync("/projects/minishell-42prague");

        Assert.Contains("cat &lt;&lt; EOF", html);
        Assert.Contains("&gt; file.txt", html);
    }

    [Fact]
    public async Task Theme_toggle_is_hidden_until_javascript_enables_it()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("data-theme-toggle aria-label=\"Switch theme\" hidden>", html);
        Assert.Contains("/fonts/inter-latin.woff2", html);
    }

    [Fact]
    public async Task Expense_excerpt_shows_every_step_that_changes_the_balance()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/projects/expense-tracker-python"));

        Assert.Contains("==Current wallet balance:  0 $ ==", html);
        Assert.Contains("Amount of 500 $ was added to the history", html);
        Assert.Contains("==Current wallet balance:  380 $ ==", html);
        Assert.Contains("the menu printed between steps, blank lines, and the final exit are left out", html);
    }

    [Fact]
    public async Task Minishell_session_includes_the_rest_of_the_screenshot()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/projects/minishell-42prague"));

        Assert.Contains("minishell: ff: command not found", html);
        Assert.Contains("^C", html);
    }

    [Fact]
    public async Task Project_pages_do_not_ship_the_unused_icon_sprite()
    {
        var html = await client.GetStringAsync("/projects/minishell-42prague");

        Assert.DoesNotContain("<symbol id=\"icon-", html);
    }

    [Fact]
    public async Task Hero_keeps_slashed_words_on_one_line()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("<span class=\"nowrap\">CI/CD-deployed</span>", html);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/?bg=dots")]
    public async Task Hero_has_the_single_grid_background_and_no_variant_switch(string path)
    {
        var html = await client.GetStringAsync(path);

        Assert.Contains("aria-labelledby=\"hero-name\" data-pointer-glow>", html);
        Assert.DoesNotContain("data-hero-bg", html);
    }

    [Fact]
    public async Task No_invented_contact_or_skills_copy()
    {
        var html = await client.GetStringAsync("/");

        Assert.DoesNotContain("quickest way to reach me", html);
        Assert.DoesNotContain("Each skill links to", html);
    }
}
