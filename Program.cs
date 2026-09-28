using FaresPortfolio.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPortfolioDataService, JsonPortfolioDataService>();
builder.Services.AddSingleton<ProjectMediaService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Empty 4xx/5xx responses (unknown routes, unknown project slugs) render the styled status page
// while keeping their original status code, so crawlers see a real 404 instead of a soft one.
app.UseStatusCodePagesWithReExecute("/status/{0}");

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
app.MapGet("/api/assets/{folder}", (string folder, ProjectMediaService media) =>
{
    if (!ProjectMediaService.IsValidFolderName(folder))
    {
        return Results.BadRequest(new { error = "Invalid folder" });
    }

    var files = media.ListAssetFolder(folder);
    return files is null
        ? Results.NotFound(new { files = Array.Empty<string>() })
        : Results.Ok(new { files });
});

app.MapControllers();
app.MapRazorPages();

app.MapGet("/error", () => Results.Problem("An unexpected error occurred."));

app.Run();
