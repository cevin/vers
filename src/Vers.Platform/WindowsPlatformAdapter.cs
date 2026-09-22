using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Vers.Platform;

[SupportedOSPlatform("windows")]
internal sealed class WindowsPlatformAdapter : IPlatformAdapter
{
    private const uint HwndBroadcast = 0xffff;
    private const uint WmSettingChange = 0x001a;
    private const uint SmtoAbortIfHung = 0x0002;
    private const string VersRegistryPath = @"SOFTWARE\Vers";
    private const string ManagedBinValueName = "ManagedBinPath";

    public string ExecutableSuffix => ".exe";

    public string ToolResourceName => "Vers.Gui.Resources.tool.exe";

    public string GetInstallationRoot(string startupDirectory)
    {
        var overridePath = Environment.GetEnvironmentVariable("VERS_HOME");
        return Path.GetFullPath(string.IsNullOrWhiteSpace(overridePath)
            ? startupDirectory
            : overridePath);
    }

    public PathStatus GetPathStatus(string binDirectory)
    {
        using var environmentKey = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            writable: false);
        var systemPath = environmentKey?.GetValue(
            "Path",
            null,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
        var configured = PathList.Contains(systemPath, binDirectory, ';');
        return new PathStatus(configured, false, configured ? "configured" : "missing");
    }

    public PathStatus EnsureBinOnPath(string binDirectory)
    {
        using var environmentKey = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            writable: true) ?? throw new InvalidOperationException("The Windows system environment registry key was not found.");
        var systemPath = environmentKey.GetValue(
            "Path",
            string.Empty,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;

        using var versKey = Registry.LocalMachine.CreateSubKey(VersRegistryPath, writable: true);
        var previousManagedBin = versKey.GetValue(ManagedBinValueName) as string;
        var pathWithoutPreviousInstall = PathList.Remove(systemPath, previousManagedBin, ';');
        var updatedPath = PathList.Prepend(pathWithoutPreviousInstall, binDirectory, ';');
        var changed = !updatedPath.Equals(systemPath, StringComparison.OrdinalIgnoreCase);
        if (changed)
        {
            environmentKey.SetValue("Path", updatedPath, RegistryValueKind.ExpandString);
        }
        versKey.SetValue(ManagedBinValueName, binDirectory, RegistryValueKind.String);
        AddToCurrentProcess(binDirectory, ';');
        BroadcastEnvironmentChange();

        return new PathStatus(true, changed, changed ? "added" : "configured");
    }

    public CommandPriorityStatus GetCommandPriorityStatus(
        string binDirectory,
        IReadOnlyList<string> commandNames) =>
        PathPriorityAnalyzer.Analyze(
            binDirectory,
            ExecutableSuffix,
            GetPersistedEffectivePath(),
            ';',
            commandNames);

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
        var current = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", PathList.Prepend(current, binDirectory, separator));
    }

    private static string GetPersistedEffectivePath()
    {
        using var machineKey = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            writable: false);
        using var userKey = Registry.CurrentUser.OpenSubKey("Environment", writable: false);
        var machinePath = machineKey?.GetValue(
            "Path",
            string.Empty,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
        var userPath = userKey?.GetValue(
            "Path",
            string.Empty,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
        return string.Join(';', new[] { machinePath, userPath }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static void BroadcastEnvironmentChange()
    {
        _ = SendMessageTimeout(
            new IntPtr(HwndBroadcast),
            WmSettingChange,
            IntPtr.Zero,
            "Environment",
            SmtoAbortIfHung,
            5000,
            out _);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr windowHandle,
        uint message,
        IntPtr messageParameter,
        string value,
        uint flags,
        uint timeout,
        out IntPtr result);
}
