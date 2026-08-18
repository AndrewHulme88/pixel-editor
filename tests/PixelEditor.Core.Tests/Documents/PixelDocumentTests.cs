using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.Core.Tests.Documents;

public sealed class PixelDocumentTests
{
    [Fact]
    public void Constructor_StoresDimensions()
    {
        var document = new PixelDocument(16, 24);

        Assert.Equal(16, document.Width);
        Assert.Equal(24, document.Height);
    }

    [Theory]
    [InlineData(0, 1, "width")]
    [InlineData(-1, 1, "width")]
    [InlineData(PixelDocumentLimits.MaximumDimension + 1, 1, "width")]
    [InlineData(1, 0, "height")]
    [InlineData(1, -1, "height")]
    [InlineData(1, PixelDocumentLimits.MaximumDimension + 1, "height")]
    public void Constructor_WithInvalidDimensions_Throws(
        int width,
        int height,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PixelDocument(width, height));

        Assert.Equal(expectedParameter, exception.ParamName);
    }

    [Theory]
    [InlineData(PixelDocumentLimits.MaximumDimension, 1)]
    [InlineData(1, PixelDocumentLimits.MaximumDimension)]
    public void Constructor_AtMaximumDimension_Succeeds(int width, int height)
    {
        var document = new PixelDocument(width, height);

        Assert.Equal(width, document.Width);
        Assert.Equal(height, document.Height);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(0, 2)]
    [InlineData(3, 2)]
    public void NewDocument_ContainsTransparentPixels(int x, int y)
    {
        var document = new PixelDocument(4, 3);

        Assert.Equal(PixelColor.Transparent, document.GetPixel(x, y));
    }

    [Fact]
    public void SetPixel_StoresColorAtRequestedCoordinate()
    {
        var document = new PixelDocument(4, 3);
        var color = new PixelColor(20, 40, 60, 128);

        document.SetPixel(2, 1, color);

        Assert.Equal(color, document.GetPixel(2, 1));
        Assert.Equal(PixelColor.Transparent, document.GetPixel(1, 1));
    }

    [Fact]
    public void SetPixel_WhenColorChanges_RaisesPixelChanged()
    {
        var document = new PixelDocument(4, 3);
        var color = new PixelColor(20, 40, 60, 128);
        PixelChangedEventArgs? receivedChange = null;
        document.PixelChanged += (_, change) => receivedChange = change;

        document.SetPixel(2, 1, color);

        Assert.NotNull(receivedChange);
        Assert.Equal(2, receivedChange.X);
        Assert.Equal(1, receivedChange.Y);
        Assert.Equal(PixelColor.Transparent, receivedChange.PreviousColor);
        Assert.Equal(color, receivedChange.Color);
    }

    [Fact]
    public void SetPixel_WhenColorIsUnchanged_DoesNotRaisePixelChanged()
    {
        var document = new PixelDocument(4, 3);
        var color = new PixelColor(20, 40, 60, 128);
        document.SetPixel(2, 1, color);
        var changeCount = 0;
        document.PixelChanged += (_, _) => changeCount++;

        document.SetPixel(2, 1, color);

        Assert.Equal(0, changeCount);
    }

    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(4, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 3, "y")]
    public void GetPixel_OutsideDocument_Throws(
        int x,
        int y,
        string expectedParameter)
    {
        var document = new PixelDocument(4, 3);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => document.GetPixel(x, y));

        Assert.Equal(expectedParameter, exception.ParamName);
    }

    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(4, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 3, "y")]
    public void SetPixel_OutsideDocument_Throws(
        int x,
        int y,
        string expectedParameter)
    {
        var document = new PixelDocument(4, 3);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => document.SetPixel(x, y, new PixelColor(1, 2, 3)));

        Assert.Equal(expectedParameter, exception.ParamName);
    }
}
