using System.Diagnostics;
using Vers.Core;

return await ProxyProgram.RunAsync(args);

internal static class ProxyProgram
{
    private const int ConfigurationError = 2;
    private const int LaunchError = 3;

    public static async Task<int> RunAsync(string[] arguments)
    {
        try
        {
            var processPath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine the proxy executable path.");
            var binDirectory = Path.GetDirectoryName(processPath)
                ?? throw new InvalidOperationException("Cannot determine the proxy directory.");
            var rootDirectory = Directory.GetParent(binDirectory)?.FullName
                ?? throw new InvalidOperationException("Cannot determine the installation directory.");
            var groupName = GetGroupName(processPath);
            var settingsPath = Path.Combine(rootDirectory, "data", "settings.json");
            var callerDirectory = Environment.GetEnvironmentVariable("VER_CALLER_CWD")
                ?? Environment.CurrentDirectory;

            var settings = SettingsStore.Load(settingsPath);
            if (DirectoryBinding.TryParseCommand(arguments, out var requestedVersion))
            {
                var binding = DirectoryBinding.Bind(
                    settings,
                    groupName,
                    requestedVersion,
                    callerDirectory);
                SettingsStore.Save(settingsPath, settings);
                WriteBindingResult(groupName, binding);
                return 0;
            }

            var resolved = ToolResolver.Resolve(settings, groupName, callerDirectory);
            var targetPath = EnvironmentValueExpander.Expand(
                resolved.Executable,
                Environment.GetEnvironmentVariable);

            EnsureTargetIsValid(processPath, targetPath, resolved);

            var startInfo = new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = false,
                WorkingDirectory = Directory.Exists(callerDirectory)
                    ? callerDirectory
                    : Environment.CurrentDirectory
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            foreach (var (name, value) in resolved.Environment)
            {
                startInfo.Environment[name] = EnvironmentValueExpander.Expand(
                    value,
                    variableName => startInfo.Environment.TryGetValue(variableName, out var current)
                        ? current
                        : null);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start '{targetPath}'.");
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        catch (FileNotFoundException exception)
        {
            Console.Error.WriteLine($"vers: {exception.Message}");
            return ConfigurationError;
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"vers: {exception.Message}");
            return ConfigurationError;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine($"vers: {exception.Message}");
            return ConfigurationError;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"vers: unable to launch tool: {exception.Message}");
            return LaunchError;
        }
    }

    private static void WriteBindingResult(
        string groupName,
        DirectoryBindingResult binding)
    {
        var directory = binding.DirectoryPath;
        if (binding.IsReplacement)
        {
            Console.WriteLine(
                $"vers: binding replaced. Current directory '{directory}' changed from " +
                $"'{groupName}:{binding.PreviousVersionName}' to '{groupName}:{binding.VersionName}'.");
            return;
        }

        if (binding.IsUnchanged)
        {
            Console.WriteLine(
                $"vers: binding unchanged. Current directory '{directory}' already uses " +
                $"'{groupName}:{binding.VersionName}'.");
            return;
        }

        Console.WriteLine(
            $"vers: binding successful. Current directory '{directory}' now uses " +
            $"'{groupName}:{binding.VersionName}'.");
    }

    private static string GetGroupName(string processPath)
    {
        var fileName = Path.GetFileName(processPath);
        return fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;
    }

    private static void EnsureTargetIsValid(string processPath, string targetPath, ResolvedTool resolved)
    {
        var fullProxyPath = Path.GetFullPath(processPath);
        var fullTargetPath = Path.GetFullPath(targetPath);

        if (string.Equals(fullProxyPath, fullTargetPath, OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Version '{resolved.GroupName}/{resolved.VersionName}' points back to its proxy.");
        }

        if (!File.Exists(fullTargetPath))
        {
            throw new FileNotFoundException(
                $"Executable for '{resolved.GroupName}/{resolved.VersionName}' was not found: {fullTargetPath}",
                fullTargetPath);
        }
    }
}
