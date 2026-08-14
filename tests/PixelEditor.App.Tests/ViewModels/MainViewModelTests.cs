using pixel_editor.ViewModels;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.App.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public void NewViewModel_SelectsBrush()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(EditorTool.Brush, viewModel.ActiveTool);
        Assert.True(viewModel.IsBrushSelected);
        Assert.False(viewModel.IsEraserSelected);
    }

    [Fact]
    public void SelectEraserCommand_SelectsEraser()
    {
        var viewModel = new MainViewModel();

        viewModel.SelectEraserCommand.Execute(null);

        Assert.Equal(EditorTool.Eraser, viewModel.ActiveTool);
        Assert.False(viewModel.IsBrushSelected);
        Assert.True(viewModel.IsEraserSelected);
    }

    [Fact]
    public void SelectBrushCommand_SelectsBrush()
    {
        var viewModel = new MainViewModel
        {
            ActiveTool = EditorTool.Eraser
        };

        viewModel.SelectBrushCommand.Execute(null);

        Assert.Equal(EditorTool.Brush, viewModel.ActiveTool);
        Assert.True(viewModel.IsBrushSelected);
        Assert.False(viewModel.IsEraserSelected);
    }
}
