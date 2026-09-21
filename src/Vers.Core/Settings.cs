using System.Text.Json.Serialization;

namespace Vers.Core;

public sealed class Settings
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("uiCulture")]
    public string? UiCulture { get; set; }

    [JsonPropertyName("groups")]
    public Dictionary<string, ToolGroup> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ToolGroup
{
    [JsonPropertyName("defaultVersion")]
    public string? DefaultVersion { get; set; }

    [JsonPropertyName("versions")]
    public Dictionary<string, ToolVersion> Versions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("projects")]
    public Dictionary<string, string> Projects { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ToolVersion
{
    [JsonPropertyName("executable")]
    public string Executable { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    public Dictionary<string, string> Environment { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record ResolvedTool(
    string GroupName,
    string VersionName,
    string Executable,
    IReadOnlyDictionary<string, string> Environment);
