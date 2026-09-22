using Vers.Core;

namespace Vers.Core.Tests;

public sealed class DirectoryBindingTests
{
    [Theory]
    [InlineData("php82", "php82")]
    [InlineData("PHP84", "php84")]
    public void ResolveVersionName_RequiresFullNameButIgnoresCase(string requested, string expected)
    {
        var group = CreateGroup("php82", "php84");

        var result = DirectoryBinding.ResolveVersionName("php", group, requested);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveVersionName_RejectsShortcutAndListsExactVersionNames()
    {
        var group = CreateGroup("php72", "php84");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DirectoryBinding.ResolveVersionName("php", group, "72"));

        Assert.Contains("Version name '72' was not found", exception.Message);
        Assert.Contains("  php72: /tools/php72", exception.Message);
        Assert.Contains("  php84: /tools/php84", exception.Message);
    }

    [Fact]
    public void Bind_AddsCurrentDirectoryToGroupProjects()
    {
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = CreateGroup("82")
            }
        };

        var binding = DirectoryBinding.Bind(settings, "php", "82", "/work/api/");

        Assert.Equal("82", binding.VersionName);
        Assert.Null(binding.PreviousVersionName);
        Assert.Equal("/work/api", binding.DirectoryPath);
        Assert.Equal("82", settings.Groups["php"].Projects["/work/api"]);
    }

    [Fact]
    public void Bind_ReplacesExistingDirectoryMapping()
    {
        var group = CreateGroup("72", "84");
        group.Projects["/work/api"] = "72";
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = group
            }
        };

        var binding = DirectoryBinding.Bind(settings, "php", "84", "/work/api/");

        Assert.True(binding.IsReplacement);
        Assert.Equal("72", binding.PreviousVersionName);
        Assert.Equal("84", binding.VersionName);
        Assert.Equal("84", group.Projects["/work/api"]);
        Assert.Single(group.Projects);
    }

    [Fact]
    public void Bind_PreservesWindowsPathFormatForSettings()
    {
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = CreateGroup("72")
            }
        };

        var binding = DirectoryBinding.Bind(settings, "php", "72", @"d:/work/api/");

        Assert.Equal(@"D:\work\api", binding.DirectoryPath);
        Assert.Equal("72", settings.Groups["php"].Projects[@"D:\work\api"]);
    }

    [Fact]
    public void Bind_ReportsUnchangedForSameVersion()
    {
        var group = CreateGroup("84");
        group.Projects["/work/api"] = "84";
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = group
            }
        };

        var binding = DirectoryBinding.Bind(settings, "php", "84", "/work/api");

        Assert.True(binding.IsUnchanged);
        Assert.False(binding.IsReplacement);
        Assert.Single(group.Projects);
    }

    [Theory]
    [InlineData("@bind:82", true, "82")]
    [InlineData("@BIND:php82", true, "php82")]
    [InlineData("@bind", true, "")]
    [InlineData("@bind:", true, "")]
    [InlineData("--version", false, "")]
    public void TryParseCommand_RecognizesBindDirective(string argument, bool expected, string version)
    {
        var result = DirectoryBinding.TryParseCommand([argument], out var requestedVersion);

        Assert.Equal(expected, result);
        Assert.Equal(version, requestedVersion);
    }

    [Fact]
    public void TryParseCommand_RejectsAdditionalArguments()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DirectoryBinding.TryParseCommand(["@bind:82", "--version"], out _));

        Assert.Contains("additional arguments", exception.Message);
    }

    [Fact]
    public void Bind_MissingVersionListsFriendlyChoices()
    {
        var settings = new Settings
        {
            Groups = new Dictionary<string, ToolGroup>(StringComparer.OrdinalIgnoreCase)
            {
                ["php"] = new()
                {
                    Versions = new Dictionary<string, ToolVersion>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["php72"] = new() { Executable = "/tools/php72" },
                        ["php84"] = new() { Executable = "/tools/php84" }
                    }
                }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DirectoryBinding.Bind(settings, "php", string.Empty, "/work"));

        Assert.Contains("Missing version", exception.Message);
        Assert.Contains("  php72: /tools/php72", exception.Message);
        Assert.Contains("  php84: /tools/php84", exception.Message);
        Assert.Contains("Usage: php @bind:<version-name>", exception.Message);
    }

    private static ToolGroup CreateGroup(params string[] versions) => new()
    {
        Versions = versions.ToDictionary(
            name => name,
            name => new ToolVersion { Executable = $"/tools/{name}" },
            StringComparer.OrdinalIgnoreCase)
    };
}
