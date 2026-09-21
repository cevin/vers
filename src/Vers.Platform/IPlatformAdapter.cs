namespace Vers.Platform;

public interface IPlatformAdapter
{
    string ExecutableSuffix { get; }

    string ToolResourceName { get; }

    PathStatus GetPathStatus(string binDirectory);

    PathStatus EnsureBinOnUserPath(string binDirectory);

    void MakeExecutable(string filePath);

    void EnsureCompanionProxy(string proxyPath);

    void DeleteCompanionProxy(string proxyPath);
}

public sealed record PathStatus(bool IsConfigured, bool Changed, string Detail);
