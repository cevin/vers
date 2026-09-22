namespace Vers.Platform;

internal static class PathList
{
    public static bool Contains(string? pathValue, string directory, char separator)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var normalizedDirectory = Normalize(directory);

        return pathValue.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Any(entry => entry.Equals(normalizedDirectory, comparison));
    }

    public static string Prepend(string? pathValue, string directory, char separator)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var normalizedDirectory = Normalize(directory);
        var remainingEntries = (pathValue ?? string.Empty)
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => !Normalize(entry).Equals(normalizedDirectory, comparison));

        return string.Join(separator, new[] { directory }.Concat(remainingEntries));
    }

    public static string Remove(string? pathValue, string? directory, char separator)
    {
        if (string.IsNullOrWhiteSpace(pathValue) || string.IsNullOrWhiteSpace(directory))
        {
            return pathValue ?? string.Empty;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var normalizedDirectory = Normalize(directory);
        return string.Join(
            separator,
            pathValue.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(entry => !Normalize(entry).Equals(normalizedDirectory, comparison)));
    }

    private static string Normalize(string path)
    {
        var normalized = path.Trim().Trim('"');
        if (OperatingSystem.IsWindows())
        {
            normalized = Environment.ExpandEnvironmentVariables(normalized);
        }

        return Path.TrimEndingDirectorySeparator(normalized);
    }
}
