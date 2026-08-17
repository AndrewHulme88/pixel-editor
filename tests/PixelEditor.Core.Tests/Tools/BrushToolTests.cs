using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.Core.Tests.Tools;

public sealed class BrushToolTests
{
    private static readonly PixelColor Color = new(10, 20, 30);

    [Fact]
    public void DrawLine_WithSinglePoint_PaintsOnePixel()
    {
        var document = new PixelDocument(5, 5);

        BrushTool.DrawLine(document, 2, 3, 2, 3, Color);

        Assert.Equal(Color, document.GetPixel(2, 3));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(2, 2));
    }

    [Fact]
    public void DrawLine_Horizontally_PaintsEveryPixelIncludingEndpoints()
    {
        var document = new PixelDocument(6, 3);

        BrushTool.DrawLine(document, 1, 1, 4, 1, Color);

        for (var x = 1; x <= 4; x++)
        {
            Assert.Equal(Color, document.GetPixel(x, 1));
        }

        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 1));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(5, 1));
    }

    [Fact]
    public void DrawLine_Diagonally_PaintsContinuousLine()
    {
        var document = new PixelDocument(6, 6);

        BrushTool.DrawLine(document, 1, 1, 4, 4, Color);

        Assert.Equal(Color, document.GetPixel(1, 1));
        Assert.Equal(Color, document.GetPixel(2, 2));
        Assert.Equal(Color, document.GetPixel(3, 3));
        Assert.Equal(Color, document.GetPixel(4, 4));
    }

    [Fact]
    public void DrawLine_Backwards_PaintsContinuousLine()
    {
        var document = new PixelDocument(6, 6);

        BrushTool.DrawLine(document, 4, 1, 1, 4, Color);

        Assert.Equal(Color, document.GetPixel(4, 1));
        Assert.Equal(Color, document.GetPixel(3, 2));
        Assert.Equal(Color, document.GetPixel(2, 3));
        Assert.Equal(Color, document.GetPixel(1, 4));
    }

    [Fact]
    public void DrawLine_WithTransparentColor_ErasesContinuousLine()
    {
        var document = new PixelDocument(6, 3);

        for (var x = 0; x < document.Width; x++)
        {
            document.SetPixel(x, 1, Color);
        }

        BrushTool.DrawLine(document, 1, 1, 4, 1, PixelColor.Transparent);

        for (var x = 1; x <= 4; x++)
        {
            Assert.Equal(PixelColor.Transparent, document.GetPixel(x, 1));
        }

        Assert.Equal(Color, document.GetPixel(0, 1));
        Assert.Equal(Color, document.GetPixel(5, 1));
    }

    [Fact]
    public void DrawLine_WithSizedSinglePoint_PaintsSquareStamp()
    {
        var document = new PixelDocument(7, 7);

        BrushTool.DrawLine(document, 3, 3, 3, 3, Color, size: 3);

        for (var y = 2; y <= 4; y++)
        {
            for (var x = 2; x <= 4; x++)
            {
                Assert.Equal(Color, document.GetPixel(x, y));
            }
        }

        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 3));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(5, 3));
    }

    [Fact]
    public void DrawLine_WithSizedBrush_ClipsStampAtDocumentEdges()
    {
        var document = new PixelDocument(4, 4);

        BrushTool.DrawLine(document, 0, 0, 0, 0, Color, size: 3);

        Assert.Equal(Color, document.GetPixel(0, 0));
        Assert.Equal(Color, document.GetPixel(1, 0));
        Assert.Equal(Color, document.GetPixel(0, 1));
        Assert.Equal(Color, document.GetPixel(1, 1));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(2, 0));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 2));
    }

    [Fact]
    public void DrawLine_WithSizedBrush_PaintsContinuousThickStroke()
    {
        var document = new PixelDocument(9, 7);

        BrushTool.DrawLine(document, 2, 3, 6, 3, Color, size: 3);

        for (var y = 2; y <= 4; y++)
        {
            for (var x = 1; x <= 7; x++)
            {
                Assert.Equal(Color, document.GetPixel(x, y));
            }
        }

        Assert.Equal(PixelColor.Transparent, document.GetPixel(0, 3));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(8, 3));
    }

    [Fact]
    public void DrawLine_WithSizedTransparentBrush_ErasesSquareStamp()
    {
        var document = new PixelDocument(5, 5);

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                document.SetPixel(x, y, Color);
            }
        }

        BrushTool.DrawLine(document, 2, 2, 2, 2, PixelColor.Transparent, size: 3);

        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 1));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(2, 2));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(3, 3));
        Assert.Equal(Color, document.GetPixel(0, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    public void DrawLine_WithInvalidSize_Throws(int size)
    {
        var document = new PixelDocument(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BrushTool.DrawLine(document, 2, 2, 2, 2, Color, size));
    }
}
