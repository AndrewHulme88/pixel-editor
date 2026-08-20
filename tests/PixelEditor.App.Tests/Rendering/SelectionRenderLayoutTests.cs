using Avalonia;
using pixel_editor.Rendering;
using PixelEditor.Core.Selections;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class SelectionRenderLayoutTests
{
    [Fact]
    public void Calculate_AlignsSelectionWithZoomedAndPannedPixelEdges()
    {
        var layout = CanvasLayout.Calculate(
            10,
            8,
            new Size(300, 240),
            pixelScale: 12,
            new Vector(17, -9));
        var bounds = new PixelSelectionBounds(2, 3, 4, 2);

        var result = SelectionRenderLayout.Calculate(bounds, layout);

        Assert.Equal(layout.Destination.X + 24, result.X);
        Assert.Equal(layout.Destination.Y + 36, result.Y);
        Assert.Equal(48, result.Width);
        Assert.Equal(24, result.Height);
    }
}
