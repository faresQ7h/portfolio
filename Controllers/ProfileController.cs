using FaresPortfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaresPortfolio.Controllers;

[ApiController]
[Route("api/profile")]
public sealed class ProfileController(IPortfolioDataService portfolioDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var profile = await portfolioDataService.GetProfileAsync(cancellationToken);
        return Ok(profile);
    }
}
