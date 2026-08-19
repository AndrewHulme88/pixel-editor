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

    [AvaloniaTheory]
    [InlineData(12, 34, 56, 255)]
    [InlineData(78, 90, 123, 127)]
    [InlineData(0, 0, 0, 0)]
    public void EyedropperClick_SamplesExactColorWithoutHistory(
        int red,
        int green,
        int blue,
        int alpha)
    {
        var (window, canvas, document, history) = ShowCanvas(EditorTool.Eyedropper);
        var sampledColor = new PixelColor(
            (byte)red,
            (byte)green,
            (byte)blue,
            (byte)alpha);
        document.SetPixel(1, 2, sampledColor);
        var originalStateId = history.CurrentStateId;
        var point = GetWindowPixelCentre(window, canvas, document, 1, 2);

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(sampledColor, canvas.BrushColor);
        Assert.Equal(sampledColor, document.GetPixel(1, 2));
        Assert.Equal(originalStateId, history.CurrentStateId);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        window.Close();
    }

    [AvaloniaFact]
    public void AltClick_TemporarilySamplesThenReturnsToSelectedTool()
    {
        var (window, canvas, document, history) = ShowCanvas(EditorTool.Brush);
        var sampledColor = new PixelColor(210, 120, 30, 140);
        document.SetPixel(1, 2, sampledColor);
        var samplePoint = GetWindowPixelCentre(window, canvas, document, 1, 2);
        var drawPoint = GetWindowPixelCentre(window, canvas, document, 3, 0);
        var samplingModifiers = RawInputModifiers.LeftMouseButton | RawInputModifiers.Alt;

        window.MouseDown(samplePoint, MouseButton.Left, samplingModifiers);
        window.MouseUp(samplePoint, MouseButton.Left, RawInputModifiers.Alt);

        Assert.Equal(sampledColor, canvas.BrushColor);
        Assert.Equal(EditorTool.Brush, canvas.ActiveTool);
        Assert.False(history.CanUndo);

        window.MouseDown(drawPoint, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(drawPoint, MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(sampledColor, document.GetPixel(3, 0));
        Assert.Equal(EditorTool.Brush, canvas.ActiveTool);
        Assert.True(history.CanUndo);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData((int)EditorTool.Rectangle)]
    [InlineData((int)EditorTool.Ellipse)]
    public void ShapeDrag_PreviewsThenCommitsOneUndoableOutline(int toolValue)
    {
        var tool = (EditorTool)toolValue;
        var (window, canvas, document, history) = ShowCanvas(tool, width: 8, height: 7);
        var start = GetWindowPixelCentre(window, canvas, document, 1, 1);
        var end = GetWindowPixelCentre(window, canvas, document, 6, 5);

        window.MouseDown(start, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseMove(end, RawInputModifiers.LeftMouseButton);

        AssertDocumentIsTransparent(document);
        Assert.False(history.CanUndo);

        window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);

        Assert.True(CountColoredPixels(document) > 0);
        Assert.Equal(PixelColor.Transparent, document.GetPixel(3, 3));
        Assert.True(history.CanUndo);
        Assert.True(history.Undo());
        AssertDocumentIsTransparent(document);
        Assert.False(history.CanUndo);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData((int)EditorTool.Rectangle)]
    [InlineData((int)EditorTool.Ellipse)]
    public void ShapeClick_PaintsOneSizedBrushStamp(int toolValue)
    {
        var tool = (EditorTool)toolValue;
        var (window, canvas, document, history) = ShowCanvas(
            tool,
            width: 9,
            height: 9,
            brushSize: 3);
        var point = GetWindowPixelCentre(window, canvas, document, 4, 4);

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(9, CountColoredPixels(document));
        Assert.Equal(BrushColor, document.GetPixel(3, 3));
        Assert.Equal(BrushColor, document.GetPixel(5, 5));
        Assert.True(history.CanUndo);
        window.Close();
    }

    private static (Window, PixelCanvas, PixelDocument, DocumentHistory) ShowCanvas(
        EditorTool tool,
        int width = 4,
        int height = 4,
        int brushSize = BrushTool.MinimumSize)
    {
        var document = new PixelDocument(width, height);
        var history = new DocumentHistory();
        var canvas = new PixelCanvas
        {
            Document = document,
            History = history,
            BrushColor = BrushColor,
            BrushSize = brushSize,
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

    private static int CountColoredPixels(PixelDocument document)
    {
        var count = 0;

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                if (document.GetPixel(x, y) != PixelColor.Transparent)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void AssertDocumentIsTransparent(PixelDocument document)
    {
        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                Assert.Equal(PixelColor.Transparent, document.GetPixel(x, y));
            }
        }
    }
}
