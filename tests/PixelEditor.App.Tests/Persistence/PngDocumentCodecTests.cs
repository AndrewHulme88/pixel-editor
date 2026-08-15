using pixel_editor.Persistence;
using PixelEditor.Core.Documents;
using SkiaSharp;
using Xunit;

namespace PixelEditor.App.Tests.Persistence;

public sealed class PngDocumentCodecTests
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    [Fact]
    public void Save_WritesPngAndLeavesStreamOpen()
    {
        var document = new PixelDocument(1, 1);
        document.SetPixel(0, 0, new PixelColor(10, 20, 30, 40));
        using var stream = new MemoryStream();

        PngDocumentCodec.Save(document, stream);

        Assert.True(stream.CanWrite);
        Assert.Equal(PngSignature, stream.ToArray()[..PngSignature.Length]);
    }

    [Fact]
    public void SaveAndLoad_PreservesDimensionsAndExactColors()
    {
        var document = new PixelDocument(3, 2);
        document.SetPixel(0, 0, new PixelColor(255, 0, 0));
        document.SetPixel(1, 0, new PixelColor(0, 255, 0, 128));
        document.SetPixel(2, 0, new PixelColor(0, 0, 255, 1));
        document.SetPixel(0, 1, PixelColor.Transparent);
        document.SetPixel(1, 1, new PixelColor(12, 34, 56, 78));
        document.SetPixel(2, 1, new PixelColor(255, 255, 255));
        using var stream = new MemoryStream();

        PngDocumentCodec.Save(document, stream);
        stream.Position = 0;
        var loaded = PngDocumentCodec.Load(stream);

        Assert.Equal(document.Width, loaded.Width);
        Assert.Equal(document.Height, loaded.Height);

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                Assert.Equal(document.GetPixel(x, y), loaded.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Save_TruncatesExistingSeekableStream()
    {
        var document = new PixelDocument(1, 1);
        document.SetPixel(0, 0, new PixelColor(10, 20, 30, 40));
        using var stream = new MemoryStream(new byte[4096], writable: true);

        PngDocumentCodec.Save(document, stream);

        Assert.Equal(stream.Position, stream.Length);
        stream.Position = 0;
        var loaded = PngDocumentCodec.Load(stream);
        Assert.Equal(document.GetPixel(0, 0), loaded.GetPixel(0, 0));
    }

    [Fact]
    public void Load_LeavesStreamOpen()
    {
        using var stream = CreatePngStream();

        _ = PngDocumentCodec.Load(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public void Load_WithInvalidData_Throws()
    {
        using var stream = new MemoryStream([1, 2, 3, 4]);

        Assert.Throws<InvalidDataException>(() => PngDocumentCodec.Load(stream));
    }

    [Fact]
    public void Load_WithTruncatedPng_Throws()
    {
        using var complete = CreatePngStream();
        var bytes = complete.ToArray();
        using var truncated = new MemoryStream(bytes[..(bytes.Length / 2)]);

        Assert.Throws<InvalidDataException>(() => PngDocumentCodec.Load(truncated));
    }

    [Fact]
    public void Load_WithNonPngImage_Throws()
    {
        using var bitmap = new SKBitmap(1, 1);
        bitmap.SetPixel(0, 0, SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        using var stream = new MemoryStream(encoded.ToArray());

        var exception = Assert.Throws<InvalidDataException>(() => PngDocumentCodec.Load(stream));

        Assert.Equal("Only PNG images are supported.", exception.Message);
    }

    private static MemoryStream CreatePngStream()
    {
        var document = new PixelDocument(1, 1);
        document.SetPixel(0, 0, new PixelColor(10, 20, 30, 40));
        var stream = new MemoryStream();
        PngDocumentCodec.Save(document, stream);
        stream.Position = 0;
        return stream;
    }
}
