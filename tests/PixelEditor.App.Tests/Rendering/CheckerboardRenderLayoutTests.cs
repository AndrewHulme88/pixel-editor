using Avalonia;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CheckerboardRenderLayoutTests
{
    [Fact]
    public void Calculate_AlignsDocumentCoordinatesWithCanvasPixels()
    {
        var canvasLayout = new CanvasLayoutResult(
            new Rect(13.5, 27.25, 40, 30),
            10);

        var checkerboardLayout = CheckerboardRenderLayout.Calculate(canvasLayout);
        var pixelBounds = CanvasPixelGrid.GetPixelBounds(canvasLayout, 2, 1);
        var checkerTopLeft = new Point(2, 1).Transform(checkerboardLayout.DocumentToScreen);
        var checkerBottomRight = new Point(3, 2).Transform(checkerboardLayout.DocumentToScreen);

        Assert.Equal(new Rect(0, 0, 4, 3), checkerboardLayout.DocumentBounds);
        Assert.Equal(pixelBounds.TopLeft, checkerTopLeft);
        Assert.Equal(pixelBounds.BottomRight, checkerBottomRight);
    }
}
