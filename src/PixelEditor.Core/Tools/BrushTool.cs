using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public static class BrushTool
{
    public static void DrawLine(
        PixelDocument document,
        int startX,
        int startY,
        int endX,
        int endY,
        PixelColor color)
    {
        ArgumentNullException.ThrowIfNull(document);

        var x = startX;
        var y = startY;
        var horizontalDistance = Math.Abs(endX - startX);
        var verticalDistance = Math.Abs(endY - startY);
        var horizontalStep = startX < endX ? 1 : -1;
        var verticalStep = startY < endY ? 1 : -1;
        var error = horizontalDistance - verticalDistance;

        while (true)
        {
            document.SetPixel(x, y, color);

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
}
