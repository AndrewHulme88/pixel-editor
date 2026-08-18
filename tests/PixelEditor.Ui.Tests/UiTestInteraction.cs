using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace PixelEditor.Ui.Tests;

internal static class UiTestInteraction
{
    public static void ClickButton(Window dialog, string content)
    {
        var button = dialog
            .GetVisualDescendants()
            .OfType<Button>()
            .Single(candidate => Equals(candidate.Content, content));
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
}
