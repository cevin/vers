using Vers.Platform;

namespace Vers.Core.Tests;

public sealed class PathPriorityAnalyzerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"vers-path-tests-{Guid.NewGuid():N}");
    private readonly string _executableSuffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

    [Fact]
    public void Prepend_RemovesDuplicateEntriesAndPlacesDirectoryFirst()
    {
        var bin = Path.Combine(_root, "bin");
        var other = Path.Combine(_root, "other");
        var original = string.Join(Path.PathSeparator, [other, bin, bin + Path.DirectorySeparatorChar]);

        var updated = PathList.Prepend(original, bin, Path.PathSeparator);
        var entries = updated.Split(Path.PathSeparator);

        Assert.Equal(bin, entries[0]);
        Assert.Equal(1, entries.Count(entry => entry == bin));
        Assert.Contains(other, entries);
    }

    [Fact]
    public void Analyze_ReportsConflictWhenAnotherExecutableComesFirst()
    {
        var bin = CreateDirectoryWithCommand("bin", "php");
        var external = CreateDirectoryWithCommand("external", "php");
        var path = string.Join(Path.PathSeparator, [external, bin]);

        var result = PathPriorityAnalyzer.Analyze(bin, _executableSuffix, path, Path.PathSeparator, ["php"]);

        var probe = Assert.Single(result.Probes);
        Assert.Equal(CommandPathState.Conflict, probe.State);
        Assert.Equal(Path.Combine(external, "php" + _executableSuffix), probe.ResolvedPath);
    }

    [Fact]
    public void Analyze_ReportsActiveWhenVersBinComesFirst()
    {
        var bin = CreateDirectoryWithCommand("bin", "custom-tool");
        var external = CreateDirectoryWithCommand("external", "custom-tool");
        var path = string.Join(Path.PathSeparator, [bin, external]);

        var result = PathPriorityAnalyzer.Analyze(
            bin,
            _executableSuffix,
            path,
            Path.PathSeparator,
            ["custom-tool"]);

        Assert.True(result.IsHealthy);
        Assert.Equal(CommandPathState.Active, Assert.Single(result.Probes).State);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CreateDirectoryWithCommand(string directoryName, string commandName)
    {
        var directory = Path.Combine(_root, directoryName);
        Directory.CreateDirectory(directory);
        var commandPath = Path.Combine(directory, commandName + _executableSuffix);
        File.WriteAllText(commandPath, string.Empty);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(commandPath, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }
        return directory;
    }
}
