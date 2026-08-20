using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public static class FilledShapeTool
{
    public static IReadOnlyList<PixelSpan> CreateRectangleSpans(
        PixelDocument document,
        int startX,
        int startY,
        int endX,
        int endY)
    {
        ValidateCoordinates(document, startX, startY, endX, endY);

        var left = Math.Min(startX, endX);
        var right = Math.Max(startX, endX);
        var top = Math.Min(startY, endY);
        var bottom = Math.Max(startY, endY);
        var spans = new PixelSpan[(bottom - top) + 1];

        for (var y = top; y <= bottom; y++)
        {
            spans[y - top] = new PixelSpan(left, y, (right - left) + 1);
        }

        return spans;
    }

    public static IReadOnlyList<PixelSpan> CreateEllipseSpans(
        PixelDocument document,
        int startX,
        int startY,
        int endX,
        int endY)
    {
        ValidateCoordinates(document, startX, startY, endX, endY);

        var left = Math.Min(startX, endX);
        var right = Math.Max(startX, endX);
        var top = Math.Min(startY, endY);
        var bottom = Math.Max(startY, endY);

        if (left == right)
        {
            var verticalSpans = new PixelSpan[(bottom - top) + 1];

            for (var y = top; y <= bottom; y++)
            {
                verticalSpans[y - top] = new PixelSpan(left, y, 1);
            }

            return verticalSpans;
        }

        if (top == bottom)
        {
            return [new PixelSpan(left, top, (right - left) + 1)];
        }

        var rowCount = (bottom - top) + 1;
        var rowStarts = new int[rowCount];
        var rowEnds = new int[rowCount];
        Array.Fill(rowStarts, int.MaxValue);
        Array.Fill(rowEnds, int.MinValue);

        IntegerEllipseRasterizer.Rasterize(
            left,
            top,
            right,
            bottom,
            (x, y) =>
            {
                var row = y - top;
                rowStarts[row] = Math.Min(rowStarts[row], x);
                rowEnds[row] = Math.Max(rowEnds[row], x);
            });

        var spans = new List<PixelSpan>(rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            if (rowStarts[row] <= rowEnds[row])
            {
                spans.Add(new PixelSpan(
                    rowStarts[row],
                    top + row,
                    (rowEnds[row] - rowStarts[row]) + 1));
            }
        }

        return spans;
    }

    private static void ValidateCoordinates(
        PixelDocument? document,
        int startX,
        int startY,
        int endX,
        int endY)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateCoordinate(startX, document.Width, nameof(startX));
        ValidateCoordinate(startY, document.Height, nameof(startY));
        ValidateCoordinate(endX, document.Width, nameof(endX));
        ValidateCoordinate(endY, document.Height, nameof(endY));
    }

    private static void ValidateCoordinate(int coordinate, int length, string parameterName)
    {
        if ((uint)coordinate >= (uint)length)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
