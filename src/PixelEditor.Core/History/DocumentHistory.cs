using PixelEditor.Core.Documents;

namespace PixelEditor.Core.History;

// Stores reversible pixel edits without copying the entire document.
public sealed class DocumentHistory
{
    public const long DefaultMemoryLimitBytes = 128L * 1024 * 1024;

    private const int EstimatedHistoryEntryOverheadBytes = 128;
    private const int PixelChangePayloadBytes = 16;
    private const int PixelSpanPayloadBytes = 12;

    private readonly LinkedList<HistoryEntry> _undoEntries = new();
    private readonly LinkedList<HistoryEntry> _redoEntries = new();
    private ActiveChangeSet? _activeChangeSet;
    private long _currentStateId;
    private long _estimatedMemoryUsageBytes;
    private long _nextStateId;

    public DocumentHistory(long memoryLimitBytes = DefaultMemoryLimitBytes)
    {
        if (memoryLimitBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(memoryLimitBytes),
                "The history memory limit must be greater than zero.");
        }

        MemoryLimitBytes = memoryLimitBytes;
    }

    public event EventHandler? Changed;

    public bool CanUndo => _activeChangeSet is null && _undoEntries.Count > 0;

    public bool CanRedo => _activeChangeSet is null && _redoEntries.Count > 0;

    public long MemoryLimitBytes { get; }

    // Estimates retained undo and redo payloads without counting active stroke recording.
    public long EstimatedMemoryUsageBytes => _estimatedMemoryUsageBytes;

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
            var wasRecorded = PushChange(changeSet.Document, new PixelHistoryChange(edit));
            Changed?.Invoke(this, EventArgs.Empty);
            return wasRecorded;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return false;
    }

    public bool RecordSpanChange(
        PixelDocument document,
        IReadOnlyList<PixelSpan> spans,
        PixelColor previousColor,
        PixelColor color)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(spans);

        if (_activeChangeSet is not null)
        {
            throw new InvalidOperationException(
                "A completed span change cannot be recorded while another change set is active.");
        }

        if (spans.Count == 0 || previousColor == color)
        {
            return false;
        }

        var recordedSpans = new PixelSpan[spans.Count];

        for (var index = 0; index < spans.Count; index++)
        {
            var span = spans[index];
            ValidateSpan(document, span);
            recordedSpans[index] = span;
        }

        var wasRecorded = PushChange(
            document,
            new UniformSpanHistoryChange(recordedSpans, previousColor, color));
        Changed?.Invoke(this, EventArgs.Empty);
        return wasRecorded;
    }

    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var edit = _undoEntries.Last!.Value;
        _undoEntries.RemoveLast();

        edit.Change.Undo(edit.Document);

        _redoEntries.AddLast(edit);
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

        var edit = _redoEntries.Last!.Value;
        _redoEntries.RemoveLast();

        edit.Change.Redo(edit.Document);

        _undoEntries.AddLast(edit);
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

        if (_undoEntries.Count == 0 && _redoEntries.Count == 0)
        {
            return;
        }

        _undoEntries.Clear();
        _redoEntries.Clear();
        _estimatedMemoryUsageBytes = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnPixelChanged(object? sender, PixelChangedEventArgs e)
    {
        if (_activeChangeSet is not null && ReferenceEquals(sender, _activeChangeSet.Document))
        {
            _activeChangeSet.Record(e);
        }
    }

    private bool PushChange(PixelDocument document, IHistoryChange change)
    {
        var nextStateId = checked(++_nextStateId);
        var entry = new HistoryEntry(
            document,
            change,
            _currentStateId,
            nextStateId,
            checked(EstimatedHistoryEntryOverheadBytes + change.EstimatedMemoryBytes));

        ClearEntries(_redoEntries);
        _currentStateId = nextStateId;

        if (entry.EstimatedMemoryBytes > MemoryLimitBytes)
        {
            ClearEntries(_undoEntries);
            return false;
        }

        _undoEntries.AddLast(entry);
        _estimatedMemoryUsageBytes = checked(
            _estimatedMemoryUsageBytes + entry.EstimatedMemoryBytes);

        while (_estimatedMemoryUsageBytes > MemoryLimitBytes)
        {
            var oldestEntry = _undoEntries.First!.Value;
            _undoEntries.RemoveFirst();
            _estimatedMemoryUsageBytes -= oldestEntry.EstimatedMemoryBytes;
        }

        return true;
    }

    private void ClearEntries(LinkedList<HistoryEntry> entries)
    {
        foreach (var entry in entries)
        {
            _estimatedMemoryUsageBytes -= entry.EstimatedMemoryBytes;
        }

        entries.Clear();
    }

    private static void ValidateSpan(PixelDocument document, PixelSpan span)
    {
        if (span.X < 0 ||
            (uint)span.Y >= (uint)document.Height ||
            span.Length <= 0 ||
            span.X > document.Width - span.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(span),
                "Every span must fit within the document.");
        }
    }

    private readonly record struct PixelChange(
        int X,
        int Y,
        PixelColor PreviousColor,
        PixelColor Color);

    private sealed record HistoryEntry(
        PixelDocument Document,
        IHistoryChange Change,
        long PreviousStateId,
        long ResultStateId,
        long EstimatedMemoryBytes);

    private interface IHistoryChange
    {
        long EstimatedMemoryBytes { get; }

        void Undo(PixelDocument document);

        void Redo(PixelDocument document);
    }

    private sealed record PixelHistoryChange(PixelChange[] Changes) : IHistoryChange
    {
        public long EstimatedMemoryBytes =>
            checked((long)Changes.Length * PixelChangePayloadBytes);

        public void Undo(PixelDocument document)
        {
            for (var index = Changes.Length - 1; index >= 0; index--)
            {
                var change = Changes[index];
                document.SetPixel(change.X, change.Y, change.PreviousColor);
            }
        }

        public void Redo(PixelDocument document)
        {
            foreach (var change in Changes)
            {
                document.SetPixel(change.X, change.Y, change.Color);
            }
        }
    }

    private sealed record UniformSpanHistoryChange(
        PixelSpan[] Spans,
        PixelColor PreviousColor,
        PixelColor Color) : IHistoryChange
    {
        public long EstimatedMemoryBytes =>
            checked((long)Spans.Length * PixelSpanPayloadBytes);

        public void Undo(PixelDocument document) =>
            document.ApplyPixelSpans(Spans, PreviousColor);

        public void Redo(PixelDocument document) =>
            document.ApplyPixelSpans(Spans, Color);
    }

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
