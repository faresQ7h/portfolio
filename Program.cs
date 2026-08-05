using FaresPortfolio.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IPortfolioDataService, JsonPortfolioDataService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseDefaultFiles();
// Serve wwwroot (including wwwroot/assets). Long caching only applies outside Development so local
// edits (HTML/CSS/JS/images) are always reflected on refresh instead of being masked by the browser cache.
app.UseStaticFiles(app.Environment.IsDevelopment()
    ? new StaticFileOptions()
    : new StaticFileOptions
    {
        OnPrepareResponse = context =>
        {
            const int cacheDurationInSeconds = 60 * 60 * 24 * 7;
            context.Context.Response.Headers.CacheControl = $"public,max-age={cacheDurationInSeconds}";
        }
    });

// API to list image files inside a project's folder under wwwroot/assets, so galleries
// stay in sync with the filesystem without hardcoding filenames.
var assetsPath = Path.Combine(app.Environment.WebRootPath, "assets");
app.MapGet("/api/assets/{folder}", (string folder) =>
{
    // sanitize folder name to prevent path traversal
    if (string.IsNullOrWhiteSpace(folder) || folder.IndexOfAny(new[] { '\0', '/', '\\' }) >= 0)
    {
        return Results.BadRequest(new { error = "Invalid folder" });
    }

    var folderPath = Path.Combine(assetsPath, folder);
    if (!Directory.Exists(folderPath)) return Results.NotFound(new { files = Array.Empty<string>() });

    var allowed = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif" };
    var files = Directory.EnumerateFiles(folderPath)
        .Where(f => allowed.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
        .Select(f => "/assets/" + folder + "/" + Path.GetFileName(f))
        .OrderBy(s => s)
        .ToArray();

    return Results.Ok(new { files });
});

app.MapControllers();

app.MapMethods("/projects/{slug}", ["GET", "HEAD"], async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "project.html"));
});

app.MapGet("/error", () => Results.Problem("An unexpected error occurred."));

app.Run();
