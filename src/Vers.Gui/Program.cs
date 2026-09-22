using Avalonia;

namespace Vers.Gui;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        IsElevatedRelaunch = args.Contains("--vers-elevated", StringComparer.Ordinal);
        SetPathOnLaunch = args.Contains("--vers-set-path", StringComparer.Ordinal);
        var applicationArgs = args.Where(argument =>
            argument is not "--vers-elevated" and not "--vers-set-path").ToArray();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(applicationArgs);
    }

    public static bool IsElevatedRelaunch { get; private set; }

    public static bool SetPathOnLaunch { get; private set; }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
