using System;
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

    public static PixelCoordinate MapClamped(
        Point pointerPosition,
        CanvasLayoutResult layout,
        int documentWidth,
        int documentHeight)
    {
        if (documentWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentWidth));
        }

        if (documentHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentHeight));
        }

        var x = Math.Floor(
            (pointerPosition.X - layout.Destination.X) / layout.PixelScale);
        var y = Math.Floor(
            (pointerPosition.Y - layout.Destination.Y) / layout.PixelScale);

        return new PixelCoordinate(
            (int)Math.Clamp(x, 0, documentWidth - 1),
            (int)Math.Clamp(y, 0, documentHeight - 1));
    }
}
