using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public static class BrushTool
{
    public const int MinimumSize = 1;
    public const int MaximumSize = 64;

    public static void DrawLine(
        PixelDocument document,
        int startX,
        int startY,
        int endX,
        int endY,
        PixelColor color,
        int size = MinimumSize)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (size is < MinimumSize or > MaximumSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        ValidateCoordinate(startX, document.Width, nameof(startX));
        ValidateCoordinate(startY, document.Height, nameof(startY));
        ValidateCoordinate(endX, document.Width, nameof(endX));
        ValidateCoordinate(endY, document.Height, nameof(endY));

        var x = startX;
        var y = startY;
        var horizontalDistance = Math.Abs(endX - startX);
        var verticalDistance = Math.Abs(endY - startY);
        var horizontalStep = startX < endX ? 1 : -1;
        var verticalStep = startY < endY ? 1 : -1;
        var error = horizontalDistance - verticalDistance;

        while (true)
        {
            StampSquare(document, x, y, color, size);

            if (x == endX && y == endY)
            {
                return;
            }

            var doubledError = error * 2;

            if (doubledError > -verticalDistance)
            {
                error -= verticalDistance;
                x += horizontalStep;
            }

            if (doubledError < horizontalDistance)
            {
                error += horizontalDistance;
                y += verticalStep;
            }
        }
    }

    private static void StampSquare(
        PixelDocument document,
        int centreX,
        int centreY,
        PixelColor color,
        int size)
    {
        var left = centreX - (size / 2);
        var top = centreY - (size / 2);
        var startX = Math.Max(0, left);
        var startY = Math.Max(0, top);
        var endX = Math.Min(document.Width, left + size);
        var endY = Math.Min(document.Height, top + size);

        for (var y = startY; y < endY; y++)
        {
            for (var x = startX; x < endX; x++)
            {
                document.SetPixel(x, y, color);
            }
        }
    }

    private static void ValidateCoordinate(int coordinate, int length, string parameterName)
    {
        if ((uint)coordinate >= (uint)length)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
