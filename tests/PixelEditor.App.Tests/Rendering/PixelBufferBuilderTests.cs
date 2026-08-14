using pixel_editor.Rendering;
using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class PixelBufferBuilderTests
{
    [Fact]
    public void CreatePremultipliedBgra_WritesChannelsInBgraOrder()
    {
        var document = new PixelDocument(1, 1);
        document.SetPixel(0, 0, new PixelColor(10, 20, 30, 255));

        var buffer = PixelBufferBuilder.CreatePremultipliedBgra(document);

        Assert.Equal(new byte[] { 30, 20, 10, 255 }, buffer);
    }

    [Fact]
    public void CreatePremultipliedBgra_PremultipliesColorChannelsByAlpha()
    {
        var document = new PixelDocument(1, 1);
        document.SetPixel(0, 0, new PixelColor(100, 40, 200, 128));

        var buffer = PixelBufferBuilder.CreatePremultipliedBgra(document);

        Assert.Equal(new byte[] { 100, 20, 50, 128 }, buffer);
    }

    [Fact]
    public void CreatePremultipliedBgra_PreservesRowMajorPixelOrder()
    {
        var document = new PixelDocument(2, 2);
        document.SetPixel(0, 0, new PixelColor(1, 0, 0));
        document.SetPixel(1, 0, new PixelColor(2, 0, 0));
        document.SetPixel(0, 1, new PixelColor(3, 0, 0));
        document.SetPixel(1, 1, new PixelColor(4, 0, 0));

        var buffer = PixelBufferBuilder.CreatePremultipliedBgra(document);

        Assert.Equal(
            new byte[]
            {
                0, 0, 1, 255,
                0, 0, 2, 255,
                0, 0, 3, 255,
                0, 0, 4, 255,
            },
            buffer);
    }

    [Fact]
    public void CreatePremultipliedBgra_WritesTransparentPixelsAsZeroes()
    {
        var document = new PixelDocument(1, 1);

        var buffer = PixelBufferBuilder.CreatePremultipliedBgra(document);

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, buffer);
    }

    [Fact]
    public void WritePremultipliedBgra_UpdatesProvidedPixelBuffer()
    {
        Span<byte> pixel = stackalloc byte[PixelBufferBuilder.BytesPerPixel];

        PixelBufferBuilder.WritePremultipliedBgra(
            new PixelColor(100, 40, 200, 128),
            pixel);

        Assert.Equal(new byte[] { 100, 20, 50, 128 }, pixel.ToArray());
    }
}
