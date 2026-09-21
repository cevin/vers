using Avalonia;

namespace Vers.Gui;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        IsElevatedRelaunch = args.Contains("--vers-elevated", StringComparer.Ordinal);
        var applicationArgs = args.Where(argument => argument != "--vers-elevated").ToArray();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(applicationArgs);
    }

    public static bool IsElevatedRelaunch { get; private set; }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
