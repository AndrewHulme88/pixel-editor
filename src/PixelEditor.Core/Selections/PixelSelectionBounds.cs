using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Selections;

public readonly record struct PixelSelectionBounds
{
    public PixelSelectionBounds(int x, int y, int width, int height)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Right => checked(X + Width);

    public int Bottom => checked(Y + Height);

    public static PixelSelectionBounds FromInclusiveCorners(
        int startX,
        int startY,
        int endX,
        int endY,
        int documentWidth,
        int documentHeight)
    {
        PixelDocumentLimits.ValidateDimensions(documentWidth, documentHeight);

        var clippedStartX = Math.Clamp(startX, 0, documentWidth - 1);
        var clippedStartY = Math.Clamp(startY, 0, documentHeight - 1);
        var clippedEndX = Math.Clamp(endX, 0, documentWidth - 1);
        var clippedEndY = Math.Clamp(endY, 0, documentHeight - 1);
        var left = Math.Min(clippedStartX, clippedEndX);
        var top = Math.Min(clippedStartY, clippedEndY);
        var right = Math.Max(clippedStartX, clippedEndX);
        var bottom = Math.Max(clippedStartY, clippedEndY);

        return new PixelSelectionBounds(
            left,
            top,
            (right - left) + 1,
            (bottom - top) + 1);
    }
}
