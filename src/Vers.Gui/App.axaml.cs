using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Diagnostics;
using Vers.Platform;

namespace Vers.Gui;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var adapter = PlatformAdapterFactory.Create();
                var context = BootstrapService.Initialize(Environment.CurrentDirectory, adapter);
                desktop.MainWindow = new MainWindow(context);
            }
            catch (UnauthorizedAccessException exception) when (OperatingSystem.IsWindows() && !Program.IsElevatedRelaunch)
            {
                if (TryRelaunchElevated())
                {
                    desktop.Shutdown();
                    return;
                }

                desktop.MainWindow = CreateFatalErrorWindow(exception.Message);
            }
            catch (Exception exception)
            {
                desktop.MainWindow = CreateFatalErrorWindow(exception.Message);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateFatalErrorWindow(string message) => new()
    {
        Title = "Vers",
        Width = 520,
        Height = 240,
        Content = new TextBlock
        {
            Text = message,
            Margin = new Thickness(24),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        }
    };

    private static bool TryRelaunchElevated()
    {
        var processPath = Environment.ProcessPath;
        if (processPath is null)
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            };
            startInfo.ArgumentList.Add("--vers-elevated");
            return Process.Start(startInfo) is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
