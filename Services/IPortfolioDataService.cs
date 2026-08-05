using FaresPortfolio.Models;

namespace FaresPortfolio.Services;

public interface IPortfolioDataService
{
    Task<PortfolioProfile> GetProfileAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken);
    Task<Project?> GetProjectBySlugAsync(string slug, CancellationToken cancellationToken);
}
