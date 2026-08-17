using Avalonia;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CanvasCoordinateMapperTests
{
    private static readonly CanvasLayoutResult Layout =
        CanvasLayout.Calculate(4, 3, new Size(100, 80));

    [Fact]
    public void TryMap_AtTopLeftEdge_ReturnsFirstPixel()
    {
        var wasMapped = CanvasCoordinateMapper.TryMap(
            new Point(0, 2.5),
            Layout,
            4,
            3,
            out var coordinate);

        Assert.True(wasMapped);
        Assert.Equal(new PixelCoordinate(0, 0), coordinate);
    }

    [Fact]
    public void TryMap_UsesIntegerPixelScale()
    {
        var wasMapped = CanvasCoordinateMapper.TryMap(
            new Point(25, 27.5),
            Layout,
            4,
            3,
            out var coordinate);

        Assert.True(wasMapped);
        Assert.Equal(new PixelCoordinate(1, 1), coordinate);
    }

    [Fact]
    public void TryMap_JustInsideBottomRightEdge_ReturnsLastPixel()
    {
        var wasMapped = CanvasCoordinateMapper.TryMap(
            new Point(99.999, 77.499),
            Layout,
            4,
            3,
            out var coordinate);

        Assert.True(wasMapped);
        Assert.Equal(new PixelCoordinate(3, 2), coordinate);
    }

    [Theory]
    [InlineData(-0.001, 40)]
    [InlineData(100, 40)]
    [InlineData(50, 2.499)]
    [InlineData(50, 77.5)]
    public void TryMap_OutsideDocument_ReturnsFalse(double x, double y)
    {
        var wasMapped = CanvasCoordinateMapper.TryMap(
            new Point(x, y),
            Layout,
            4,
            3,
            out _);

        Assert.False(wasMapped);
    }

    [Fact]
    public void TryMap_WithClippedDocument_AccountsForNegativeOrigin()
    {
        var clippedLayout = CanvasLayout.Calculate(200, 100, new Size(100, 80));

        var wasMapped = CanvasCoordinateMapper.TryMap(
            new Point(0, 0),
            clippedLayout,
            200,
            100,
            out var coordinate);

        Assert.True(wasMapped);
        Assert.Equal(new PixelCoordinate(50, 10), coordinate);
    }

    [Fact]
    public void TryMap_WithPannedLayout_AccountsForViewportOffset()
    {
        var pannedLayout = CanvasLayout.Calculate(
            4,
            3,
            new Size(100, 80),
            10,
            new Vector(18, -7));
        var pointer = new Point(
            pannedLayout.Destination.X + 25,
            pannedLayout.Destination.Y + 15);

        var wasMapped = CanvasCoordinateMapper.TryMap(
            pointer,
            pannedLayout,
            4,
            3,
            out var coordinate);

        Assert.True(wasMapped);
        Assert.Equal(new PixelCoordinate(2, 1), coordinate);
    }
}
