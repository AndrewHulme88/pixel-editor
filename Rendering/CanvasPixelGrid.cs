using System;
using Avalonia;

namespace pixel_editor.Rendering;

internal static class CanvasPixelGrid
{
    public static Rect GetPixelBounds(CanvasLayoutResult layout, int x, int y) => new(
        layout.Destination.X + (x * layout.PixelScale),
        layout.Destination.Y + (y * layout.PixelScale),
        layout.PixelScale,
        layout.PixelScale);

    public static Rect GetBrushBounds(
        CanvasLayoutResult layout,
        int centreX,
        int centreY,
        int brushSize,
        int documentWidth,
        int documentHeight)
    {
        var left = Math.Max(0, centreX - (brushSize / 2));
        var top = Math.Max(0, centreY - (brushSize / 2));
        var right = Math.Min(documentWidth, centreX - (brushSize / 2) + brushSize);
        var bottom = Math.Min(documentHeight, centreY - (brushSize / 2) + brushSize);

        return new Rect(
            layout.Destination.X + (left * layout.PixelScale),
            layout.Destination.Y + (top * layout.PixelScale),
            (right - left) * layout.PixelScale,
            (bottom - top) * layout.PixelScale);
    }
}
