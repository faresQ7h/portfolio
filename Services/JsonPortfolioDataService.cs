using System.Text.Json;
using FaresPortfolio.Models;

namespace FaresPortfolio.Services;

public sealed class JsonPortfolioDataService(IWebHostEnvironment environment) : IPortfolioDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<PortfolioProfile> GetProfileAsync(CancellationToken cancellationToken)
    {
        return ReadJsonAsync<PortfolioProfile>("profile.json", cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        return await ReadJsonAsync<List<Project>>("projects.json", cancellationToken);
    }

    public async Task<Project?> GetProjectBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var projects = await GetProjectsAsync(cancellationToken);
        return projects.FirstOrDefault(project =>
            string.Equals(project.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<T> ReadJsonAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(environment.WebRootPath, "api", fileName);

        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

        return data ?? throw new InvalidOperationException($"Could not read portfolio data from {path}.");
    }
}
