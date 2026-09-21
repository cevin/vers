namespace Vers.Gui;

public static class EnvironmentSuggestionCatalog
{
    private static readonly string[] Common =
    [
        "TEMP", "TMP"
    ];

    private static readonly Dictionary<string, string[]> GroupSuggestions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["php"] = ["COMPOSER_HOME", "COMPOSER_CACHE_DIR"],
        ["java"] = ["JAVA_HOME", "GRADLE_USER_HOME", "ANDROID_SDK_ROOT", "ANDROID_HOME", "ANDROID_AVD_HOME", "ANDROID_EMULATOR_HOME"],
        ["node"] = ["NPM_CONFIG_CACHE", "NPM_CONFIG_TMP", "YARN_CACHE_FOLDER", "PLAYWRIGHT_BROWSERS_PATH", "ELECTRON_CACHE"],
        ["npm"] = ["NPM_CONFIG_CACHE", "NPM_CONFIG_TMP", "YARN_CACHE_FOLDER", "PLAYWRIGHT_BROWSERS_PATH", "ELECTRON_CACHE"],
        ["go"] = ["GOCACHE", "GOMODCACHE", "GOENV"],
        ["rust"] = ["CARGO_HOME", "RUSTUP_HOME", "SCCACHE_DIR"],
        ["cargo"] = ["CARGO_HOME", "RUSTUP_HOME", "SCCACHE_DIR"],
        ["dotnet"] = ["NUGET_PACKAGES", "NUGET_SCRATCH", "DOTNET_CLI_HOME", "ASPNETCORE_TEMP"],
        ["python"] = ["PIP_CACHE_DIR", "PYTHONPYCACHEPREFIX", "PYTHONUSERBASE", "HF_HOME", "TRANSFORMERS_CACHE"],
        ["python3"] = ["PIP_CACHE_DIR", "PYTHONPYCACHEPREFIX", "PYTHONUSERBASE", "HF_HOME", "TRANSFORMERS_CACHE"],
        ["deno"] = ["DENO_DIR"],
        ["hugo"] = ["HUGO_CACHEDIR"],
        ["terraform"] = ["TF_PLUGIN_CACHE_DIR"],
        ["vcpkg"] = ["VCPKG_DEFAULT_BINARY_CACHE"],
        ["conan"] = ["CONAN_USER_HOME"],
        ["choco"] = ["ChocolateyInstall"],
        ["scoop"] = ["SCOOP"]
    };

    private static readonly string[] All = Common
        .Concat(GroupSuggestions.Values.SelectMany(values => values))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static IReadOnlyList<string> ForGroup(string? groupName)
    {
        if (groupName is not null && GroupSuggestions.TryGetValue(groupName, out var suggestions))
        {
            return suggestions.Concat(Common).Concat(All)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return All;
    }
}
