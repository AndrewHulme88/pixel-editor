using Avalonia;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CanvasLayoutTests
{
    [Fact]
    public void Calculate_UsesLargestIntegerScaleThatFits()
    {
        var result = CanvasLayout.Calculate(16, 16, new Size(512, 320));

        Assert.Equal(20, result.PixelScale);
        Assert.Equal(new Rect(96, 0, 320, 320), result.Destination);
    }

    [Fact]
    public void Calculate_CentresDocumentInAvailableSpace()
    {
        var result = CanvasLayout.Calculate(10, 5, new Size(103, 81));

        Assert.Equal(10, result.PixelScale);
        Assert.Equal(new Rect(1.5, 15.5, 100, 50), result.Destination);
    }

    [Fact]
    public void Calculate_DoesNotShrinkPixelsBelowTheirNativeSize()
    {
        var result = CanvasLayout.Calculate(200, 100, new Size(100, 80));

        Assert.Equal(1, result.PixelScale);
        Assert.Equal(new Rect(-50, -10, 200, 100), result.Destination);
    }

    [Fact]
    public void Calculate_WithExplicitScaleAndPan_AppliesViewportTransform()
    {
        var result = CanvasLayout.Calculate(
            10,
            5,
            new Size(200, 100),
            8,
            new Vector(12, -6));

        Assert.Equal(8, result.PixelScale);
        Assert.Equal(new Rect(72, 24, 80, 40), result.Destination);
    }

    [Theory]
    [InlineData(0, 1, "documentWidth")]
    [InlineData(-1, 1, "documentWidth")]
    [InlineData(1, 0, "documentHeight")]
    [InlineData(1, -1, "documentHeight")]
    public void Calculate_WithInvalidDocumentSize_Throws(
        int width,
        int height,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CanvasLayout.Calculate(width, height, new Size(100, 100)));

        Assert.Equal(expectedParameter, exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Calculate_WithInvalidExplicitScale_Throws(double scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CanvasLayout.Calculate(1, 1, new Size(100, 100), scale, default));
    }
}
