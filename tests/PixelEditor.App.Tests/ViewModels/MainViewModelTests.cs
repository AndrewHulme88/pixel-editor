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

    [Fact]
    public void UndoAndRedoCommands_FollowDocumentHistory()
    {
        var viewModel = new MainViewModel();
        var originalColor = viewModel.Document.GetPixel(0, 0);

        Assert.False(viewModel.UndoCommand.CanExecute(null));
        Assert.False(viewModel.RedoCommand.CanExecute(null));

        viewModel.History.BeginChangeSet(viewModel.Document);
        viewModel.Document.SetPixel(0, 0, viewModel.BrushColor);
        viewModel.History.CommitChangeSet();

        Assert.True(viewModel.UndoCommand.CanExecute(null));
        viewModel.UndoCommand.Execute(null);
        Assert.Equal(originalColor, viewModel.Document.GetPixel(0, 0));

        Assert.True(viewModel.RedoCommand.CanExecute(null));
        viewModel.RedoCommand.Execute(null);
        Assert.Equal(viewModel.BrushColor, viewModel.Document.GetPixel(0, 0));
    }
}
