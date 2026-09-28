using System.Text.Json;
using FaresPortfolio.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FaresPortfolio.Services;

// Portfolio content lives in Data/*.json, outside wwwroot, so the raw files are never served as
// static files. Parsed data is cached in memory and evicted when its file changes, so edits to the
// JSON still show up on the next request without a restart.
public sealed class JsonPortfolioDataService(IWebHostEnvironment environment, IMemoryCache cache) : IPortfolioDataService
{
    private const string DataDirectory = "Data";
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
        var relativePath = $"{DataDirectory}/{fileName}";

        var data = await cache.GetOrCreateAsync(relativePath, async entry =>
        {
            entry.AddExpirationToken(environment.ContentRootFileProvider.Watch(relativePath));

            var path = Path.Combine(environment.ContentRootPath, DataDirectory, fileName);
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        });

        return data ?? throw new InvalidOperationException($"Could not read portfolio data from {relativePath}.");
    }
}
