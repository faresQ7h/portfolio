using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaresPortfolio.Pages;

// Rendered by UseStatusCodePagesWithReExecute for empty error responses (unknown routes, unknown
// project slugs), so visitors get a styled page while the original status code is preserved.
public sealed class StatusModel : PageModel
{
    public int Code { get; private set; }
    public bool IsNotFound => Code == StatusCodes.Status404NotFound;

    public void OnGet(int code)
    {
        Code = code is >= 400 and <= 599 ? code : StatusCodes.Status404NotFound;
        Response.StatusCode = Code;

        ViewData["Meta"] = new PageMeta
        {
            Title = IsNotFound ? "Page not found | Fares Mohamed" : "Something went wrong | Fares Mohamed",
            Description = "This page could not be found on Fares Mohamed's portfolio."
        };
    }
}
