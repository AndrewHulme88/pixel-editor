using System.Globalization;
using Avalonia;
using Avalonia.Media;
using pixel_editor.Converters;
using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.App.Tests.Converters;

public sealed class PixelColorConverterTests
{
    private readonly PixelColorConverter _converter = new();

    [Fact]
    public void Convert_MapsPixelColorToAvaloniaColor()
    {
        var pixelColor = new PixelColor(10, 20, 30, 40);

        var result = _converter.Convert(
            pixelColor,
            typeof(Color),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(Color.FromArgb(40, 10, 20, 30), result);
    }

    [Fact]
    public void ConvertBack_MapsAvaloniaColorToPixelColor()
    {
        var color = Color.FromArgb(40, 10, 20, 30);

        var result = _converter.ConvertBack(
            color,
            typeof(PixelColor),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(new PixelColor(10, 20, 30, 40), result);
    }

    [Fact]
    public void Convert_WithUnsupportedValue_ReturnsUnsetValue()
    {
        var result = _converter.Convert(
            "not a color",
            typeof(Color),
            null,
            CultureInfo.InvariantCulture);

        Assert.Same(AvaloniaProperty.UnsetValue, result);
    }

    [Fact]
    public void ConvertBack_WithUnsupportedValue_ReturnsUnsetValue()
    {
        var result = _converter.ConvertBack(
            "not a color",
            typeof(PixelColor),
            null,
            CultureInfo.InvariantCulture);

        Assert.Same(AvaloniaProperty.UnsetValue, result);
    }
}
