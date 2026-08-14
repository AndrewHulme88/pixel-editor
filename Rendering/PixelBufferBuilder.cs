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

                buffer[offset] = Premultiply(color.Blue, color.Alpha);
                buffer[offset + 1] = Premultiply(color.Green, color.Alpha);
                buffer[offset + 2] = Premultiply(color.Red, color.Alpha);
                buffer[offset + 3] = color.Alpha;
            }
        }

        return buffer;
    }

    private static byte Premultiply(byte channel, byte alpha)
        => (byte)((channel * alpha + 127) / byte.MaxValue);
}
