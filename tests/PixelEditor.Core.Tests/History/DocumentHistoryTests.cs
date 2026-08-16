using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.Core.Tests.History;

public sealed class DocumentHistoryTests
{
    private static readonly PixelColor Red = new(220, 50, 50);
    private static readonly PixelColor Blue = new(50, 100, 220);

    [Fact]
    public void NewHistory_CannotUndoOrRedo()
    {
        var history = new DocumentHistory();

        Assert.Equal(0, history.CurrentStateId);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.Undo());
        Assert.False(history.Redo());
    }

    [Fact]
    public void CommittedStroke_UndoesAndRedoesAsOneEdit()
    {
        var document = new PixelDocument(5, 1);
        var history = new DocumentHistory();

        history.BeginChangeSet(document);
        BrushTool.DrawLine(document, 0, 0, 4, 0, Red);
        Assert.True(history.CommitChangeSet());

        Assert.True(history.Undo());

        for (var x = 0; x < document.Width; x++)
        {
            Assert.Equal(PixelColor.Transparent, document.GetPixel(x, 0));
        }

        Assert.True(history.Redo());

        for (var x = 0; x < document.Width; x++)
        {
            Assert.Equal(Red, document.GetPixel(x, 0));
        }
    }

    [Fact]
    public void RepeatedPixelChanges_KeepOriginalAndFinalColors()
    {
        var document = new PixelDocument(1, 1);
        var history = new DocumentHistory();

        history.BeginChangeSet(document);
        document.SetPixel(0, 0, Red);
        document.SetPixel(0, 0, Blue);
        history.CommitChangeSet();

        history.Undo();
        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 0));

        history.Redo();
        Assert.Equal(Blue, document.GetPixel(0, 0));
    }

    [Fact]
    public void EmptyChangeSet_DoesNotCreateHistoryEntry()
    {
        var document = new PixelDocument(1, 1);
        var history = new DocumentHistory();

        history.BeginChangeSet(document);
        document.SetPixel(0, 0, PixelColor.Transparent);

        Assert.False(history.CommitChangeSet());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void ChangeSetEndingAtOriginalColor_DoesNotCreateHistoryEntry()
    {
        var document = new PixelDocument(1, 1);
        var history = new DocumentHistory();

        history.BeginChangeSet(document);
        document.SetPixel(0, 0, Red);
        document.SetPixel(0, 0, PixelColor.Transparent);

        Assert.False(history.CommitChangeSet());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void NewEdit_AfterUndo_ClearsRedoStack()
    {
        var document = new PixelDocument(2, 1);
        var history = new DocumentHistory();

        RecordPixel(history, document, 0, Red);
        history.Undo();
        Assert.True(history.CanRedo);

        RecordPixel(history, document, 1, Blue);

        Assert.False(history.CanRedo);
        Assert.False(history.Redo());
        Assert.Equal(Blue, document.GetPixel(1, 0));
    }

    [Fact]
    public void StateId_IdentifiesUndoRedoAndBranchedStates()
    {
        var document = new PixelDocument(2, 1);
        var history = new DocumentHistory();
        var initialState = history.CurrentStateId;

        RecordPixel(history, document, 0, Red);
        var firstEditState = history.CurrentStateId;

        Assert.NotEqual(initialState, firstEditState);
        history.Undo();
        Assert.Equal(initialState, history.CurrentStateId);
        history.Redo();
        Assert.Equal(firstEditState, history.CurrentStateId);

        history.Undo();
        RecordPixel(history, document, 1, Blue);

        Assert.NotEqual(initialState, history.CurrentStateId);
        Assert.NotEqual(firstEditState, history.CurrentStateId);
    }

    [Fact]
    public void MultipleEdits_UndoAndRedoInOrder()
    {
        var document = new PixelDocument(1, 1);
        var history = new DocumentHistory();

        RecordPixel(history, document, 0, Red);
        RecordPixel(history, document, 0, Blue);

        history.Undo();
        Assert.Equal(Red, document.GetPixel(0, 0));

        history.Undo();
        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 0));

        history.Redo();
        Assert.Equal(Red, document.GetPixel(0, 0));

        history.Redo();
        Assert.Equal(Blue, document.GetPixel(0, 0));
    }

    [Fact]
    public void ActiveChangeSet_DisablesUndoUntilCommitted()
    {
        var document = new PixelDocument(2, 1);
        var history = new DocumentHistory();

        RecordPixel(history, document, 0, Red);
        history.BeginChangeSet(document);

        Assert.False(history.CanUndo);
        Assert.False(history.Undo());

        history.CommitChangeSet();
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void BeginChangeSet_WhileRecording_Throws()
    {
        var document = new PixelDocument(1, 1);
        var history = new DocumentHistory();
        history.BeginChangeSet(document);

        Assert.Throws<InvalidOperationException>(() => history.BeginChangeSet(document));

        history.CommitChangeSet();
    }

    [Fact]
    public void Clear_RemovesUndoAndRedoEntries()
    {
        var document = new PixelDocument(2, 1);
        var history = new DocumentHistory();

        RecordPixel(history, document, 0, Red);
        RecordPixel(history, document, 1, Blue);
        history.Undo();

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    private static void RecordPixel(
        DocumentHistory history,
        PixelDocument document,
        int x,
        PixelColor color)
    {
        history.BeginChangeSet(document);
        document.SetPixel(x, 0, color);
        history.CommitChangeSet();
    }
}
