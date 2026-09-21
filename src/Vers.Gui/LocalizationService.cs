using System.Globalization;
using System.Diagnostics;
using System.Resources;

namespace Vers.Gui;

public static class LocalizationService
{
    private static readonly ResourceManager ResourceManager =
        new("Vers.Gui.Resources.Strings", typeof(LocalizationService).Assembly);

    public static string CurrentCulture { get; private set; } = "en-US";

    public static IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("en-US", "English"),
        new("zh-CN", "简体中文"),
        new("ja-JP", "日本語")
    ];

    public static void SetCulture(string? cultureName)
    {
        CurrentCulture = NormalizeCulture(cultureName);
        var culture = CultureInfo.GetCultureInfo(CurrentCulture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.GetCultureInfo(CurrentCulture)) ?? key;

    public static string DetectSystemCulture()
    {
        if (OperatingSystem.IsMacOS())
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/defaults",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                startInfo.ArgumentList.Add("read");
                startInfo.ArgumentList.Add("-g");
                startInfo.ArgumentList.Add("AppleLanguages");
                using var process = Process.Start(startInfo);
                var appleLanguages = process?.StandardOutput.ReadToEnd();
                process?.WaitForExit();
                var detected = TryDetectSupportedCulture(appleLanguages);
                if (detected is not null)
                {
                    return detected;
                }
            }
            catch
            {
                // Fall through to the platform-neutral culture sources.
            }
        }

        var candidates = new[]
        {
            CultureInfo.CurrentUICulture.Name,
            CultureInfo.InstalledUICulture.Name,
            Environment.GetEnvironmentVariable("LC_ALL"),
            Environment.GetEnvironmentVariable("LANG")
        };
        return candidates.Select(TryDetectSupportedCulture).FirstOrDefault(culture => culture is not null)
            ?? "en-US";
    }

    public static string NormalizeCulture(string? cultureName)
    {
        if (cultureName?.StartsWith("zh", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "zh-CN";
        }

        if (cultureName?.StartsWith("ja", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "ja-JP";
        }

        return "en-US";
    }

    private static string? TryDetectSupportedCulture(string? value)
    {
        if (value?.Contains("zh", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "zh-CN";
        }

        if (value?.Contains("ja", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "ja-JP";
        }

        if (value?.Contains("en", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "en-US";
        }

        return null;
    }
}

public sealed record LanguageOption(string CultureName, string DisplayName);
