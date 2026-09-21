using System.Text.RegularExpressions;

namespace Vers.Core;

public static partial class EnvironmentValueExpander
{
    public static string Expand(string value, Func<string, string?> lookup)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(lookup);

        var expanded = PercentVariableRegex().Replace(value, match => lookup(match.Groups[1].Value) ?? string.Empty);
        return DollarVariableRegex().Replace(expanded, match =>
        {
            var name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return lookup(name) ?? string.Empty;
        });
    }

    [GeneratedRegex("%([^%]+)%", RegexOptions.CultureInvariant)]
    private static partial Regex PercentVariableRegex();

    [GeneratedRegex(@"\$\{([^}]+)\}|\$([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.CultureInvariant)]
    private static partial Regex DollarVariableRegex();
}
