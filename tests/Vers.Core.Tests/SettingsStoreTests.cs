using Vers.Core;

namespace Vers.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"vers-tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsSettingsAndRestoresCaseInsensitiveKeys()
    {
        var path = Path.Combine(_directory, "data", "settings.json");
        var settings = new Settings
        {
            UiCulture = "zh-CN",
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["PHP"] = new()
                {
                    DefaultVersion = "php84",
                    Versions = new Dictionary<string, ToolVersion>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["php84"] = new()
                        {
                            Executable = "/tools/php",
                            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["COMPOSER_HOME"] = "/cache/composer"
                            }
                        }
                    }
                }
            }
        };

        SettingsStore.Save(path, settings);
        var loaded = SettingsStore.Load(path);

        Assert.Equal("zh-CN", loaded.UiCulture);
        Assert.True(loaded.Groups.ContainsKey("php"));
        Assert.True(loaded.Groups["php"].Versions["PHP84"].Environment.ContainsKey("composer_home"));
    }

    [Fact]
    public void Validate_RejectsProjectMappingToMissingVersion()
    {
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = new()
                {
                    Projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["/work/app"] = "missing"
                    }
                }
            }
        };

        var exception = Assert.Throws<InvalidDataException>(() => SettingsValidator.Validate(settings));
        Assert.Contains("missing", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
