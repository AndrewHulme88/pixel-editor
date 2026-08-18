using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using pixel_editor.Controls;
using pixel_editor.Rendering;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.Ui.Tests;

public sealed class CanvasDrawingWorkflowTests
{
    private static readonly PixelColor BrushColor = new(20, 80, 160);

    [AvaloniaFact]
    public void LeftDrag_DrawsContinuousUndoableStroke()
    {
        var (window, canvas, document, history) = ShowCanvas(EditorTool.Brush);

        var start = GetWindowPixelCentre(window, canvas, document, 0, 1);
        var end = GetWindowPixelCentre(window, canvas, document, 3, 1);

        window.MouseDown(start, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);

        for (var x = 0; x < document.Width; x++)
        {
            Assert.Equal(BrushColor, document.GetPixel(x, 1));
        }

        Assert.True(history.CanUndo);
        Assert.True(history.Undo());

        for (var x = 0; x < document.Width; x++)
        {
            Assert.Equal(PixelColor.Transparent, document.GetPixel(x, 1));
        }

        window.Close();
    }

    [AvaloniaFact]
    public void ShiftDrag_PaintsStraightLineOnlyWhenReleased()
    {
        var (window, canvas, document, history) = ShowCanvas(EditorTool.Brush);
        var start = GetWindowPixelCentre(window, canvas, document, 0, 0);
        var end = GetWindowPixelCentre(window, canvas, document, 3, 3);
        var drawingModifiers = RawInputModifiers.LeftMouseButton | RawInputModifiers.Shift;

        window.MouseDown(start, MouseButton.Left, drawingModifiers);
        window.MouseMove(end, drawingModifiers);

        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 0));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(3, 3));

        window.MouseUp(end, MouseButton.Left, RawInputModifiers.Shift);

        for (var coordinate = 0; coordinate < document.Width; coordinate++)
        {
            Assert.Equal(BrushColor, document.GetPixel(coordinate, coordinate));
        }

        Assert.True(history.CanUndo);
        window.Close();
    }

    [AvaloniaFact]
    public void FillClick_FillsRegionAsOneUndoableAction()
    {
        var (window, canvas, document, history) = ShowCanvas(EditorTool.Fill);
        var point = GetWindowPixelCentre(window, canvas, document, 1, 1);

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                Assert.Equal(BrushColor, document.GetPixel(x, y));
            }
        }

        Assert.True(history.CanUndo);
        Assert.True(history.Undo());
        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 1));
        window.Close();
    }

    private static (Window, PixelCanvas, PixelDocument, DocumentHistory) ShowCanvas(
        EditorTool tool)
    {
        var document = new PixelDocument(4, 4);
        var history = new DocumentHistory();
        var canvas = new PixelCanvas
        {
            Document = document,
            History = history,
            BrushColor = BrushColor,
            ActiveTool = tool
        };
        var window = new Window
        {
            Width = 200,
            Height = 200,
            Content = canvas
        };
        window.Show();
        return (window, canvas, document, history);
    }

    private static Point GetWindowPixelCentre(
        Window window,
        PixelCanvas canvas,
        PixelDocument document,
        int x,
        int y)
    {
        var layout = CanvasLayout.Calculate(
            document.Width,
            document.Height,
            canvas.Bounds.Size);
        var localPoint = CanvasPixelGrid.GetPixelBounds(layout, x, y).Center;
        return canvas.TranslatePoint(localPoint, window)
            ?? throw new InvalidOperationException("Canvas position could not be mapped to the window.");
    }
}
