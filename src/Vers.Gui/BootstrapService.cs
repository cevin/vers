using System.Reflection;
using System.Security.Cryptography;
using Vers.Core;
using Vers.Platform;

namespace Vers.Gui;

public sealed record AppContext(
    string RootDirectory,
    string SettingsPath,
    string ToolPath,
    string BinDirectory,
    Settings Settings,
    PathStatus PathStatus,
    IPlatformAdapter PlatformAdapter);

public static class BootstrapService
{
    public static AppContext Initialize(string rootDirectory, IPlatformAdapter adapter)
    {
        rootDirectory = adapter.GetInstallationRoot(rootDirectory);
        var dataDirectory = Path.Combine(rootDirectory, "data");
        var utilDirectory = Path.Combine(rootDirectory, "util");
        var binDirectory = Path.Combine(rootDirectory, "bin");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(utilDirectory);
        Directory.CreateDirectory(binDirectory);

        var toolPath = Path.Combine(utilDirectory, "tool" + adapter.ExecutableSuffix);
        ExtractTool(adapter, toolPath);

        var settingsPath = Path.Combine(dataDirectory, "settings.json");
        var settings = SettingsStore.LoadOrCreate(settingsPath);
        foreach (var groupName in settings.Groups.Keys)
        {
            EnsureProxy(groupName, toolPath, binDirectory, adapter);
        }

        var pathStatus = adapter.GetPathStatus(binDirectory);
        return new AppContext(
            rootDirectory,
            settingsPath,
            toolPath,
            binDirectory,
            settings,
            pathStatus,
            adapter);
    }

    public static void EnsureProxy(AppContext context, string groupName) =>
        EnsureProxy(groupName, context.ToolPath, context.BinDirectory, context.PlatformAdapter);

    public static void DeleteProxy(AppContext context, string groupName)
    {
        var proxyPath = GetProxyPath(context, groupName);
        if (File.Exists(proxyPath))
        {
            File.Delete(proxyPath);
        }

        context.PlatformAdapter.DeleteCompanionProxy(proxyPath);
    }

    public static void RenameProxy(AppContext context, string currentName, string newName)
    {
        EnsureProxy(context, newName);

        var currentPath = GetProxyPath(context, currentName);
        var newPath = GetProxyPath(context, newName);
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!Path.GetFullPath(currentPath).Equals(Path.GetFullPath(newPath), comparison))
        {
            DeleteProxy(context, currentName);
        }
    }

    public static string GetProxyPath(AppContext context, string groupName) =>
        Path.Combine(context.BinDirectory, groupName + context.PlatformAdapter.ExecutableSuffix);

    private static void ExtractTool(IPlatformAdapter adapter, string destinationPath)
    {
        using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(adapter.ToolResourceName)
            ?? throw new InvalidOperationException($"Embedded proxy resource is missing: {adapter.ToolResourceName}");

        if (File.Exists(destinationPath) && StreamsMatch(source, destinationPath))
        {
            adapter.MakeExecutable(destinationPath);
            return;
        }

        source.Position = 0;
        WriteStreamAtomically(source, destinationPath);
        adapter.MakeExecutable(destinationPath);
    }

    private static void EnsureProxy(
        string groupName,
        string toolPath,
        string binDirectory,
        IPlatformAdapter adapter)
    {
        SettingsValidator.ValidateGroupName(groupName);
        var proxyPath = Path.Combine(binDirectory, groupName + adapter.ExecutableSuffix);
        if (!File.Exists(proxyPath) || !FilesMatch(toolPath, proxyPath))
        {
            var temporaryPath = proxyPath + ".tmp";
            File.Copy(toolPath, temporaryPath, overwrite: true);
            File.Move(temporaryPath, proxyPath, overwrite: true);
        }

        adapter.MakeExecutable(proxyPath);
        adapter.EnsureCompanionProxy(proxyPath);
    }

    private static bool StreamsMatch(Stream embeddedStream, string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        if (embeddedStream.Length != fileStream.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(embeddedStream),
            SHA256.HashData(fileStream));
    }

    private static bool FilesMatch(string firstPath, string secondPath)
    {
        var first = new FileInfo(firstPath);
        var second = new FileInfo(secondPath);
        if (first.Length != second.Length)
        {
            return false;
        }

        using var firstStream = first.OpenRead();
        using var secondStream = second.OpenRead();
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(firstStream),
            SHA256.HashData(secondStream));
    }

    private static void WriteStreamAtomically(Stream source, string destinationPath)
    {
        var temporaryPath = destinationPath + ".tmp";
        using (var destination = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            source.CopyTo(destination);
            destination.Flush(flushToDisk: true);
        }

        File.Move(temporaryPath, destinationPath, overwrite: true);
    }
}
