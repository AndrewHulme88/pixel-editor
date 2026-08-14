using Avalonia;

namespace pixel_editor.Rendering;

internal static class CanvasPixelGrid
{
    public static Rect GetPixelBounds(CanvasLayoutResult layout, int x, int y) => new(
        layout.Destination.X + (x * layout.PixelScale),
        layout.Destination.Y + (y * layout.PixelScale),
        layout.PixelScale,
        layout.PixelScale);
}
