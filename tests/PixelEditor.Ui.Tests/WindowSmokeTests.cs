using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using pixel_editor.Controls;
using pixel_editor.ViewModels;
using pixel_editor.Views;
using PixelEditor.Core.Tools;
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

    [AvaloniaFact]
    public void ShapeModeSelector_FollowsSelectedToolAndUpdatesCanvas()
    {
        var viewModel = new MainViewModel();
        var window = new MainWindow
        {
            DataContext = viewModel
        };
        window.Show();
        var selector = window.FindControl<ComboBox>("ShapeModeSelector")!;
        var canvas = window.FindControl<PixelCanvas>("EditorCanvas")!;

        Assert.False(selector.IsEnabled);

        viewModel.SelectRectangleCommand.Execute(null);
        selector.SelectedItem = ShapeDrawMode.Filled;

        Assert.True(selector.IsEnabled);
        Assert.Equal(ShapeDrawMode.Filled, viewModel.ShapeMode);
        Assert.Equal(ShapeDrawMode.Filled, canvas.ShapeMode);
        viewModel.MarkDocumentSaved("test.png");
        window.Close();
    }
}
