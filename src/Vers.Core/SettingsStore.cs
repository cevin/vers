using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vers.Core;

public static class SettingsStore
{
    public static Settings Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Settings file was not found.", path);
        }

        using var stream = File.OpenRead(path);
        var settings = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.Settings)
            ?? throw new InvalidDataException("Settings file is empty.");

        NormalizeComparers(settings);
        SettingsValidator.Validate(settings);
        return settings;
    }

    public static Settings LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            return Load(path);
        }

        var settings = new Settings();
        Save(path, settings);
        return settings;
    }

    public static void Save(string path, Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        SettingsValidator.Validate(settings);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.Settings);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void NormalizeComparers(Settings settings)
    {
        settings.Groups = new Dictionary<string, ToolGroup>(
            settings.Groups ?? new Dictionary<string, ToolGroup>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var group in settings.Groups.Values)
        {
            group.Versions = new Dictionary<string, ToolVersion>(
                group.Versions ?? new Dictionary<string, ToolVersion>(),
                StringComparer.OrdinalIgnoreCase);
            group.Projects = new Dictionary<string, string>(
                group.Projects ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var version in group.Versions.Values)
            {
                version.Environment = new Dictionary<string, string>(
                    version.Environment ?? new Dictionary<string, string>(),
                    StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
