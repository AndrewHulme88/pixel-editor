using Avalonia;
using Avalonia.Media;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CheckerboardBrushCacheTests
{
    [Fact]
    public void GetBrush_ReturnsCachedBrush()
    {
        var cache = new CheckerboardBrushCache();

        var first = cache.GetBrush();
        var second = cache.GetBrush();

        Assert.Same(first, second);
    }

    [Fact]
    public void GetBrush_UsesTwoByTwoDocumentPixelTile()
    {
        var cache = new CheckerboardBrushCache();

        var brush = cache.GetBrush();

        Assert.Equal(
            new RelativeRect(new Rect(0, 0, 2, 2), RelativeUnit.Absolute),
            brush.DestinationRect);
        Assert.Equal(
            new RelativeRect(new Rect(0, 0, 2, 2), RelativeUnit.Absolute),
            brush.SourceRect);
        Assert.Equal(AlignmentX.Left, brush.AlignmentX);
        Assert.Equal(AlignmentY.Top, brush.AlignmentY);
        Assert.Equal(Stretch.Fill, brush.Stretch);
        Assert.Equal(TileMode.Tile, brush.TileMode);

        var tile = Assert.IsType<DrawingGroup>(brush.Drawing);
        Assert.Collection(
            tile.Children,
            drawing => AssertRectangle(
                drawing,
                new Rect(0, 0, 2, 2),
                Color.FromRgb(214, 214, 214)),
            drawing => AssertRectangle(
                drawing,
                new Rect(1, 0, 1, 1),
                Color.FromRgb(174, 174, 174)),
            drawing => AssertRectangle(
                drawing,
                new Rect(0, 1, 1, 1),
                Color.FromRgb(174, 174, 174)));
    }

    private static void AssertRectangle(Drawing drawing, Rect expectedBounds, Color expectedColor)
    {
        var geometryDrawing = Assert.IsType<GeometryDrawing>(drawing);
        var rectangle = Assert.IsType<RectangleGeometry>(geometryDrawing.Geometry);
        var brush = Assert.IsType<SolidColorBrush>(geometryDrawing.Brush);
        Assert.Equal(expectedBounds, rectangle.Rect);
        Assert.Equal(expectedColor, brush.Color);
    }
}
