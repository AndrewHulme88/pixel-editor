using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public static class OutlineShapeTool
{
    public static void DrawRectangle(
        PixelDocument document,
        int startX,
        int startY,
        int endX,
        int endY,
        PixelColor color,
        int size = BrushTool.MinimumSize)
    {
        ValidateArguments(document, startX, startY, endX, endY, size);

        var left = Math.Min(startX, endX);
        var right = Math.Max(startX, endX);
        var top = Math.Min(startY, endY);
        var bottom = Math.Max(startY, endY);
        var coverage = new OutlineCoverage(document, size);

        for (var x = left; x <= right; x++)
        {
            coverage.IncludeStamp(x, top);
            coverage.IncludeStamp(x, bottom);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            coverage.IncludeStamp(left, y);
            coverage.IncludeStamp(right, y);
        }

        coverage.Paint(color);
    }

    public static void DrawEllipse(
        PixelDocument document,
        int startX,
        int startY,
        int endX,
        int endY,
        PixelColor color,
        int size = BrushTool.MinimumSize)
    {
        ValidateArguments(document, startX, startY, endX, endY, size);

        var left = Math.Min(startX, endX);
        var right = Math.Max(startX, endX);
        var top = Math.Min(startY, endY);
        var bottom = Math.Max(startY, endY);
        var coverage = new OutlineCoverage(document, size);

        if (left == right)
        {
            for (var y = top; y <= bottom; y++)
            {
                coverage.IncludeStamp(left, y);
            }
        }
        else if (top == bottom)
        {
            for (var x = left; x <= right; x++)
            {
                coverage.IncludeStamp(x, top);
            }
        }
        else
        {
            RasterizeEllipse(left, top, right, bottom, coverage.IncludeStamp);
        }

        coverage.Paint(color);
    }

    private static void RasterizeEllipse(
        int left,
        int top,
        int right,
        int bottom,
        Action<int, int> includePoint)
    {
        // Integer error terms keep the outline connected without floating-point drift.
        long x0 = left;
        long y0 = top;
        long x1 = right;
        long y1 = bottom;
        var horizontalDiameter = x1 - x0;
        var verticalDiameter = y1 - y0;
        var verticalParity = verticalDiameter & 1;
        var horizontalSquared = horizontalDiameter * horizontalDiameter;
        var verticalSquared = verticalDiameter * verticalDiameter;
        var horizontalDelta = 4 * (1 - horizontalDiameter) * verticalSquared;
        var verticalDelta = 4 * (verticalParity + 1) * horizontalSquared;
        var error = horizontalDelta + verticalDelta + (verticalParity * horizontalSquared);

        y0 += (verticalDiameter + 1) / 2;
        y1 = y0 - verticalParity;
        horizontalSquared *= 8;
        verticalSquared *= 8;

        do
        {
            includePoint((int)x1, (int)y0);
            includePoint((int)x0, (int)y0);
            includePoint((int)x0, (int)y1);
            includePoint((int)x1, (int)y1);

            var doubledError = 2 * error;

            if (doubledError <= verticalDelta)
            {
                y0++;
                y1--;
                error += verticalDelta += horizontalSquared;
            }

            if (doubledError >= horizontalDelta || (2 * error) > verticalDelta)
            {
                x0++;
                x1--;
                error += horizontalDelta += verticalSquared;
            }
        }
        while (x0 <= x1);

        while (y0 - y1 < verticalDiameter)
        {
            includePoint((int)(x0 - 1), (int)y0);
            includePoint((int)(x1 + 1), (int)y0);
            y0++;
            includePoint((int)(x0 - 1), (int)y1);
            includePoint((int)(x1 + 1), (int)y1);
            y1--;
        }
    }

    private static void ValidateArguments(
        PixelDocument? document,
        int startX,
        int startY,
        int endX,
        int endY,
        int size)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (size is < BrushTool.MinimumSize or > BrushTool.MaximumSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

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

    private sealed class OutlineCoverage
    {
        private readonly PixelDocument _document;
        private readonly int _brushSize;
        private readonly List<Interval>?[] _rows;

        public OutlineCoverage(PixelDocument document, int brushSize)
        {
            _document = document;
            _brushSize = brushSize;
            _rows = new List<Interval>?[document.Height];
        }

        public void IncludeStamp(int centreX, int centreY)
        {
            var left = centreX - (_brushSize / 2);
            var top = centreY - (_brushSize / 2);
            var startX = Math.Max(0, left);
            var startY = Math.Max(0, top);
            var endX = Math.Min(_document.Width, left + _brushSize);
            var endY = Math.Min(_document.Height, top + _brushSize);

            for (var y = startY; y < endY; y++)
            {
                IncludeInterval(y, startX, endX);
            }
        }

        public void Paint(PixelColor color)
        {
            for (var y = 0; y < _rows.Length; y++)
            {
                if (_rows[y] is not { } intervals)
                {
                    continue;
                }

                foreach (var interval in intervals)
                {
                    for (var x = interval.Start; x < interval.End; x++)
                    {
                        _document.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void IncludeInterval(int y, int start, int end)
        {
            var intervals = _rows[y] ??= [];

            // Ellipse rows can retain two separated edge intervals.
            for (var index = intervals.Count - 1; index >= 0; index--)
            {
                var interval = intervals[index];

                if (end < interval.Start || start > interval.End)
                {
                    continue;
                }

                start = Math.Min(start, interval.Start);
                end = Math.Max(end, interval.End);
                intervals.RemoveAt(index);
            }

            intervals.Add(new Interval(start, end));
        }

        private readonly record struct Interval(int Start, int End);
    }
}
