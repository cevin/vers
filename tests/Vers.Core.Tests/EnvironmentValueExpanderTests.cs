using Vers.Core;

namespace Vers.Core.Tests;

public sealed class EnvironmentValueExpanderTests
{
    [Theory]
    [InlineData("${PATH}:/tools", "/bin:/tools")]
    [InlineData("$PATH:/tools", "/bin:/tools")]
    [InlineData("%PATH%;C:\\tools", "/bin;C:\\tools")]
    [InlineData("$MISSING/value", "/value")]
    public void Expand_SupportsWindowsAndUnixSyntax(string value, string expected)
    {
        var result = EnvironmentValueExpander.Expand(
            value,
            name => name == "PATH" ? "/bin" : null);

        Assert.Equal(expected, result);
    }
}
