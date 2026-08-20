using pixel_editor.ViewModels;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.App.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public void NewViewModel_StartsWithCleanBlankDocument()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(16, viewModel.Document.Width);
        Assert.Equal(16, viewModel.Document.Height);
        Assert.Equal("Untitled", viewModel.DocumentName);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.CanUndo);
        Assert.False(viewModel.CanRedo);

        for (var y = 0; y < viewModel.Document.Height; y++)
        {
            for (var x = 0; x < viewModel.Document.Width; x++)
            {
                Assert.Equal(PixelColor.Transparent, viewModel.Document.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void NewViewModel_SelectsBrush()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(EditorTool.Brush, viewModel.ActiveTool);
        Assert.True(viewModel.IsBrushSelected);
        Assert.False(viewModel.IsEraserSelected);
        Assert.False(viewModel.IsFillSelected);
        Assert.False(viewModel.IsEyedropperSelected);
        Assert.False(viewModel.IsRectangleSelected);
        Assert.False(viewModel.IsEllipseSelected);
        Assert.False(viewModel.IsShapeSelected);
        Assert.Equal(ShapeDrawMode.Outline, viewModel.ShapeMode);
        Assert.Equal(
            [ShapeDrawMode.Outline, ShapeDrawMode.Filled],
            viewModel.ShapeDrawModeOptions);
        Assert.Equal(1, viewModel.BrushSize);
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
    public void SelectFillCommand_SelectsFill()
    {
        var viewModel = new MainViewModel();

        viewModel.SelectFillCommand.Execute(null);

        Assert.Equal(EditorTool.Fill, viewModel.ActiveTool);
        Assert.False(viewModel.IsBrushSelected);
        Assert.False(viewModel.IsEraserSelected);
        Assert.True(viewModel.IsFillSelected);
    }

    [Fact]
    public void SelectEyedropperCommand_SelectsEyedropper()
    {
        var viewModel = new MainViewModel();

        viewModel.SelectEyedropperCommand.Execute(null);

        Assert.Equal(EditorTool.Eyedropper, viewModel.ActiveTool);
        Assert.False(viewModel.IsBrushSelected);
        Assert.False(viewModel.IsEraserSelected);
        Assert.False(viewModel.IsFillSelected);
        Assert.True(viewModel.IsEyedropperSelected);
    }

    [Fact]
    public void SelectRectangleCommand_SelectsRectangle()
    {
        var viewModel = new MainViewModel();

        viewModel.SelectRectangleCommand.Execute(null);

        Assert.Equal(EditorTool.Rectangle, viewModel.ActiveTool);
        Assert.True(viewModel.IsRectangleSelected);
        Assert.False(viewModel.IsEllipseSelected);
        Assert.True(viewModel.IsShapeSelected);
    }

    [Fact]
    public void SelectEllipseCommand_SelectsEllipse()
    {
        var viewModel = new MainViewModel();

        viewModel.SelectEllipseCommand.Execute(null);

        Assert.Equal(EditorTool.Ellipse, viewModel.ActiveTool);
        Assert.False(viewModel.IsRectangleSelected);
        Assert.True(viewModel.IsEllipseSelected);
        Assert.True(viewModel.IsShapeSelected);
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
    public void CreateNewDocument_CreatesTransparentUntitledDocumentAndClearsHistory()
    {
        var viewModel = new MainViewModel();
        RecordPixelEdit(viewModel, 0, 0);

        viewModel.CreateNewDocument(24, 18);

        Assert.Equal(24, viewModel.Document.Width);
        Assert.Equal(18, viewModel.Document.Height);
        Assert.Equal(PixelColor.Transparent, viewModel.Document.GetPixel(0, 0));
        Assert.Equal("Untitled", viewModel.DocumentName);
        Assert.Equal("Untitled* - Pixel Editor", viewModel.WindowTitle);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.CanUndo);
        Assert.False(viewModel.CanRedo);
    }

    [Theory]
    [InlineData(0, 16)]
    [InlineData(16, 0)]
    [InlineData(PixelDocumentLimits.MaximumDimension + 1, 16)]
    [InlineData(16, PixelDocumentLimits.MaximumDimension + 1)]
    public void CreateNewDocument_WithInvalidDimensions_LeavesCurrentDocumentUnchanged(
        int width,
        int height)
    {
        var viewModel = new MainViewModel();
        var original = viewModel.Document;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            viewModel.CreateNewDocument(width, height));
        Assert.Same(original, viewModel.Document);
    }

    [Fact]
    public void ResizeDocument_ReplacesCanvasKeepsNameAndClearsHistory()
    {
        var viewModel = new MainViewModel();
        viewModel.MarkDocumentSaved("art.png");
        RecordPixelEdit(viewModel, 0, 0);
        var retainedColor = viewModel.Document.GetPixel(0, 0);

        var wasResized = viewModel.ResizeDocument(20, 18, CanvasAnchor.TopLeft);

        Assert.True(wasResized);
        Assert.Equal(20, viewModel.Document.Width);
        Assert.Equal(18, viewModel.Document.Height);
        Assert.Equal(retainedColor, viewModel.Document.GetPixel(0, 0));
        Assert.Equal("art.png", viewModel.DocumentName);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.CanUndo);
        Assert.False(viewModel.CanRedo);
    }

    [Fact]
    public void ResizeDocument_ToSameDimensions_DoesNotChangeEditorState()
    {
        var viewModel = new MainViewModel();
        RecordPixelEdit(viewModel, 0, 0);
        viewModel.MarkDocumentSaved("art.png");
        var original = viewModel.Document;

        var wasResized = viewModel.ResizeDocument(
            original.Width,
            original.Height,
            CanvasAnchor.Center);

        Assert.False(wasResized);
        Assert.Same(original, viewModel.Document);
        Assert.False(viewModel.IsDirty);
        Assert.True(viewModel.CanUndo);
    }

    [Theory]
    [InlineData(0, 16)]
    [InlineData(PixelDocumentLimits.MaximumDimension + 1, 16)]
    public void ResizeDocument_WithInvalidDimensions_LeavesCurrentDocumentUnchanged(
        int width,
        int height)
    {
        var viewModel = new MainViewModel();
        var original = viewModel.Document;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            viewModel.ResizeDocument(width, height, CanvasAnchor.Center));
        Assert.Same(original, viewModel.Document);
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
    public void SpanHistoryChange_TracksDirtyStateAndUndo()
    {
        var viewModel = new MainViewModel();
        var result = FillTool.Fill(
            viewModel.Document,
            0,
            0,
            viewModel.BrushColor);

        viewModel.History.RecordSpanChange(
            viewModel.Document,
            result.Spans,
            result.PreviousColor,
            result.Color);

        Assert.True(viewModel.IsDirty);
        Assert.True(viewModel.CanUndo);

        viewModel.UndoCommand.Execute(null);

        Assert.False(viewModel.IsDirty);
        Assert.Equal(PixelColor.Transparent, viewModel.Document.GetPixel(0, 0));
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

    [Fact]
    public void BrushSize_WhenChanged_NotifiesBindingsAndClampsToSupportedRange()
    {
        var viewModel = new MainViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.BrushSize = 5;

        Assert.Equal(5, viewModel.BrushSize);
        Assert.Contains(nameof(MainViewModel.BrushSize), changedProperties);
        Assert.Contains(nameof(MainViewModel.BrushSizeText), changedProperties);

        viewModel.BrushSize = 100;
        Assert.Equal(BrushTool.MaximumSize, viewModel.BrushSize);

        viewModel.BrushSize = -10;
        Assert.Equal(BrushTool.MinimumSize, viewModel.BrushSize);
    }

    [Fact]
    public void BrushSizeText_AcceptsTypedValuesAndIgnoresInvalidText()
    {
        var viewModel = new MainViewModel();

        viewModel.BrushSizeText = "12";
        Assert.Equal(12, viewModel.BrushSize);
        Assert.Equal("12", viewModel.BrushSizeText);

        viewModel.BrushSizeText = "100";
        Assert.Equal(BrushTool.MaximumSize, viewModel.BrushSize);

        viewModel.BrushSizeText = "not a number";
        Assert.Equal(BrushTool.MaximumSize, viewModel.BrushSize);
    }

    [Fact]
    public void BrushSizeOptions_ContainsCommonSizesAndSupportedLimits()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(BrushTool.MinimumSize, viewModel.BrushSizeOptions[0]);
        Assert.Contains(8, viewModel.BrushSizeOptions);
        Assert.Contains(16, viewModel.BrushSizeOptions);
        Assert.Equal(BrushTool.MaximumSize, viewModel.BrushSizeOptions[^1]);
    }

    [Fact]
    public void BrushSizeCommands_AdjustSizeWithinSupportedRange()
    {
        var viewModel = new MainViewModel();

        viewModel.IncreaseBrushSizeCommand.Execute(null);
        Assert.Equal(2, viewModel.BrushSize);

        viewModel.DecreaseBrushSizeCommand.Execute(null);
        viewModel.DecreaseBrushSizeCommand.Execute(null);
        Assert.Equal(BrushTool.MinimumSize, viewModel.BrushSize);

        viewModel.BrushSize = BrushTool.MaximumSize;
        viewModel.IncreaseBrushSizeCommand.Execute(null);
        Assert.Equal(BrushTool.MaximumSize, viewModel.BrushSize);
    }

    private static void RecordPixelEdit(MainViewModel viewModel, int x, int y)
    {
        viewModel.History.BeginChangeSet(viewModel.Document);
        viewModel.Document.SetPixel(x, y, viewModel.BrushColor);
        viewModel.History.CommitChangeSet();
    }
}
