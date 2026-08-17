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

        return Calculate(documentWidth, documentHeight, availableSize, pixelScale, default);
    }

    public static CanvasLayoutResult Calculate(
        int documentWidth,
        int documentHeight,
        Size availableSize,
        double pixelScale,
        Vector panOffset)
    {
        if (documentWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentWidth));
        }

        if (documentHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentHeight));
        }

        if (!double.IsFinite(pixelScale) || pixelScale < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelScale));
        }

        var renderedWidth = documentWidth * pixelScale;
        var renderedHeight = documentHeight * pixelScale;
        var x = ((availableSize.Width - renderedWidth) / 2) + panOffset.X;
        var y = ((availableSize.Height - renderedHeight) / 2) + panOffset.Y;

        return new CanvasLayoutResult(
            new Rect(x, y, renderedWidth, renderedHeight),
            pixelScale);
    }
}
