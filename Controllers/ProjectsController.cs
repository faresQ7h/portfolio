using FaresPortfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaresPortfolio.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(IPortfolioDataService portfolioDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var projects = await portfolioDataService.GetProjectsAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var project = await portfolioDataService.GetProjectBySlugAsync(slug, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }
}
