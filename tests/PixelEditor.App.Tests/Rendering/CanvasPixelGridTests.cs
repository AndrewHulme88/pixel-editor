using Avalonia;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CanvasPixelGridTests
{
    [Fact]
    public void GetPixelBounds_UsesLayoutOriginAndPixelScale()
    {
        var layout = new CanvasLayoutResult(
            new Rect(1.5, 15.5, 100, 50),
            10);

        var bounds = CanvasPixelGrid.GetPixelBounds(layout, 2, 3);

        Assert.Equal(new Rect(21.5, 45.5, 10, 10), bounds);
    }

    [Fact]
    public void GetPixelBounds_ForLastPixel_EndsAtDocumentEdge()
    {
        var layout = CanvasLayout.Calculate(16, 16, new Size(512, 320));

        var bounds = CanvasPixelGrid.GetPixelBounds(layout, 15, 15);

        Assert.Equal(layout.Destination.Right, bounds.Right);
        Assert.Equal(layout.Destination.Bottom, bounds.Bottom);
    }

    [Fact]
    public void GetBrushBounds_UsesFullBrushFootprint()
    {
        var layout = new CanvasLayoutResult(new Rect(20, 10, 70, 70), 10);

        var bounds = CanvasPixelGrid.GetBrushBounds(layout, 3, 3, 3, 7, 7);

        Assert.Equal(new Rect(40, 30, 30, 30), bounds);
    }

    [Fact]
    public void GetBrushBounds_ClipsFootprintAtDocumentEdge()
    {
        var layout = new CanvasLayoutResult(new Rect(20, 10, 70, 70), 10);

        var bounds = CanvasPixelGrid.GetBrushBounds(layout, 0, 0, 3, 7, 7);

        Assert.Equal(new Rect(20, 10, 20, 20), bounds);
    }
}
