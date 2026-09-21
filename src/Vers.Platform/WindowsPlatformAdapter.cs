using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace Vers.Platform;

[SupportedOSPlatform("windows")]
internal sealed class WindowsPlatformAdapter : IPlatformAdapter
{
    public string ExecutableSuffix => ".exe";

    public string ToolResourceName => "Vers.Gui.Resources.tool.exe";

    public PathStatus GetPathStatus(string binDirectory)
    {
        using var environmentKey = Registry.CurrentUser.OpenSubKey("Environment", writable: false);
        var userPath = environmentKey?.GetValue(
            "Path",
            null,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
        var configured = PathList.Contains(userPath, binDirectory, ';') ||
            PathList.Contains(Environment.GetEnvironmentVariable("PATH"), binDirectory, ';');
        return new PathStatus(configured, false, configured ? "configured" : "missing");
    }

    public PathStatus EnsureBinOnUserPath(string binDirectory)
    {
        using var environmentKey = Registry.CurrentUser.CreateSubKey("Environment", writable: true);
        var userPath = environmentKey.GetValue(
            "Path",
            string.Empty,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;

        if (PathList.Contains(userPath, binDirectory, ';') ||
            PathList.Contains(Environment.GetEnvironmentVariable("PATH"), binDirectory, ';'))
        {
            AddToCurrentProcess(binDirectory, ';');
            return new PathStatus(true, false, "configured");
        }

        var updatedPath = string.IsNullOrWhiteSpace(userPath)
            ? binDirectory
            : $"{userPath.TrimEnd(';')};{binDirectory}";
        environmentKey.SetValue("Path", updatedPath, RegistryValueKind.ExpandString);
        AddToCurrentProcess(binDirectory, ';');

        return new PathStatus(true, true, "added");
    }

    public void MakeExecutable(string filePath)
    {
    }

    public void EnsureCompanionProxy(string proxyPath)
    {
        var commandName = Path.GetFileNameWithoutExtension(proxyPath);
        var companionPath = Path.Combine(Path.GetDirectoryName(proxyPath)!, commandName);
        var script = $"#!/bin/sh\nexport VER_CALLER_CWD=\"$(wslpath -w \"$PWD\" 2>/dev/null || printf '%s' \"$PWD\")\"\nexec \"$(dirname \"$0\")/{commandName}.exe\" \"$@\"\n";
        File.WriteAllText(companionPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public void DeleteCompanionProxy(string proxyPath)
    {
        var companionPath = Path.Combine(
            Path.GetDirectoryName(proxyPath)!,
            Path.GetFileNameWithoutExtension(proxyPath));
        if (File.Exists(companionPath))
        {
            File.Delete(companionPath);
        }
    }

    private static void AddToCurrentProcess(string binDirectory, char separator)
    {
        var current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!PathList.Contains(current, binDirectory, separator))
        {
            Environment.SetEnvironmentVariable(
                "PATH",
                string.IsNullOrEmpty(current) ? binDirectory : $"{current}{separator}{binDirectory}");
        }
    }
}
