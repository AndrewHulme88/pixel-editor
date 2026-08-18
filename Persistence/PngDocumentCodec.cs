using System;
using System.IO;
using PixelEditor.Core.Documents;
using SkiaSharp;

namespace pixel_editor.Persistence;

// Converts between PixelDocument and lossless PNG streams.
internal static class PngDocumentCodec
{
    private const int BytesPerPixel = 4;

    public static void Save(PixelDocument document, Stream output)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }

        var imageInfo = CreateImageInfo(document.Width, document.Height);
        using var bitmap = new SKBitmap(imageInfo);
        var pixels = bitmap.GetPixelSpan();

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                var color = document.GetPixel(x, y);
                var offset = (y * bitmap.RowBytes) + (x * BytesPerPixel);
                pixels[offset] = color.Red;
                pixels[offset + 1] = color.Green;
                pixels[offset + 2] = color.Blue;
                pixels[offset + 3] = color.Alpha;
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new IOException("The document could not be encoded as PNG.");

        encoded.SaveTo(output);

        if (output.CanSeek)
        {
            output.SetLength(output.Position);
        }
    }

    public static PixelDocument Load(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!input.CanRead)
        {
            throw new ArgumentException("The input stream must be readable.", nameof(input));
        }

        using var skiaStream = new SKManagedStream(input, disposeManagedStream: false);
        using var codec = SKCodec.Create(skiaStream, out var creationResult);

        if (codec is null || creationResult != SKCodecResult.Success)
        {
            throw new InvalidDataException("The stream does not contain a valid image.");
        }

        if (codec.EncodedFormat != SKEncodedImageFormat.Png)
        {
            throw new InvalidDataException("Only PNG images are supported.");
        }

        if (codec.FrameCount > 1)
        {
            throw new InvalidDataException("Animated PNG images are not supported.");
        }

        var sourceInfo = codec.Info;
        ValidateImportedDimensions(sourceInfo.Width, sourceInfo.Height);
        var imageInfo = CreateImageInfo(sourceInfo.Width, sourceInfo.Height);
        using var bitmap = new SKBitmap(imageInfo);
        var decodeResult = codec.GetPixels(imageInfo, bitmap.GetPixels());

        if (decodeResult != SKCodecResult.Success)
        {
            throw new InvalidDataException($"The PNG image could not be decoded: {decodeResult}.");
        }

        var document = new PixelDocument(imageInfo.Width, imageInfo.Height);
        var pixels = bitmap.GetPixelSpan();

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                var offset = (y * bitmap.RowBytes) + (x * BytesPerPixel);
                document.SetPixel(
                    x,
                    y,
                    new PixelColor(
                        pixels[offset],
                        pixels[offset + 1],
                        pixels[offset + 2],
                        pixels[offset + 3]));
            }
        }

        return document;
    }

    private static SKImageInfo CreateImageInfo(int width, int height)
    {
        PixelDocumentLimits.ValidateDimensions(width, height);
        _ = checked(width * height * BytesPerPixel);
        return new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
    }

    private static void ValidateImportedDimensions(int width, int height)
    {
        if (!PixelDocumentLimits.AreDimensionsSupported(width, height))
        {
            throw new InvalidDataException(
                $"PNG dimensions must each be between " +
                $"{PixelDocumentLimits.MinimumDimension} and " +
                $"{PixelDocumentLimits.MaximumDimension} pixels. " +
                $"Image dimensions are {width} × {height}.");
        }
    }
}
