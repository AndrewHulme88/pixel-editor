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
}
