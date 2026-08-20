using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.Core.Tests.Tools;

public sealed class FilledShapeToolTests
{
    private static readonly PixelColor Color = new(45, 110, 185, 190);

    [Fact]
    public void CreateRectangleSpans_CoversInclusiveBounds()
    {
        var document = new PixelDocument(8, 7);

        var spans = FilledShapeTool.CreateRectangleSpans(document, 1, 2, 6, 5);

        Assert.Equal(4, spans.Count);

        for (var index = 0; index < spans.Count; index++)
        {
            Assert.Equal(new PixelSpan(1, 2 + index, 6), spans[index]);
        }
    }

    [Fact]
    public void CreateRectangleSpans_WithReverseDragMatchesForwardDrag()
    {
        var document = new PixelDocument(8, 7);

        var forward = FilledShapeTool.CreateRectangleSpans(document, 1, 2, 6, 5);
        var reverse = FilledShapeTool.CreateRectangleSpans(document, 6, 5, 1, 2);

        Assert.Equal(forward, reverse);
    }

    [Fact]
    public void CreateEllipseSpans_FillsInteriorAndLeavesCornersTransparent()
    {
        var document = new PixelDocument(9, 7);
        var spans = FilledShapeTool.CreateEllipseSpans(document, 1, 1, 7, 5);

        document.SetPixelSpans(spans, Color);

        Assert.Equal(Color, document.GetPixel(4, 1));
        Assert.Equal(Color, document.GetPixel(1, 3));
        Assert.Equal(Color, document.GetPixel(4, 3));
        Assert.Equal(Color, document.GetPixel(7, 3));
        Assert.Equal(Color, document.GetPixel(4, 5));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 1));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(7, 5));
    }

    [Fact]
    public void CreateEllipseSpans_WithReverseDragMatchesForwardDrag()
    {
        var document = new PixelDocument(10, 8);

        var forward = FilledShapeTool.CreateEllipseSpans(document, 1, 1, 8, 6);
        var reverse = FilledShapeTool.CreateEllipseSpans(document, 8, 6, 1, 1);

        Assert.Equal(forward, reverse);
    }

    [Theory]
    [InlineData(2, 2, 2, 2, 1)]
    [InlineData(2, 1, 2, 5, 5)]
    [InlineData(1, 3, 5, 3, 1)]
    public void CreateEllipseSpans_HandlesDegenerateBounds(
        int startX,
        int startY,
        int endX,
        int endY,
        int expectedSpanCount)
    {
        var document = new PixelDocument(7, 7);

        var spans = FilledShapeTool.CreateEllipseSpans(
            document,
            startX,
            startY,
            endX,
            endY);

        Assert.Equal(expectedSpanCount, spans.Count);
        document.SetPixelSpans(spans, Color);
        Assert.Equal(Color, document.GetPixel(startX, startY));
        Assert.Equal(Color, document.GetPixel(endX, endY));
    }

    [Fact]
    public void CreateFilledShapeSpans_AtDocumentEdgesStayWithinBounds()
    {
        var document = new PixelDocument(12, 10);
        var rectangle = FilledShapeTool.CreateRectangleSpans(document, 0, 0, 11, 9);
        var ellipse = FilledShapeTool.CreateEllipseSpans(document, 0, 0, 11, 9);

        document.SetPixelSpans(rectangle, Color);
        document.SetPixelSpans(ellipse, Color);

        Assert.All(rectangle, span => AssertSpanFits(document, span));
        Assert.All(ellipse, span => AssertSpanFits(document, span));
    }

    private static void AssertSpanFits(PixelDocument document, PixelSpan span)
    {
        Assert.InRange(span.X, 0, document.Width - 1);
        Assert.InRange(span.Y, 0, document.Height - 1);
        Assert.InRange(span.Length, 1, document.Width - span.X);
    }
}
