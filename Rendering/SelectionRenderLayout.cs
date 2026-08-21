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

    public static SelectionRenderSegment Calculate(
        SelectionOutlineSegment segment,
        CanvasLayoutResult layout) => new(
            new Point(
                layout.Destination.X + (segment.StartX * layout.PixelScale),
                layout.Destination.Y + (segment.StartY * layout.PixelScale)),
            new Point(
                layout.Destination.X + (segment.EndX * layout.PixelScale),
                layout.Destination.Y + (segment.EndY * layout.PixelScale)));
}

internal readonly record struct SelectionRenderSegment(Point Start, Point End);
