namespace Vers.Gui;

public sealed record GroupListItem(string Name, bool HasWarning, string? WarningText);

public sealed class EnvironmentRow
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public sealed class ProjectRow
{
    public string Path { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;
}
