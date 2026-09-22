namespace Vers.Platform;

public interface IPlatformAdapter
{
    string ExecutableSuffix { get; }

    string ToolResourceName { get; }

    string GetInstallationRoot(string startupDirectory);

    PathStatus GetPathStatus(string binDirectory);

    PathStatus EnsureBinOnPath(string binDirectory);

    CommandPriorityStatus GetCommandPriorityStatus(
        string binDirectory,
        IReadOnlyList<string> commandNames);

    void MakeExecutable(string filePath);

    void EnsureCompanionProxy(string proxyPath);

    void DeleteCompanionProxy(string proxyPath);
}

public sealed record PathStatus(bool IsConfigured, bool Changed, string Detail);

public enum CommandPathState
{
    NotManaged,
    Active,
    Conflict,
    Missing
}

public sealed record CommandPathProbe(
    string CommandName,
    CommandPathState State,
    string ExpectedPath,
    string? ResolvedPath);

public sealed record CommandPriorityStatus(IReadOnlyList<CommandPathProbe> Probes)
{
    public bool HasManagedCommands => Probes.Any(probe => probe.State != CommandPathState.NotManaged);

    public bool IsHealthy => HasManagedCommands && Probes
        .Where(probe => probe.State != CommandPathState.NotManaged)
        .All(probe => probe.State == CommandPathState.Active);
}
