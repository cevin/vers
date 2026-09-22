namespace Vers.Core;

public static class ToolResolver
{
    public static ResolvedTool Resolve(
        Settings settings,
        string groupName,
        string workingDirectory,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        if (!settings.Groups.TryGetValue(groupName, out var group))
        {
            throw new InvalidOperationException($"Group '{groupName}' is not configured.");
        }

        var overrideName = $"VER_{SanitizeForEnvironment(groupName)}_VERSION";
        var versionName = NullIfWhiteSpace(getEnvironmentVariable(overrideName))
            ?? FindProjectVersion(group, workingDirectory)
            ?? group.DefaultVersion;

        if (string.IsNullOrWhiteSpace(versionName))
        {
            throw new InvalidOperationException(
                $"No version was selected for group '{groupName}'. Configure a default or project mapping.");
        }

        if (!group.Versions.TryGetValue(versionName, out var version))
        {
            throw new InvalidOperationException(
                $"Version '{versionName}' does not exist in group '{groupName}'.");
        }

        return new ResolvedTool(groupName, versionName, version.Executable, version.Environment);
    }

    public static string? FindProjectVersion(ToolGroup group, string workingDirectory)
    {
        var normalizedWorkingDirectory = NormalizePath(workingDirectory);

        return group.Projects
            .Select(mapping => new
            {
                Path = NormalizePath(mapping.Key),
                mapping.Value
            })
            .Where(mapping => IsSameOrChild(normalizedWorkingDirectory, mapping.Path))
            .OrderByDescending(mapping => mapping.Path.Length)
            .Select(mapping => mapping.Value)
            .FirstOrDefault();
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(path));
        }

        var normalized = path.Trim().Replace('\\', '/');
        while (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    public static string NormalizePathForStorage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(path));
        }

        var trimmed = path.Trim();
        var isDrivePath = trimmed.Length >= 3 &&
            char.IsLetter(trimmed[0]) &&
            trimmed[1] == ':' &&
            trimmed[2] is '/' or '\\';
        var isUncPath = trimmed.StartsWith("\\\\", StringComparison.Ordinal) ||
            trimmed.StartsWith("//", StringComparison.Ordinal);
        if (!isDrivePath && !isUncPath)
        {
            return NormalizePath(trimmed);
        }

        var normalized = trimmed.Replace('/', '\\');
        if (isDrivePath)
        {
            normalized = char.ToUpperInvariant(normalized[0]) + normalized[1..];
        }

        while (normalized.Length > 3 && normalized.EndsWith('\\'))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    private static bool IsSameOrChild(string candidate, string parent)
    {
        if (candidate.Equals(parent, PathComparison))
        {
            return true;
        }

        var prefix = parent.EndsWith('/') ? parent : parent + "/";
        return candidate.StartsWith(prefix, PathComparison);
    }

    private static string SanitizeForEnvironment(string groupName)
    {
        return string.Concat(groupName.Select(character =>
            char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_'));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
