using Avalonia;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CanvasViewportTests
{
    [Theory]
    [InlineData(1, true, 2)]
    [InlineData(4, true, 6)]
    [InlineData(20, true, 24)]
    [InlineData(24, false, 16)]
    [InlineData(20, false, 16)]
    [InlineData(1, false, 1)]
    public void GetNextScale_ReturnsDiscretePixelScale(
        double current,
        bool zoomIn,
        double expected)
    {
        Assert.Equal(expected, CanvasViewport.GetNextScale(current, zoomIn));
    }

    [Fact]
    public void ZoomAt_KeepsAnchoredDocumentPositionUnderPointer()
    {
        var availableSize = new Size(500, 320);
        var current = CanvasLayout.Calculate(
            16,
            12,
            availableSize,
            16,
            new Vector(35, -20));
        var anchor = new Point(310, 125);
        var documentX = (anchor.X - current.Destination.X) / current.PixelScale;
        var documentY = (anchor.Y - current.Destination.Y) / current.PixelScale;

        var viewport = CanvasViewport.ZoomAt(
            current,
            16,
            12,
            availableSize,
            anchor,
            zoomIn: true);
        var zoomed = CanvasLayout.Calculate(
            16,
            12,
            availableSize,
            viewport.PixelScale,
            viewport.PanOffset);

        Assert.Equal(documentX, (anchor.X - zoomed.Destination.X) / zoomed.PixelScale, 10);
        Assert.Equal(documentY, (anchor.Y - zoomed.Destination.Y) / zoomed.PixelScale, 10);
    }

    [Fact]
    public void ZoomAt_FromCentredLayoutAroundCentre_RemainsCentred()
    {
        var availableSize = new Size(400, 300);
        var current = CanvasLayout.Calculate(10, 10, availableSize, 16, default);

        var viewport = CanvasViewport.ZoomAt(
            current,
            10,
            10,
            availableSize,
            new Point(200, 150),
            zoomIn: true);

        Assert.Equal(24, viewport.PixelScale);
        Assert.Equal(default, viewport.PanOffset);
    }
}
