namespace FaresPortfolio.Services;

// Maps a skill/tag label to its icon. Only technologies with a real, recognizable brand mark get an
// icon; concepts stay text-only.
public static class TechIcons
{
    // Symbol ids in the inline sprite (Pages/Shared/_IconSprite.cshtml).
    private static readonly Dictionary<string, string> SpriteSymbols = new(StringComparer.Ordinal)
    {
        ["C"] = "icon-c",
        ["C++"] = "icon-cplusplus",
        ["C#"] = "icon-csharp",
        ["Python"] = "icon-python",
        ["SQL"] = "icon-sql",
        ["JavaScript"] = "icon-javascript",
        ["Bash"] = "icon-bash",
        [".NET"] = "icon-dotnet",
        ["ASP.NET Core"] = "icon-dotnet",
        ["PostgreSQL"] = "icon-postgresql",
        ["Linux"] = "icon-linux",
        ["Git"] = "icon-git",
        ["GitHub Actions"] = "icon-github"
    };

    // Brand marks kept as standalone asset files (full official artwork, e.g. gradients) instead of
    // the sprite, so they render pixel-for-pixel like the source logo.
    private static readonly Dictionary<string, string> AssetFiles = new(StringComparer.Ordinal)
    {
        ["Azure App Service"] = "/assets/icons/azureLogo.svg"
    };

    private static readonly Dictionary<string, string> LearningSymbols = new(StringComparer.Ordinal)
    {
        ["Docker"] = "icon-docker",
        ["Cisco CCNA"] = "icon-cisco",
        ["Cloud Computing"] = "icon-cloud"
    };

    public static string? SymbolFor(string label) => SpriteSymbols.GetValueOrDefault(label);

    public static string? AssetFor(string label) => AssetFiles.GetValueOrDefault(label);

    public static string? LearningSymbolFor(string name) => LearningSymbols.GetValueOrDefault(name);
}
