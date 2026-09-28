using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FaresPortfolio.Tests;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/api/profile")]
    [InlineData("/api/projects")]
    [InlineData("/api/projects/minishell-42prague")]
    public async Task Json_endpoints_keep_working(string path)
    {
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_project_slug_returns_404_from_the_api()
    {
        var response = await client.GetAsync("/api/projects/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/profile.json")]
    [InlineData("/api/projects.json")]
    public async Task Raw_data_files_are_not_served_statically(string path)
    {
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Asset_folder_listing_returns_sorted_image_urls()
    {
        var body = await client.GetFromJsonAsync<JsonElement>("/api/assets/minishell");
        var files = body.GetProperty("files").EnumerateArray().Select(file => file.GetString()!).ToArray();

        Assert.NotEmpty(files);
        Assert.All(files, file => Assert.StartsWith("/assets/minishell/", file));
        Assert.Equal(files.Order().ToArray(), files);
    }

    [Fact]
    public async Task Asset_folder_listing_returns_404_for_missing_folders()
    {
        var response = await client.GetAsync("/api/assets/no-such-folder");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("a%5Cb")]
    [InlineData("..")]
    public async Task Asset_folder_listing_rejects_path_tricks(string folder)
    {
        // Built by hand so the client doesn't normalise ".." out of the path before sending it.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/assets/{folder}", UriKind.Relative));
        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"Expected 400 or 404 for '{folder}', got {(int)response.StatusCode}.");
        Assert.DoesNotContain("/assets/..", await response.Content.ReadAsStringAsync());
    }
}
