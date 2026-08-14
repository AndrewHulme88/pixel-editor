using Avalonia;

namespace pixel_editor.Rendering;

internal readonly record struct PixelCoordinate(int X, int Y);

internal static class CanvasCoordinateMapper
{
    public static bool TryMap(
        Point pointerPosition,
        CanvasLayoutResult layout,
        int documentWidth,
        int documentHeight,
        out PixelCoordinate coordinate)
    {
        var destination = layout.Destination;

        if (pointerPosition.X < destination.X ||
            pointerPosition.X >= destination.Right ||
            pointerPosition.Y < destination.Y ||
            pointerPosition.Y >= destination.Bottom)
        {
            coordinate = default;
            return false;
        }

        var x = (int)((pointerPosition.X - destination.X) / layout.PixelScale);
        var y = (int)((pointerPosition.Y - destination.Y) / layout.PixelScale);

        if ((uint)x >= (uint)documentWidth || (uint)y >= (uint)documentHeight)
        {
            coordinate = default;
            return false;
        }

        coordinate = new PixelCoordinate(x, y);
        return true;
    }
}
