using Vers.Core;

namespace Vers.Core.Tests;

public sealed class ToolResolverTests
{
    [Fact]
    public void Resolve_UsesLongestMatchingProjectDirectory()
    {
        var settings = CreateSettings();
        settings.Groups["php"].Projects["/work"] = "php82";
        settings.Groups["php"].Projects["/work/api"] = "php84";

        var result = ToolResolver.Resolve(settings, "php", "/work/api/src");

        Assert.Equal("php84", result.VersionName);
    }

    [Fact]
    public void Resolve_DoesNotMatchDirectoryNamePrefix()
    {
        var settings = CreateSettings();
        settings.Groups["php"].Projects["/work/app"] = "php82";

        var result = ToolResolver.Resolve(settings, "php", "/work/application");

        Assert.Equal("php84", result.VersionName);
    }

    [Fact]
    public void Resolve_EnvironmentOverrideHasHighestPriority()
    {
        var settings = CreateSettings();
        settings.Groups["php"].Projects["/work"] = "php82";

        var result = ToolResolver.Resolve(
            settings,
            "php",
            "/work",
            name => name == "VER_PHP_VERSION" ? "php84" : null);

        Assert.Equal("php84", result.VersionName);
    }

    [Fact]
    public void Resolve_ReturnsConfiguredVersionEnvironment()
    {
        var settings = CreateSettings();
        settings.Groups["php"].Versions["php84"].Environment["COMPOSER_HOME"] = "/cache/composer";

        var result = ToolResolver.Resolve(settings, "php", "/other");

        Assert.Equal("/cache/composer", result.Environment["COMPOSER_HOME"]);
    }

    private static Settings CreateSettings() => new()
    {
        Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
        {
            ["php"] = new()
            {
                DefaultVersion = "php84",
                Versions = new Dictionary<string, ToolVersion>(StringComparer.OrdinalIgnoreCase)
                {
                    ["php82"] = new() { Executable = "/tools/php82" },
                    ["php84"] = new() { Executable = "/tools/php84" }
                }
            }
        }
    };
}
