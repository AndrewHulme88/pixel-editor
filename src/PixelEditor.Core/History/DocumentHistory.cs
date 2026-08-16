using PixelEditor.Core.Documents;

namespace PixelEditor.Core.History;

// Stores reversible pixel edits without copying the entire document.
public sealed class DocumentHistory
{
    private readonly Stack<PixelEdit> _undoStack = new();
    private readonly Stack<PixelEdit> _redoStack = new();
    private ActiveChangeSet? _activeChangeSet;
    private long _currentStateId;
    private long _nextStateId;

    public event EventHandler? Changed;

    public bool CanUndo => _activeChangeSet is null && _undoStack.Count > 0;

    public bool CanRedo => _activeChangeSet is null && _redoStack.Count > 0;

    // Identifies the current point in history so saved states can be recognized after undo or redo.
    public long CurrentStateId => _currentStateId;

    public void BeginChangeSet(PixelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_activeChangeSet is not null)
        {
            throw new InvalidOperationException("A change set is already being recorded.");
        }

        _activeChangeSet = new ActiveChangeSet(document);
        document.PixelChanged += OnPixelChanged;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool CommitChangeSet()
    {
        if (_activeChangeSet is null)
        {
            return false;
        }

        var changeSet = _activeChangeSet;
        changeSet.Document.PixelChanged -= OnPixelChanged;
        _activeChangeSet = null;

        var edit = changeSet.CreateEdit();

        if (edit.Length > 0)
        {
            var nextStateId = checked(++_nextStateId);
            _undoStack.Push(new PixelEdit(
                changeSet.Document,
                edit,
                _currentStateId,
                nextStateId));
            _redoStack.Clear();
            _currentStateId = nextStateId;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return edit.Length > 0;
    }

    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var edit = _undoStack.Pop();

        for (var index = edit.Changes.Length - 1; index >= 0; index--)
        {
            var change = edit.Changes[index];
            edit.Document.SetPixel(change.X, change.Y, change.PreviousColor);
        }

        _redoStack.Push(edit);
        _currentStateId = edit.PreviousStateId;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
        {
            return false;
        }

        var edit = _redoStack.Pop();

        foreach (var change in edit.Changes)
        {
            edit.Document.SetPixel(change.X, change.Y, change.Color);
        }

        _undoStack.Push(edit);
        _currentStateId = edit.ResultStateId;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        if (_activeChangeSet is not null)
        {
            throw new InvalidOperationException("History cannot be cleared while recording changes.");
        }

        if (_undoStack.Count == 0 && _redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Clear();
        _redoStack.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnPixelChanged(object? sender, PixelChangedEventArgs e)
    {
        if (_activeChangeSet is not null && ReferenceEquals(sender, _activeChangeSet.Document))
        {
            _activeChangeSet.Record(e);
        }
    }

    private readonly record struct PixelChange(
        int X,
        int Y,
        PixelColor PreviousColor,
        PixelColor Color);

    private sealed record PixelEdit(
        PixelDocument Document,
        PixelChange[] Changes,
        long PreviousStateId,
        long ResultStateId);

    private sealed class ActiveChangeSet
    {
        private readonly Dictionary<int, PixelChange> _changes = new();

        public ActiveChangeSet(PixelDocument document)
        {
            Document = document;
        }

        public PixelDocument Document { get; }

        public void Record(PixelChangedEventArgs change)
        {
            var index = (change.Y * Document.Width) + change.X;

            if (_changes.TryGetValue(index, out var existing))
            {
                _changes[index] = existing with { Color = change.Color };
                return;
            }

            _changes.Add(
                index,
                new PixelChange(
                    change.X,
                    change.Y,
                    change.PreviousColor,
                    change.Color));
        }

        public PixelChange[] CreateEdit()
        {
            return _changes.Values
                .Where(change => change.PreviousColor != change.Color)
                .ToArray();
        }
    }
}
