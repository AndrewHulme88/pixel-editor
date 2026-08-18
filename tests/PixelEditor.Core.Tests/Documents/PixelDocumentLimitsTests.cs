using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.Core.Tests.Documents;

public sealed class PixelDocumentLimitsTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, PixelDocumentLimits.MaximumDimension)]
    [InlineData(PixelDocumentLimits.MaximumDimension, 1)]
    [InlineData(PixelDocumentLimits.MaximumDimension, PixelDocumentLimits.MaximumDimension)]
    public void AreDimensionsSupported_WithinInclusiveRange_ReturnsTrue(int width, int height)
    {
        Assert.True(PixelDocumentLimits.AreDimensionsSupported(width, height));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(PixelDocumentLimits.MaximumDimension + 1, 1)]
    [InlineData(1, PixelDocumentLimits.MaximumDimension + 1)]
    public void AreDimensionsSupported_OutsideRange_ReturnsFalse(int width, int height)
    {
        Assert.False(PixelDocumentLimits.AreDimensionsSupported(width, height));
    }

    [Theory]
    [InlineData(0, 1, "width")]
    [InlineData(PixelDocumentLimits.MaximumDimension + 1, 1, "width")]
    [InlineData(1, 0, "height")]
    [InlineData(1, PixelDocumentLimits.MaximumDimension + 1, "height")]
    public void ValidateDimensions_OutsideRange_ThrowsForInvalidDimension(
        int width,
        int height,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            PixelDocumentLimits.ValidateDimensions(width, height));

        Assert.Equal(expectedParameter, exception.ParamName);
    }
}
