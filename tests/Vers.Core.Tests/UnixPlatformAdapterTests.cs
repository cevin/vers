using Vers.Platform;

namespace Vers.Core.Tests;

public sealed class UnixPlatformAdapterTests
{
    [Fact]
    public void UpsertManagedPathBlock_ReplacesAllOlderBlocksWithOneCurrentBlock()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        const string existing = "before\n" +
            "# >>> vers PATH >>>\nexport PATH=\"$PATH:/old/one\"\n# <<< vers PATH <<<\n" +
            "middle\n" +
            "# >>> vers PATH >>>\nexport PATH=\"$PATH:/old/two\"\n# <<< vers PATH <<<\n";

        var updated = UnixPlatformAdapter.UpsertManagedPathBlock(
            existing,
            "export PATH=\"$PATH:/current/bin\"");

        Assert.DoesNotContain("/old/one", updated);
        Assert.DoesNotContain("/old/two", updated);
        Assert.Contains("/current/bin", updated);
        Assert.Equal(1, updated.Split("# >>> vers PATH >>>").Length - 1);
        Assert.Contains("before", updated);
        Assert.Contains("middle", updated);
    }
}
