using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Vers.Gui;

internal sealed class MessageWindow : Window
{
    public MessageWindow(string title, string message, bool showCancel)
    {
        Title = title;
        Width = 470;
        Height = 210;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var confirmButton = new Button
        {
            Content = LocalizationService.Get("Confirm"),
            MinWidth = 90
        };
        confirmButton.Click += (_, _) => Close(true);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        if (showCancel)
        {
            var cancelButton = new Button
            {
                Content = LocalizationService.Get("Cancel"),
                MinWidth = 90
            };
            cancelButton.Click += (_, _) => Close(false);
            buttons.Children.Add(cancelButton);
        }

        buttons.Children.Add(confirmButton);
        Content = new Grid
        {
            Margin = new Thickness(22),
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                buttons
            }
        };
        Grid.SetRow(buttons, 1);
    }
}
