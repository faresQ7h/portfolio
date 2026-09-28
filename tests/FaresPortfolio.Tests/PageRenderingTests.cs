using System.Net;
using FaresPortfolio.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FaresPortfolio.Tests;

// The pages must arrive fully rendered, so crawlers, link previews and no-JS visitors see real content.
public sealed class PageRenderingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Home_page_html_contains_every_project_skill_and_the_location()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var profile = await data.GetProfileAsync(CancellationToken.None);
        var projects = await data.GetProjectsAsync(CancellationToken.None);

        var html = await GetDecodedHtmlAsync("/");

        Assert.All(projects, project => Assert.Contains(project.Name, html));
        Assert.All(profile.Skills.SelectMany(group => group.Items), skill => Assert.Contains(skill, html));
        Assert.Contains(profile.Personal.Location, html);
    }

    [Fact]
    public async Task Home_page_renders_every_project_card_with_a_detail_link()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var projects = await data.GetProjectsAsync(CancellationToken.None);

        var html = await GetDecodedHtmlAsync("/");

        Assert.All(projects, project => Assert.Contains($"href=\"/projects/{project.Slug}\"", html));
    }

    [Fact]
    public async Task Project_page_is_server_rendered()
    {
        var data = factory.Services.GetRequiredService<IPortfolioDataService>();
        var project = await data.GetProjectBySlugAsync("minishell-42prague", CancellationToken.None);
        Assert.NotNull(project);

        var html = await GetDecodedHtmlAsync("/projects/minishell-42prague");

        Assert.Contains($"<title>{project.Name} | Fares Mohamed</title>", html);
        Assert.Contains(project.Overview, html);
        Assert.Contains(project.KeyFeatures[0], html);
        Assert.Contains("/assets/minishell/", html);
    }

    [Fact]
    public async Task Project_page_slug_lookup_is_case_insensitive()
    {
        var response = await client.GetAsync("/projects/MINISHELL-42PRAGUE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/projects/does-not-exist")]
    [InlineData("/no-such-page")]
    public async Task Unknown_pages_return_a_real_404_with_the_styled_page(string path)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", html);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", html);
    }

    [Theory]
    [InlineData("/index.html")]
    [InlineData("/project.html")]
    public async Task Old_client_rendered_shells_are_gone(string path)
    {
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> GetDecodedHtmlAsync(string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Razor HTML-encodes characters such as '+', '&' and '–', so compare against decoded text.
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
}
