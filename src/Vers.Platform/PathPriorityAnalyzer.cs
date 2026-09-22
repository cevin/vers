namespace Vers.Platform;

internal static class PathPriorityAnalyzer
{
    public static CommandPriorityStatus Analyze(
        string binDirectory,
        string executableSuffix,
        string? pathValue,
        char separator,
        IReadOnlyList<string> commandNames)
    {
        var probes = commandNames
            .Select(commandName => AnalyzeCommand(
                binDirectory,
                executableSuffix,
                pathValue,
                separator,
                commandName))
            .ToArray();
        return new CommandPriorityStatus(probes);
    }

    private static CommandPathProbe AnalyzeCommand(
        string binDirectory,
        string executableSuffix,
        string? pathValue,
        char separator,
        string commandName)
    {
        var expectedPath = Path.GetFullPath(Path.Combine(binDirectory, commandName + executableSuffix));
        if (!File.Exists(expectedPath))
        {
            return new CommandPathProbe(commandName, CommandPathState.NotManaged, expectedPath, null);
        }

        var resolvedPath = ResolveFirst(pathValue, separator, commandName, executableSuffix);
        if (resolvedPath is null)
        {
            return new CommandPathProbe(commandName, CommandPathState.Missing, expectedPath, null);
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var state = Path.GetFullPath(resolvedPath).Equals(expectedPath, comparison)
            ? CommandPathState.Active
            : CommandPathState.Conflict;
        return new CommandPathProbe(commandName, state, expectedPath, resolvedPath);
    }

    private static string? ResolveFirst(
        string? pathValue,
        char separator,
        string commandName,
        string executableSuffix)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        var extensions = GetCandidateExtensions(executableSuffix);
        foreach (var rawDirectory in pathValue.Split(
                     separator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var directory = Environment.ExpandEnvironmentVariables(rawDirectory.Trim('"'));
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, commandName + extension);
                if (IsExecutable(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        return null;
    }

    private static bool IsExecutable(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        var mode = File.GetUnixFileMode(path);
        const UnixFileMode executeBits =
            UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        return (mode & executeBits) != 0;
    }

    private static IReadOnlyList<string> GetCandidateExtensions(string executableSuffix)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [executableSuffix];
        }

        var pathExtensions = Environment.GetEnvironmentVariable("PATHEXT")
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [".COM", ".EXE", ".BAT", ".CMD"];
        return pathExtensions
            .Select(extension => extension.StartsWith('.') ? extension : "." + extension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
