using System;
using Avalonia;

namespace pixel_editor.Rendering;

internal readonly record struct CanvasLayoutResult(Rect Destination, double PixelScale);

internal static class CanvasLayout
{
    public static CanvasLayoutResult Calculate(
        int documentWidth,
        int documentHeight,
        Size availableSize)
    {
        if (documentWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentWidth));
        }

        if (documentHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentHeight));
        }

        var horizontalScale = Math.Floor(availableSize.Width / documentWidth);
        var verticalScale = Math.Floor(availableSize.Height / documentHeight);
        var pixelScale = Math.Max(1, Math.Min(horizontalScale, verticalScale));

        var renderedWidth = documentWidth * pixelScale;
        var renderedHeight = documentHeight * pixelScale;
        var x = (availableSize.Width - renderedWidth) / 2;
        var y = (availableSize.Height - renderedHeight) / 2;

        return new CanvasLayoutResult(
            new Rect(x, y, renderedWidth, renderedHeight),
            pixelScale);
    }
}
