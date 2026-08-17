using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.Core.Tests.Documents;

public sealed class PixelDocumentResizerTests
{
    private static readonly PixelColor Red = new(220, 50, 50);
    private static readonly PixelColor Green = new(50, 220, 80);
    private static readonly PixelColor Blue = new(50, 100, 220);
    private static readonly PixelColor Yellow = new(240, 210, 40);

    [Theory]
    [InlineData(CanvasAnchor.TopLeft, 0, 0)]
    [InlineData(CanvasAnchor.Top, 1, 0)]
    [InlineData(CanvasAnchor.TopRight, 2, 0)]
    [InlineData(CanvasAnchor.Left, 0, 1)]
    [InlineData(CanvasAnchor.Center, 1, 1)]
    [InlineData(CanvasAnchor.Right, 2, 1)]
    [InlineData(CanvasAnchor.BottomLeft, 0, 2)]
    [InlineData(CanvasAnchor.Bottom, 1, 2)]
    [InlineData(CanvasAnchor.BottomRight, 2, 2)]
    public void Resize_WhenGrowing_PositionsExistingPixelsFromAnchor(
        CanvasAnchor anchor,
        int expectedX,
        int expectedY)
    {
        var source = CreateTwoByTwoDocument();

        var resized = PixelDocumentResizer.Resize(source, 4, 4, anchor);

        Assert.Equal(Red, resized.GetPixel(expectedX, expectedY));
        Assert.Equal(Green, resized.GetPixel(expectedX + 1, expectedY));
        Assert.Equal(Blue, resized.GetPixel(expectedX, expectedY + 1));
        Assert.Equal(Yellow, resized.GetPixel(expectedX + 1, expectedY + 1));
        Assert.Equal(12, CountPixels(resized, PixelColor.Transparent));
    }

    [Theory]
    [InlineData(CanvasAnchor.TopLeft, 0, 0)]
    [InlineData(CanvasAnchor.Center, 1, 1)]
    [InlineData(CanvasAnchor.BottomRight, 2, 2)]
    public void Resize_WhenShrinking_CropsPixelsFromOppositeEdges(
        CanvasAnchor anchor,
        int expectedSourceX,
        int expectedSourceY)
    {
        var source = CreateCoordinateDocument(4, 4);

        var resized = PixelDocumentResizer.Resize(source, 2, 2, anchor);

        for (var y = 0; y < resized.Height; y++)
        {
            for (var x = 0; x < resized.Width; x++)
            {
                Assert.Equal(
                    source.GetPixel(expectedSourceX + x, expectedSourceY + y),
                    resized.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Resize_ToSameDimensions_CopiesAllPixels()
    {
        var source = CreateCoordinateDocument(3, 2);

        var resized = PixelDocumentResizer.Resize(source, 3, 2, CanvasAnchor.Center);

        Assert.NotSame(source, resized);

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                Assert.Equal(source.GetPixel(x, y), resized.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Resize_WithInvalidAnchor_Throws()
    {
        var source = new PixelDocument(1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PixelDocumentResizer.Resize(source, 2, 2, (CanvasAnchor)99));
    }

    private static PixelDocument CreateTwoByTwoDocument()
    {
        var document = new PixelDocument(2, 2);
        document.SetPixel(0, 0, Red);
        document.SetPixel(1, 0, Green);
        document.SetPixel(0, 1, Blue);
        document.SetPixel(1, 1, Yellow);
        return document;
    }

    private static PixelDocument CreateCoordinateDocument(int width, int height)
    {
        var document = new PixelDocument(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                document.SetPixel(x, y, new PixelColor((byte)(x + 1), (byte)(y + 1), 100));
            }
        }

        return document;
    }

    private static int CountPixels(PixelDocument document, PixelColor color)
    {
        var count = 0;

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                if (document.GetPixel(x, y) == color)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
