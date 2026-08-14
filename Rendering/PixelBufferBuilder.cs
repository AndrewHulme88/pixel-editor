using System;
using PixelEditor.Core.Documents;

namespace pixel_editor.Rendering;

internal static class PixelBufferBuilder
{
    public const int BytesPerPixel = 4;

    public static byte[] CreatePremultipliedBgra(PixelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var buffer = new byte[checked(document.Width * document.Height * BytesPerPixel)];

        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
            {
                var color = document.GetPixel(x, y);
                var offset = ((y * document.Width) + x) * BytesPerPixel;
                WritePremultipliedBgra(color, buffer.AsSpan(offset, BytesPerPixel));
            }
        }

        return buffer;
    }

    public static void WritePremultipliedBgra(PixelColor color, Span<byte> destination)
    {
        if (destination.Length < BytesPerPixel)
        {
            throw new ArgumentException("The destination must contain at least four bytes.", nameof(destination));
        }

        destination[0] = Premultiply(color.Blue, color.Alpha);
        destination[1] = Premultiply(color.Green, color.Alpha);
        destination[2] = Premultiply(color.Red, color.Alpha);
        destination[3] = color.Alpha;
    }

    private static byte Premultiply(byte channel, byte alpha)
        => (byte)((channel * alpha + 127) / byte.MaxValue);
}
