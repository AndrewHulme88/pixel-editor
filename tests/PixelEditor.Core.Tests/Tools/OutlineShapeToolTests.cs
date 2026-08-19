using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.Core.Tests.Tools;

public sealed class OutlineShapeToolTests
{
    private static readonly PixelColor Color = new(35, 90, 170, 210);

    [Fact]
    public void DrawRectangle_PaintsOutlineAndLeavesInteriorTransparent()
    {
        var document = new PixelDocument(7, 6);

        OutlineShapeTool.DrawRectangle(document, 1, 1, 5, 4, Color);

        AssertRectangleOutline(document, 1, 1, 5, 4);
        Assert.Equal(PixelColor.Transparent, document.GetPixel(3, 2));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(3, 3));
    }

    [Fact]
    public void DrawRectangle_WithReverseDragMatchesForwardDrag()
    {
        var forward = new PixelDocument(8, 7);
        var reverse = new PixelDocument(8, 7);

        OutlineShapeTool.DrawRectangle(forward, 1, 2, 6, 5, Color, size: 2);
        OutlineShapeTool.DrawRectangle(reverse, 6, 5, 1, 2, Color, size: 2);

        AssertDocumentsEqual(forward, reverse);
    }

    [Fact]
    public void DrawRectangle_WithSinglePointPaintsOneBrushStamp()
    {
        var document = new PixelDocument(7, 7);

        OutlineShapeTool.DrawRectangle(document, 3, 3, 3, 3, Color, size: 3);

        for (var y = 2; y <= 4; y++)
        {
            for (var x = 2; x <= 4; x++)
            {
                Assert.Equal(Color, document.GetPixel(x, y));
            }
        }

        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 3));
    }

    [Fact]
    public void DrawRectangle_WithThickBrushClipsAtDocumentEdges()
    {
        var document = new PixelDocument(7, 7);

        OutlineShapeTool.DrawRectangle(document, 0, 0, 6, 6, Color, size: 3);

        Assert.Equal(Color, document.GetPixel(0, 0));
        Assert.Equal(Color, document.GetPixel(1, 3));
        Assert.Equal(Color, document.GetPixel(5, 3));
        Assert.Equal(Color, document.GetPixel(3, 5));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(3, 3));
    }

    [Fact]
    public void DrawEllipse_PaintsCardinalPointsAndLeavesInteriorTransparent()
    {
        var document = new PixelDocument(9, 7);

        OutlineShapeTool.DrawEllipse(document, 1, 1, 7, 5, Color);

        Assert.Equal(Color, document.GetPixel(4, 1));
        Assert.Equal(Color, document.GetPixel(4, 5));
        Assert.Equal(Color, document.GetPixel(1, 3));
        Assert.Equal(Color, document.GetPixel(7, 3));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(4, 3));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 1));
    }

    [Fact]
    public void DrawEllipse_WithReverseDragMatchesForwardDrag()
    {
        var forward = new PixelDocument(10, 8);
        var reverse = new PixelDocument(10, 8);

        OutlineShapeTool.DrawEllipse(forward, 1, 1, 8, 6, Color, size: 2);
        OutlineShapeTool.DrawEllipse(reverse, 8, 6, 1, 1, Color, size: 2);

        AssertDocumentsEqual(forward, reverse);
    }

    [Fact]
    public void DrawEllipse_WithSinglePointPaintsOneBrushStamp()
    {
        var document = new PixelDocument(5, 5);

        OutlineShapeTool.DrawEllipse(document, 2, 2, 2, 2, Color, size: 3);

        Assert.Equal(Color, document.GetPixel(1, 1));
        Assert.Equal(Color, document.GetPixel(2, 2));
        Assert.Equal(Color, document.GetPixel(3, 3));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 2));
    }

    [Theory]
    [InlineData(2, 1, 2, 5)]
    [InlineData(1, 3, 5, 3)]
    public void DrawEllipse_WithOnePixelAxisPaintsLine(
        int startX,
        int startY,
        int endX,
        int endY)
    {
        var document = new PixelDocument(7, 7);

        OutlineShapeTool.DrawEllipse(
            document,
            startX,
            startY,
            endX,
            endY,
            Color);

        var left = Math.Min(startX, endX);
        var right = Math.Max(startX, endX);
        var top = Math.Min(startY, endY);
        var bottom = Math.Max(startY, endY);

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                Assert.Equal(Color, document.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void DrawEllipse_AtMaximumBoundsUsesSafeArithmetic()
    {
        var maximum = PixelDocumentLimits.MaximumDimension;
        var document = new PixelDocument(maximum, maximum);
        var centreLow = (maximum / 2) - 1;
        var centreHigh = maximum / 2;

        OutlineShapeTool.DrawEllipse(
            document,
            0,
            0,
            maximum - 1,
            maximum - 1,
            Color);

        Assert.True(
            document.GetPixel(centreLow, 0) == Color ||
            document.GetPixel(centreHigh, 0) == Color);
        Assert.True(
            document.GetPixel(0, centreLow) == Color ||
            document.GetPixel(0, centreHigh) == Color);
        Assert.Equal(PixelColor.Transparent, document.GetPixel(centreLow, centreLow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    public void DrawShapes_WithInvalidBrushSizeThrow(int size)
    {
        var document = new PixelDocument(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutlineShapeTool.DrawRectangle(document, 1, 1, 3, 3, Color, size));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutlineShapeTool.DrawEllipse(document, 1, 1, 3, 3, Color, size));
    }

    private static void AssertRectangleOutline(
        PixelDocument document,
        int left,
        int top,
        int right,
        int bottom)
    {
        for (var x = left; x <= right; x++)
        {
            Assert.Equal(Color, document.GetPixel(x, top));
            Assert.Equal(Color, document.GetPixel(x, bottom));
        }

        for (var y = top; y <= bottom; y++)
        {
            Assert.Equal(Color, document.GetPixel(left, y));
            Assert.Equal(Color, document.GetPixel(right, y));
        }
    }

    private static void AssertDocumentsEqual(PixelDocument expected, PixelDocument actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
            }
        }
    }
}
