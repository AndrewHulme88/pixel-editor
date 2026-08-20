using Avalonia;
using PixelEditor.Core.Selections;

namespace pixel_editor.Rendering;

internal static class SelectionRenderLayout
{
    public static Rect Calculate(
        PixelSelectionBounds bounds,
        CanvasLayoutResult layout) => new(
            layout.Destination.X + (bounds.X * layout.PixelScale),
            layout.Destination.Y + (bounds.Y * layout.PixelScale),
            bounds.Width * layout.PixelScale,
            bounds.Height * layout.PixelScale);
}
