using System.Buffers;
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

        var brushOffset = size / 2;
        var coverageTop = Math.Max(0, Math.Min(startY, endY) - brushOffset);
        var coverageBottom = Math.Min(
            document.Height,
            Math.Max(startY, endY) - brushOffset + size);
        var rowCount = coverageBottom - coverageTop;
        var rowStartsBuffer = ArrayPool<int>.Shared.Rent(rowCount);
        var rowEndsBuffer = ArrayPool<int>.Shared.Rent(rowCount);
        var rowStarts = rowStartsBuffer.AsSpan(0, rowCount);
        var rowEnds = rowEndsBuffer.AsSpan(0, rowCount);

        rowStarts.Fill(int.MaxValue);
        rowEnds.Fill(int.MinValue);

        try
        {
            var x = startX;
            var y = startY;
            var horizontalDistance = Math.Abs(endX - startX);
            var verticalDistance = Math.Abs(endY - startY);
            var horizontalStep = startX < endX ? 1 : -1;
            var verticalStep = startY < endY ? 1 : -1;
            var error = horizontalDistance - verticalDistance;

            while (true)
            {
                IncludeSquareStamp(
                    document,
                    x,
                    y,
                    size,
                    coverageTop,
                    rowStarts,
                    rowEnds);

                if (x == endX && y == endY)
                {
                    break;
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

            // Adjacent stamps overlap, so each affected row can be painted once as a single span.
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                for (var column = rowStarts[rowIndex]; column < rowEnds[rowIndex]; column++)
                {
                    document.SetPixel(column, coverageTop + rowIndex, color);
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rowStartsBuffer);
            ArrayPool<int>.Shared.Return(rowEndsBuffer);
        }
    }

    private static void IncludeSquareStamp(
        PixelDocument document,
        int centreX,
        int centreY,
        int size,
        int coverageTop,
        Span<int> rowStarts,
        Span<int> rowEnds)
    {
        var left = centreX - (size / 2);
        var top = centreY - (size / 2);
        var startX = Math.Max(0, left);
        var startY = Math.Max(0, top);
        var endX = Math.Min(document.Width, left + size);
        var endY = Math.Min(document.Height, top + size);

        for (var y = startY; y < endY; y++)
        {
            var rowIndex = y - coverageTop;
            rowStarts[rowIndex] = Math.Min(rowStarts[rowIndex], startX);
            rowEnds[rowIndex] = Math.Max(rowEnds[rowIndex], endX);
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
