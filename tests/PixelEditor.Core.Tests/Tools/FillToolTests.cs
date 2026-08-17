using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.Core.Tests.Tools;

public sealed class FillToolTests
{
    private static readonly PixelColor BorderColor = new(40, 50, 60);
    private static readonly PixelColor FillColor = new(10, 120, 220, 180);

    [Fact]
    public void Fill_InsideBorder_ReplacesOnlyEnclosedRegion()
    {
        var document = CreateBorderedDocument(5, 5);

        var result = FillTool.Fill(document, 2, 2, FillColor);

        Assert.Equal(9, result.FilledPixelCount);
        Assert.Equal(3, result.Spans.Count);
        Assert.Equal(PixelColor.Transparent, result.PreviousColor);
        Assert.Equal(FillColor, result.Color);

        for (var y = 1; y <= 3; y++)
        {
            for (var x = 1; x <= 3; x++)
            {
                Assert.Equal(FillColor, document.GetPixel(x, y));
            }
        }

        Assert.Equal(BorderColor, document.GetPixel(0, 0));
        Assert.Equal(BorderColor, document.GetPixel(4, 4));
    }

    [Fact]
    public void Fill_WithDiagonalConnection_DoesNotCrossCorner()
    {
        var document = new PixelDocument(3, 3);

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                document.SetPixel(x, y, BorderColor);
            }
        }

        document.SetPixel(0, 0, PixelColor.Transparent);
        document.SetPixel(1, 1, PixelColor.Transparent);

        var result = FillTool.Fill(document, 0, 0, FillColor);

        Assert.Equal(1, result.FilledPixelCount);
        Assert.Equal(FillColor, document.GetPixel(0, 0));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 1));
    }

    [Fact]
    public void Fill_OpenRegion_ReplacesEveryConnectedPixel()
    {
        var document = new PixelDocument(8, 6);

        var notificationCount = 0;
        document.PixelSpansChanged += (_, change) =>
        {
            notificationCount++;
            Assert.Equal(FillColor, change.Color);
        };

        var result = FillTool.Fill(document, 0, 0, FillColor);

        Assert.Equal(document.Width * document.Height, result.FilledPixelCount);
        Assert.Equal(document.Height, result.Spans.Count);
        Assert.All(result.Spans, span => Assert.Equal(document.Width, span.Length));
        Assert.Equal(1, notificationCount);

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                Assert.Equal(FillColor, document.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Fill_WithMatchingColor_DoesNothing()
    {
        var document = new PixelDocument(3, 3);
        document.SetPixel(1, 1, FillColor);

        var result = FillTool.Fill(document, 1, 1, FillColor);

        Assert.Equal(0, result.FilledPixelCount);
        Assert.Empty(result.Spans);
    }

    [Fact]
    public void Fill_DistinguishesColorsWithDifferentAlpha()
    {
        var document = new PixelDocument(3, 1);
        var targetColor = new PixelColor(20, 40, 60, 100);
        var boundaryColor = new PixelColor(20, 40, 60, 101);
        document.SetPixel(0, 0, targetColor);
        document.SetPixel(1, 0, targetColor);
        document.SetPixel(2, 0, boundaryColor);

        var result = FillTool.Fill(document, 0, 0, FillColor);

        Assert.Equal(2, result.FilledPixelCount);
        Assert.Equal(FillColor, document.GetPixel(0, 0));
        Assert.Equal(FillColor, document.GetPixel(1, 0));
        Assert.Equal(boundaryColor, document.GetPixel(2, 0));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void Fill_WithCoordinateOutsideDocument_Throws(int x, int y)
    {
        var document = new PixelDocument(3, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FillTool.Fill(document, x, y, FillColor));
    }

    [Fact]
    public void Fill_WithNullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FillTool.Fill(null!, 0, 0, FillColor));
    }

    private static PixelDocument CreateBorderedDocument(int width, int height)
    {
        var document = new PixelDocument(width, height);

        for (var x = 0; x < width; x++)
        {
            document.SetPixel(x, 0, BorderColor);
            document.SetPixel(x, height - 1, BorderColor);
        }

        for (var y = 1; y < height - 1; y++)
        {
            document.SetPixel(0, y, BorderColor);
            document.SetPixel(width - 1, y, BorderColor);
        }

        return document;
    }
}
