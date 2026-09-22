using System.Text;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Vers.Platform;

[UnsupportedOSPlatform("windows")]
internal sealed partial class UnixPlatformAdapter(bool isMacOS) : IPlatformAdapter
{
    private const string StartMarker = "# >>> vers PATH >>>";
    private const string EndMarker = "# <<< vers PATH <<<";

    public string ExecutableSuffix => string.Empty;

    public string ToolResourceName => "Vers.Gui.Resources.tool";

    public string GetInstallationRoot(string startupDirectory)
    {
        var overridePath = Environment.GetEnvironmentVariable("VERS_HOME");
        return Path.GetFullPath(string.IsNullOrWhiteSpace(overridePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vers")
            : overridePath);
    }

    public PathStatus GetPathStatus(string binDirectory)
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH");
        var profile = File.Exists(GetProfilePath()) ? File.ReadAllText(GetProfilePath()) : string.Empty;
        var hasManagedBlock = ManagedPathBlockRegex().IsMatch(profile);
        var profileContainsEntry = profile.Contains(CreateExportLine(binDirectory), StringComparison.Ordinal);
        var configured = hasManagedBlock
            ? profileContainsEntry
            : PathList.Contains(currentPath, binDirectory, ':');
        return new PathStatus(configured, false, configured ? "configured" : "missing");
    }

    public PathStatus EnsureBinOnPath(string binDirectory)
    {
        var profilePath = GetProfilePath();
        var exportLine = CreateExportLine(binDirectory);
        var existing = File.Exists(profilePath) ? File.ReadAllText(profilePath) : string.Empty;
        var updated = UpsertManagedPathBlock(existing, exportLine);
        var changed = !existing.Equals(updated, StringComparison.Ordinal);
        if (changed)
        {
            var temporaryPath = $"{profilePath}.vers-{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                File.Move(temporaryPath, profilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        var currentPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", PathList.Prepend(currentPath, binDirectory, ':'));

        return new PathStatus(true, changed, changed ? "added" : "configured");
    }

    public CommandPriorityStatus GetCommandPriorityStatus(
        string binDirectory,
        IReadOnlyList<string> commandNames) =>
        PathPriorityAnalyzer.Analyze(
            binDirectory,
            ExecutableSuffix,
            GetEffectivePathForProbe(binDirectory),
            ':',
            commandNames);

    public void MakeExecutable(string filePath)
    {
        var mode = File.GetUnixFileMode(filePath);
        mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        File.SetUnixFileMode(filePath, mode);
    }

    public void EnsureCompanionProxy(string proxyPath)
    {
    }

    public void DeleteCompanionProxy(string proxyPath)
    {
    }

    private string GetProfilePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, isMacOS ? ".zprofile" : ".profile");
    }

    private string GetEffectivePathForProbe(string binDirectory)
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH");
        if (PathList.Contains(currentPath, binDirectory, ':'))
        {
            return currentPath ?? string.Empty;
        }

        var profilePath = GetProfilePath();
        var profile = File.Exists(profilePath) ? File.ReadAllText(profilePath) : string.Empty;
        return profile.Contains(CreateExportLine(binDirectory), StringComparison.Ordinal)
            ? PathList.Prepend(currentPath, binDirectory, ':')
            : currentPath ?? string.Empty;
    }

    internal static string CreateExportLine(string binDirectory) =>
        $"export PATH=\"{EscapeForDoubleQuotedShell(binDirectory)}:$PATH\"";

    private static string EscapeForDoubleQuotedShell(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("$", "\\$", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);

    internal static string UpsertManagedPathBlock(string existing, string exportLine)
    {
        var contentWithoutManagedBlocks = ManagedPathBlockRegex().Replace(existing, string.Empty)
            .TrimEnd('\r', '\n');
        var block = $"{StartMarker}{Environment.NewLine}{exportLine}{Environment.NewLine}{EndMarker}{Environment.NewLine}";
        return contentWithoutManagedBlocks.Length == 0
            ? block
            : $"{contentWithoutManagedBlocks}{Environment.NewLine}{block}";
    }

    [GeneratedRegex("(?ms)^# >>> vers PATH >>>\\r?\\n.*?^# <<< vers PATH <<<\\r?\\n?")]
    private static partial Regex ManagedPathBlockRegex();
}
