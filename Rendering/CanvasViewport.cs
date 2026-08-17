using System;
using Avalonia;

namespace pixel_editor.Rendering;

internal readonly record struct CanvasViewportState(double PixelScale, Vector PanOffset);

internal static class CanvasViewport
{
    private static readonly double[] PixelScales =
    [
        1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64,
        96, 128, 192, 256, 384, 512, 768, 1024
    ];

    public static CanvasViewportState ZoomAt(
        CanvasLayoutResult currentLayout,
        int documentWidth,
        int documentHeight,
        Size availableSize,
        Point anchor,
        bool zoomIn)
    {
        var nextScale = GetNextScale(currentLayout.PixelScale, zoomIn);

        if (nextScale == currentLayout.PixelScale)
        {
            var centred = CanvasLayout.Calculate(
                documentWidth,
                documentHeight,
                availableSize,
                nextScale,
                default);

            return new CanvasViewportState(
                nextScale,
                currentLayout.Destination.Position - centred.Destination.Position);
        }

        var documentX = (anchor.X - currentLayout.Destination.X) / currentLayout.PixelScale;
        var documentY = (anchor.Y - currentLayout.Destination.Y) / currentLayout.PixelScale;
        var newOrigin = new Point(
            anchor.X - (documentX * nextScale),
            anchor.Y - (documentY * nextScale));
        var centredLayout = CanvasLayout.Calculate(
            documentWidth,
            documentHeight,
            availableSize,
            nextScale,
            default);
        var panOffset = newOrigin - centredLayout.Destination.Position;

        return new CanvasViewportState(nextScale, panOffset);
    }

    public static double GetNextScale(double currentScale, bool zoomIn)
    {
        if (!double.IsFinite(currentScale) || currentScale < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentScale));
        }

        if (zoomIn)
        {
            foreach (var scale in PixelScales)
            {
                if (scale > currentScale)
                {
                    return scale;
                }
            }

            return Math.Max(currentScale, PixelScales[^1]);
        }

        for (var index = PixelScales.Length - 1; index >= 0; index--)
        {
            if (PixelScales[index] < currentScale)
            {
                return PixelScales[index];
            }
        }

        return PixelScales[0];
    }
}
