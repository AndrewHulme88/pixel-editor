using Avalonia.Headless.XUnit;
using pixel_editor.ViewModels;
using pixel_editor.Views;
using Xunit;

namespace PixelEditor.Ui.Tests;

public sealed class WindowSmokeTests
{
    [AvaloniaFact]
    public void MainWindow_WithBlankStartupDocument_ClosesWithoutConfirmation()
    {
        var viewModel = new MainViewModel();
        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();

        Assert.True(window.IsVisible);
        Assert.False(viewModel.IsDirty);
        window.Close();
        Assert.False(window.IsVisible);
        Assert.Empty(window.OwnedWindows);
    }
}
