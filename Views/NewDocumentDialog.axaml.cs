using Avalonia.Controls;
using Avalonia.Interactivity;

namespace pixel_editor.Views;

internal readonly record struct NewDocumentSize(int Width, int Height);

public partial class NewDocumentDialog : Window
{
    public NewDocumentDialog()
    {
        InitializeComponent();
    }

    private void Create_OnClick(object? sender, RoutedEventArgs e)
    {
        if (WidthInput.Value is not { } width || HeightInput.Value is not { } height)
        {
            return;
        }

        Close(new NewDocumentSize(decimal.ToInt32(width), decimal.ToInt32(height)));
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e) =>
        Close((NewDocumentSize?)null);
}
