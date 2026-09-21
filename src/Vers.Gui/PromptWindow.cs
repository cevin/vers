using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Vers.Gui;

internal sealed class PromptWindow : Window
{
    private readonly TextBox _textBox;

    public PromptWindow(string title, string label)
    {
        Title = title;
        Width = 420;
        Height = 190;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _textBox = new TextBox { Margin = new Thickness(0, 7, 0, 18) };
        var confirmButton = new Button
        {
            Content = LocalizationService.Get("Confirm"),
            MinWidth = 90
        };
        confirmButton.Click += (_, _) => Close(string.IsNullOrWhiteSpace(_textBox.Text) ? null : _textBox.Text.Trim());
        var cancelButton = new Button
        {
            Content = LocalizationService.Get("Cancel"),
            MinWidth = 90
        };
        cancelButton.Click += (_, _) => Close(null);

        Content = new StackPanel
        {
            Margin = new Thickness(22),
            Children =
            {
                new TextBlock { Text = label },
                _textBox,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, confirmButton }
                }
            }
        };
    }

    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);
        _textBox.Focus();
    }
}
