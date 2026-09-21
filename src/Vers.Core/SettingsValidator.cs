using System.Text.RegularExpressions;

namespace Vers.Core;

public static partial class SettingsValidator
{
    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static void Validate(Settings settings)
    {
        if (settings.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported settings schema version: {settings.SchemaVersion}.");
        }

        foreach (var (groupName, group) in settings.Groups)
        {
            ValidateGroupName(groupName);

            if (group.DefaultVersion is not null && !group.Versions.ContainsKey(group.DefaultVersion))
            {
                throw new InvalidDataException(
                    $"Default version '{group.DefaultVersion}' does not exist in group '{groupName}'.");
            }

            foreach (var (versionName, version) in group.Versions)
            {
                if (string.IsNullOrWhiteSpace(versionName))
                {
                    throw new InvalidDataException($"Group '{groupName}' contains an empty version name.");
                }

                if (string.IsNullOrWhiteSpace(version.Executable))
                {
                    throw new InvalidDataException(
                        $"Executable is empty for version '{groupName}/{versionName}'.");
                }

                foreach (var variableName in version.Environment.Keys)
                {
                    ValidateEnvironmentVariableName(variableName);
                }
            }

            foreach (var (projectPath, versionName) in group.Projects)
            {
                if (string.IsNullOrWhiteSpace(projectPath))
                {
                    throw new InvalidDataException($"Group '{groupName}' contains an empty project path.");
                }

                if (!group.Versions.ContainsKey(versionName))
                {
                    throw new InvalidDataException(
                        $"Project '{projectPath}' refers to missing version '{groupName}/{versionName}'.");
                }
            }
        }
    }

    public static void ValidateGroupName(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) ||
            groupName is "." or ".." ||
            !GroupNameRegex().IsMatch(groupName) ||
            ReservedFileNames.Contains(groupName))
        {
            throw new InvalidDataException(
                $"Invalid group name '{groupName}'. Use letters, numbers, dots, underscores, or hyphens.");
        }
    }

    public static void ValidateEnvironmentVariableName(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName) ||
            variableName.Contains('=') ||
            variableName.Contains('\0'))
        {
            throw new InvalidDataException($"Invalid environment variable name '{variableName}'.");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex GroupNameRegex();
}
