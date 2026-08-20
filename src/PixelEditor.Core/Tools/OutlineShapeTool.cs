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
            IntegerEllipseRasterizer.Rasterize(
                left,
                top,
                right,
                bottom,
                coverage.IncludeStamp);
        }

        coverage.Paint(color);
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
