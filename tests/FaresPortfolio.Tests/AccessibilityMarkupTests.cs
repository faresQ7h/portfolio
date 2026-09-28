using Microsoft.AspNetCore.Mvc.Testing;

namespace FaresPortfolio.Tests;

// Markup-level accessibility guarantees; keyboard behaviour of the menu is checked in a browser.
public sealed class AccessibilityMarkupTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/projects/minishell-42prague")]
    public async Task Script_driven_buttons_are_hidden_until_the_script_runs(string path)
    {
        var html = await client.GetStringAsync(path);

        Assert.Contains("data-nav-toggle hidden>", html);
        Assert.Contains("data-theme-toggle aria-label=\"Switch theme\" hidden>", html);
        // CSS only collapses the mobile menu when this class is present
        Assert.Contains("document.documentElement.classList.add(\"js\");", html);
    }

    [Fact]
    public async Task Hero_profile_details_are_not_a_nested_complementary_landmark()
    {
        var html = await client.GetStringAsync("/");

        Assert.DoesNotContain("<aside", html);
        Assert.Contains("<div class=\"hero-spec\">", html);
    }

    [Fact]
    public async Task Labelled_thumbnail_row_has_a_role_that_allows_a_label()
    {
        var html = await client.GetStringAsync("/projects/business-inventory-management");

        Assert.Contains("class=\"thumb-row\" role=\"group\" aria-label=", html);
    }

    [Fact]
    public async Task Every_image_has_alt_text()
    {
        foreach (var path in new[] { "/", "/projects/minishell-42prague", "/projects/fract-ol-42-project" })
        {
            var html = await client.GetStringAsync(path);
            var images = System.Text.RegularExpressions.Regex.Matches(html, "<img\\b[^>]*>");

            Assert.NotEmpty(images);
            Assert.All(images, image => Assert.Contains(" alt=\"", image.Value));
        }
    }

    [Fact]
    public async Task Contact_rows_have_decorative_icons_backed_by_the_sprite()
    {
        var html = await client.GetStringAsync("/");

        foreach (var icon in new[] { "icon-mail", "icon-github", "icon-linkedin" })
        {
            Assert.Contains($"<svg class=\"contact-icon\" aria-hidden=\"true\" focusable=\"false\"><use href=\"#{icon}\"></use></svg>", html);
            Assert.Contains($"<symbol id=\"{icon}\"", html);
        }
    }
}
