using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;

namespace pixel_editor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Document = CreateSampleDocument();
        History = new DocumentHistory();
        History.Changed += OnHistoryChanged;
    }

    public PixelDocument Document { get; }

    public DocumentHistory History { get; }

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

    [RelayCommand]
    private void SelectBrush() => ActiveTool = EditorTool.Brush;

    [RelayCommand]
    private void SelectEraser() => ActiveTool = EditorTool.Eraser;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => History.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => History.Redo();

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

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
