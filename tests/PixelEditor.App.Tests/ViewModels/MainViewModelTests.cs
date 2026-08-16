using pixel_editor.ViewModels;
using PixelEditor.Core.Documents;
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
        Assert.False(viewModel.IsDirty);
        Assert.Equal("Untitled - Pixel Editor", viewModel.WindowTitle);
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

    [Fact]
    public void ReplaceDocument_ReplacesDocumentAndClearsHistory()
    {
        var viewModel = new MainViewModel();
        var replacement = new PixelDocument(3, 2);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.History.BeginChangeSet(viewModel.Document);
        viewModel.Document.SetPixel(0, 0, viewModel.BrushColor);
        viewModel.History.CommitChangeSet();
        Assert.True(viewModel.CanUndo);

        viewModel.ReplaceDocument(replacement, "opened.png");

        Assert.Same(replacement, viewModel.Document);
        Assert.Equal("opened.png", viewModel.DocumentName);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.CanUndo);
        Assert.False(viewModel.CanRedo);
        Assert.Contains(nameof(MainViewModel.Document), changedProperties);
    }

    [Fact]
    public void ReplaceDocument_WithNull_Throws()
    {
        var viewModel = new MainViewModel();

        Assert.Throws<ArgumentNullException>(() => viewModel.ReplaceDocument(null!, "opened.png"));
    }

    [Fact]
    public void DocumentState_TracksEditsSaveUndoAndRedo()
    {
        var viewModel = new MainViewModel();

        RecordPixelEdit(viewModel, 0, 0);

        Assert.True(viewModel.IsDirty);
        Assert.Equal("Untitled* - Pixel Editor", viewModel.WindowTitle);

        viewModel.UndoCommand.Execute(null);
        Assert.False(viewModel.IsDirty);

        viewModel.RedoCommand.Execute(null);
        Assert.True(viewModel.IsDirty);

        viewModel.MarkDocumentSaved("art.png");

        Assert.False(viewModel.IsDirty);
        Assert.Equal("art.png - Pixel Editor", viewModel.WindowTitle);

        RecordPixelEdit(viewModel, 1, 0);
        Assert.True(viewModel.IsDirty);

        viewModel.UndoCommand.Execute(null);
        Assert.False(viewModel.IsDirty);

        viewModel.RedoCommand.Execute(null);
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void DocumentState_NewBranchDoesNotMatchDiscardedSavedState()
    {
        var viewModel = new MainViewModel();

        RecordPixelEdit(viewModel, 0, 0);
        viewModel.MarkDocumentSaved("art.png");
        viewModel.UndoCommand.Execute(null);

        RecordPixelEdit(viewModel, 1, 0);

        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void BrushColor_WhenChanged_NotifiesBindings()
    {
        var viewModel = new MainViewModel();
        var selectedColor = new PixelColor(80, 120, 160, 200);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.BrushColor = selectedColor;

        Assert.Equal(selectedColor, viewModel.BrushColor);
        Assert.Contains(nameof(MainViewModel.BrushColor), changedProperties);
    }

    [Fact]
    public void BrushColor_WhenSwitchingTools_RemainsSelected()
    {
        var viewModel = new MainViewModel
        {
            BrushColor = new PixelColor(80, 120, 160, 200)
        };

        viewModel.SelectEraserCommand.Execute(null);
        viewModel.SelectBrushCommand.Execute(null);

        Assert.Equal(new PixelColor(80, 120, 160, 200), viewModel.BrushColor);
    }

    private static void RecordPixelEdit(MainViewModel viewModel, int x, int y)
    {
        viewModel.History.BeginChangeSet(viewModel.Document);
        viewModel.Document.SetPixel(x, y, viewModel.BrushColor);
        viewModel.History.CommitChangeSet();
    }
}
