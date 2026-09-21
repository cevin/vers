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

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(path.Trim().Trim('"'));
}
