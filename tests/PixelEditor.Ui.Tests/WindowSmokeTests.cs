using Avalonia.Headless.XUnit;
using pixel_editor.ViewModels;
using pixel_editor.Views;
using Xunit;

namespace PixelEditor.Ui.Tests;

public sealed class WindowSmokeTests
{
    [AvaloniaFact]
    public void MainWindow_CanOpenAndCloseHeadlessly()
    {
        var window = new MainWindow
        {
            DataContext = new MainViewModel()
        };

        window.Show();

        Assert.True(window.IsVisible);
        window.Close();
    }
}
