using Vers.Core;

namespace Vers.Core.Tests;

public sealed class SettingsMutationsTests
{
    [Fact]
    public void RenameGroup_PreservesGroupConfiguration()
    {
        var group = new ToolGroup
        {
            DefaultVersion = "84",
            Versions = new Dictionary<string, ToolVersion>(StringComparer.OrdinalIgnoreCase)
            {
                ["84"] = new() { Executable = "/tools/php84" }
            }
        };
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = group
            }
        };

        var result = SettingsMutations.RenameGroup(settings, "php", "php-cli");

        Assert.Equal("php-cli", result);
        Assert.Same(group, settings.Groups["php-cli"]);
        Assert.False(settings.Groups.ContainsKey("php"));
    }

    [Fact]
    public void RenameGroup_RejectsDuplicateName()
    {
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = new(),
                ["java"] = new()
            }
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            SettingsMutations.RenameGroup(settings, "php", "java"));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void RenameVersion_UpdatesDefaultAndProjectMappings()
    {
        var version = new ToolVersion { Executable = "/tools/php84" };
        var group = new ToolGroup
        {
            DefaultVersion = "php84",
            Versions = new Dictionary<string, ToolVersion>(StringComparer.OrdinalIgnoreCase)
            {
                ["php84"] = version
            },
            Projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["/work/api"] = "php84",
                ["/work/web"] = "php84"
            }
        };

        var result = SettingsMutations.RenameVersion(group, "php84", "84");

        Assert.Equal("84", result);
        Assert.Same(version, group.Versions["84"]);
        Assert.False(group.Versions.ContainsKey("php84"));
        Assert.Equal("84", group.DefaultVersion);
        Assert.All(group.Projects.Values, value => Assert.Equal("84", value));
    }

    [Fact]
    public void RenameVersion_RejectsDuplicateName()
    {
        var group = new ToolGroup
        {
            Versions = new Dictionary<string, ToolVersion>(StringComparer.OrdinalIgnoreCase)
            {
                ["72"] = new() { Executable = "/tools/php72" },
                ["84"] = new() { Executable = "/tools/php84" }
            }
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            SettingsMutations.RenameVersion(group, "72", "84"));

        Assert.Contains("already exists", exception.Message);
    }
}
