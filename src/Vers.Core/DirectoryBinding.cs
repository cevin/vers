namespace Vers.Core;

public static class DirectoryBinding
{
    private const string BindPrefix = "@bind:";

    public static bool TryParseCommand(IReadOnlyList<string> arguments, out string requestedVersion)
    {
        requestedVersion = string.Empty;
        if (arguments.Count == 0)
        {
            return false;
        }

        var isBindCommand = arguments[0].Equals("@bind", StringComparison.OrdinalIgnoreCase) ||
            arguments[0].StartsWith(BindPrefix, StringComparison.OrdinalIgnoreCase);
        if (!isBindCommand)
        {
            return false;
        }

        if (arguments.Count != 1)
        {
            throw new InvalidOperationException("The @bind command does not accept additional arguments.");
        }

        requestedVersion = arguments[0].Length > BindPrefix.Length
            ? arguments[0][BindPrefix.Length..].Trim()
            : string.Empty;

        return true;
    }

    public static DirectoryBindingResult Bind(
        Settings settings,
        string groupName,
        string requestedVersion,
        string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Groups.TryGetValue(groupName, out var group))
        {
            throw new InvalidOperationException($"Group '{groupName}' is not configured.");
        }

        var versionName = ResolveVersionName(groupName, group, requestedVersion);
        var storageDirectory = ToolResolver.NormalizePathForStorage(workingDirectory);
        var existingDirectory = group.Projects.Keys.FirstOrDefault(path =>
            ToolResolver.NormalizePath(path).Equals(
                ToolResolver.NormalizePath(storageDirectory),
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal));
        var directoryKey = existingDirectory ?? storageDirectory;
        group.Projects.TryGetValue(directoryKey, out var previousVersionName);
        group.Projects[directoryKey] = versionName;
        return new DirectoryBindingResult(versionName, previousVersionName, directoryKey);
    }

    public static string ResolveVersionName(
        string groupName,
        ToolGroup group,
        string requestedVersion)
    {
        if (string.IsNullOrWhiteSpace(requestedVersion))
        {
            throw new InvalidOperationException(BuildMissingVersionMessage(groupName, group));
        }

        requestedVersion = requestedVersion.Trim();
        if (group.Versions.TryGetValue(requestedVersion, out _))
        {
            return group.Versions.Keys.First(name =>
                name.Equals(requestedVersion, StringComparison.OrdinalIgnoreCase));
        }

        throw new InvalidOperationException(
            $"Version name '{requestedVersion}' was not found in group '{groupName}'." +
            $"{Environment.NewLine}{BuildAvailableVersions(group)}" +
            $"{Environment.NewLine}Usage: {groupName} @bind:<version-name>");
    }

    public static string BuildMissingVersionMessage(string groupName, ToolGroup group) =>
        $"Missing version for group '{groupName}'." +
        $"{Environment.NewLine}{BuildAvailableVersions(group)}" +
        $"{Environment.NewLine}Usage: {groupName} @bind:<version-name>";

    public static string BuildAvailableVersions(ToolGroup group)
    {
        if (group.Versions.Count == 0)
        {
            return "Available versions: (none)";
        }

        var lines = group.Versions
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"  {pair.Key}: {pair.Value.Executable}");

        return $"Available versions:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}

public sealed record DirectoryBindingResult(
    string VersionName,
    string? PreviousVersionName,
    string DirectoryPath)
{
    public bool IsReplacement => PreviousVersionName is not null &&
        !PreviousVersionName.Equals(VersionName, StringComparison.OrdinalIgnoreCase);

    public bool IsUnchanged => PreviousVersionName?.Equals(
        VersionName,
        StringComparison.OrdinalIgnoreCase) == true;
}
