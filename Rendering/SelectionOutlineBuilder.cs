using System;
using System.Collections.Generic;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Selections;

namespace pixel_editor.Rendering;

internal static class SelectionOutlineBuilder
{
    public static IReadOnlyList<SelectionOutlineSegment> Create(PixelSelection selection)
    {
        if (selection.Bounds is not { } bounds)
        {
            return Array.Empty<SelectionOutlineSegment>();
        }

        var spans = selection.CreateSpans();
        var rowStarts = CreateRowStarts(spans, bounds);
        var segments = new List<SelectionOutlineSegment>();

        for (var row = 0; row < bounds.Height; row++)
        {
            var currentStart = rowStarts[row];
            var currentEnd = rowStarts[row + 1];
            var previousStart = row == 0 ? 0 : rowStarts[row - 1];
            var previousEnd = row == 0 ? 0 : currentStart;
            var nextStart = currentEnd;
            var nextEnd = row == bounds.Height - 1
                ? currentEnd
                : rowStarts[row + 2];
            var y = bounds.Y + row;

            AppendHorizontalDifference(
                spans,
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                y,
                segments);
            AppendHorizontalDifference(
                spans,
                currentStart,
                currentEnd,
                nextStart,
                nextEnd,
                y + 1,
                segments);
        }

        AppendVerticalSegments(spans, rowStarts, bounds, segments);
        return segments;
    }

    private static int[] CreateRowStarts(
        IReadOnlyList<PixelSpan> spans,
        PixelSelectionBounds bounds)
    {
        var rowStarts = new int[bounds.Height + 1];
        var spanIndex = 0;

        for (var row = 0; row < bounds.Height; row++)
        {
            rowStarts[row] = spanIndex;
            var y = bounds.Y + row;

            while (spanIndex < spans.Count && spans[spanIndex].Y == y)
            {
                spanIndex++;
            }
        }

        rowStarts[^1] = spanIndex;
        return rowStarts;
    }

    private static void AppendHorizontalDifference(
        IReadOnlyList<PixelSpan> spans,
        int sourceStart,
        int sourceEnd,
        int maskStart,
        int maskEnd,
        int y,
        ICollection<SelectionOutlineSegment> segments)
    {
        var maskIndex = maskStart;

        for (var sourceIndex = sourceStart; sourceIndex < sourceEnd; sourceIndex++)
        {
            var source = spans[sourceIndex];
            var cursor = source.X;
            var sourceRight = source.X + source.Length;

            while (maskIndex < maskEnd &&
                   spans[maskIndex].X + spans[maskIndex].Length <= cursor)
            {
                maskIndex++;
            }

            var currentMaskIndex = maskIndex;

            while (currentMaskIndex < maskEnd &&
                   spans[currentMaskIndex].X < sourceRight)
            {
                var mask = spans[currentMaskIndex];

                if (mask.X > cursor)
                {
                    AppendHorizontalSegment(
                        cursor,
                        Math.Min(mask.X, sourceRight),
                        y,
                        segments);
                }

                cursor = Math.Max(cursor, mask.X + mask.Length);

                if (cursor >= sourceRight)
                {
                    break;
                }

                currentMaskIndex++;
            }

            AppendHorizontalSegment(cursor, sourceRight, y, segments);
        }
    }

    private static void AppendHorizontalSegment(
        int left,
        int right,
        int y,
        ICollection<SelectionOutlineSegment> segments)
    {
        if (left < right)
        {
            segments.Add(new SelectionOutlineSegment(left, y, right, y));
        }
    }

    private static void AppendVerticalSegments(
        IReadOnlyList<PixelSpan> spans,
        IReadOnlyList<int> rowStarts,
        PixelSelectionBounds bounds,
        ICollection<SelectionOutlineSegment> segments)
    {
        var activeEdges = new Dictionary<int, int>();
        var currentEdges = new HashSet<int>();
        var completedEdges = new List<int>();

        for (var row = 0; row < bounds.Height; row++)
        {
            currentEdges.Clear();

            for (var index = rowStarts[row]; index < rowStarts[row + 1]; index++)
            {
                var span = spans[index];
                currentEdges.Add(span.X);
                currentEdges.Add(span.X + span.Length);
            }

            completedEdges.Clear();
            var y = bounds.Y + row;

            foreach (var edge in activeEdges)
            {
                if (!currentEdges.Contains(edge.Key))
                {
                    segments.Add(new SelectionOutlineSegment(
                        edge.Key,
                        edge.Value,
                        edge.Key,
                        y));
                    completedEdges.Add(edge.Key);
                }
            }

            foreach (var x in completedEdges)
            {
                activeEdges.Remove(x);
            }

            foreach (var x in currentEdges)
            {
                activeEdges.TryAdd(x, y);
            }
        }

        foreach (var edge in activeEdges)
        {
            segments.Add(new SelectionOutlineSegment(
                edge.Key,
                edge.Value,
                edge.Key,
                bounds.Bottom));
        }
    }
}

internal readonly record struct SelectionOutlineSegment(
    int StartX,
    int StartY,
    int EndX,
    int EndY);
