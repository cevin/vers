namespace Vers.Core;

public static class SettingsMutations
{
    public static string RenameGroup(Settings settings, string currentName, string newName)
    {
        ArgumentNullException.ThrowIfNull(settings);

        newName = newName.Trim();
        SettingsValidator.ValidateGroupName(newName);
        var storedName = settings.Groups.Keys.FirstOrDefault(name =>
            name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"Group '{currentName}' does not exist.");
        var duplicateName = settings.Groups.Keys.FirstOrDefault(name =>
            !name.Equals(storedName, StringComparison.OrdinalIgnoreCase) &&
            name.Equals(newName, StringComparison.OrdinalIgnoreCase));
        if (duplicateName is not null)
        {
            throw new InvalidDataException($"Group '{newName}' already exists.");
        }

        if (storedName.Equals(newName, StringComparison.Ordinal))
        {
            return storedName;
        }

        var group = settings.Groups[storedName];
        settings.Groups.Remove(storedName);
        settings.Groups.Add(newName, group);
        return newName;
    }

    public static string RenameVersion(ToolGroup group, string currentName, string newName)
    {
        ArgumentNullException.ThrowIfNull(group);

        newName = newName.Trim();
        if (newName.Length == 0)
        {
            throw new InvalidDataException("Version name cannot be empty.");
        }

        var storedName = group.Versions.Keys.FirstOrDefault(name =>
            name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"Version '{currentName}' does not exist.");
        var duplicateName = group.Versions.Keys.FirstOrDefault(name =>
            !name.Equals(storedName, StringComparison.OrdinalIgnoreCase) &&
            name.Equals(newName, StringComparison.OrdinalIgnoreCase));
        if (duplicateName is not null)
        {
            throw new InvalidDataException($"Version '{newName}' already exists.");
        }

        if (storedName.Equals(newName, StringComparison.Ordinal))
        {
            return storedName;
        }

        var version = group.Versions[storedName];
        group.Versions.Remove(storedName);
        group.Versions.Add(newName, version);

        if (string.Equals(group.DefaultVersion, storedName, StringComparison.OrdinalIgnoreCase))
        {
            group.DefaultVersion = newName;
        }

        foreach (var projectPath in group.Projects
                     .Where(pair => pair.Value.Equals(storedName, StringComparison.OrdinalIgnoreCase))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            group.Projects[projectPath] = newName;
        }

        return newName;
    }
}
