using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;

namespace pixel_editor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private long? _savedHistoryStateId;

    public MainViewModel()
    {
        Document = CreateSampleDocument();
        History = new DocumentHistory();
        _savedHistoryStateId = History.CurrentStateId;
        History.Changed += OnHistoryChanged;
    }

    [ObservableProperty]
    public partial PixelDocument Document { get; private set; }

    public DocumentHistory History { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial string DocumentName { get; private set; } = "Untitled";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial bool IsDirty { get; private set; }

    [ObservableProperty]
    public partial PixelColor BrushColor { get; set; } = new(49, 130, 206);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBrushSelected))]
    [NotifyPropertyChangedFor(nameof(IsEraserSelected))]
    public partial EditorTool ActiveTool { get; set; } = EditorTool.Brush;

    public bool IsBrushSelected => ActiveTool == EditorTool.Brush;

    public bool IsEraserSelected => ActiveTool == EditorTool.Eraser;

    public bool CanUndo => History.CanUndo;

    public bool CanRedo => History.CanRedo;

    public string WindowTitle => $"{DocumentName}{(IsDirty ? "*" : string.Empty)} - Pixel Editor";

    [RelayCommand]
    private void SelectBrush() => ActiveTool = EditorTool.Brush;

    [RelayCommand]
    private void SelectEraser() => ActiveTool = EditorTool.Eraser;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => History.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => History.Redo();

    public void ReplaceDocument(PixelDocument document, string documentName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        History.Clear();
        Document = document;
        DocumentName = documentName;
        _savedHistoryStateId = History.CurrentStateId;
        UpdateDirtyState();
    }

    public void CreateNewDocument(int width, int height)
    {
        var document = new PixelDocument(width, height);

        History.Clear();
        Document = document;
        DocumentName = "Untitled";
        _savedHistoryStateId = null;
        UpdateDirtyState();
    }

    public void MarkDocumentSaved(string documentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        DocumentName = documentName;
        _savedHistoryStateId = History.CurrentStateId;
        UpdateDirtyState();
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        UpdateDirtyState();
    }

    private void UpdateDirtyState() =>
        IsDirty = _savedHistoryStateId is null || History.CurrentStateId != _savedHistoryStateId;

    private static PixelDocument CreateSampleDocument()
    {
        var document = new PixelDocument(16, 16);
        var yellow = new PixelColor(245, 196, 66);
        var dark = new PixelColor(55, 46, 40);
        var red = new PixelColor(211, 72, 65);

        for (var y = 3; y <= 12; y++)
        {
            for (var x = 3; x <= 12; x++)
            {
                var isCorner = (x is 3 or 12) && (y is 3 or 12);
                if (!isCorner)
                {
                    document.SetPixel(x, y, yellow);
                }
            }
        }

        document.SetPixel(6, 6, dark);
        document.SetPixel(9, 6, dark);

        for (var x = 6; x <= 9; x++)
        {
            document.SetPixel(x, 10, red);
        }

        document.SetPixel(5, 9, red);
        document.SetPixel(10, 9, red);

        return document;
    }
}
