using PixelEditor.Core.Documents;
using PixelEditor.Core.Selections;
using Xunit;

namespace PixelEditor.Core.Tests.Selections;

public sealed class PixelSelectionTests
{
    [Fact]
    public void NewSelection_IsEmptyAndUsesPackedStorage()
    {
        var selection = new PixelSelection(10, 12);

        Assert.Equal(10, selection.Width);
        Assert.Equal(12, selection.Height);
        Assert.Equal(16, selection.StorageByteCount);
        Assert.Equal(0, selection.SelectedPixelCount);
        Assert.False(selection.HasSelection);
        Assert.Null(selection.Bounds);
    }

    [Fact]
    public void MaximumCanvas_UsesTwoMebibytesOfSelectionStorage()
    {
        var selection = new PixelSelection(
            PixelDocumentLimits.MaximumDimension,
            PixelDocumentLimits.MaximumDimension);

        Assert.Equal(2 * 1024 * 1024, selection.StorageByteCount);
    }

    [Fact]
    public void ReplaceRectangleFromInclusiveCorners_ClipsAndSelectsExactPixels()
    {
        var selection = new PixelSelection(8, 6);

        Assert.True(selection.ReplaceRectangleFromInclusiveCorners(-5, 2, 3, 20));

        Assert.Equal(new PixelSelectionBounds(0, 2, 4, 4), selection.Bounds);
        Assert.Equal(16, selection.SelectedPixelCount);
        Assert.True(selection.Contains(0, 2));
        Assert.True(selection.Contains(3, 5));
        Assert.False(selection.Contains(4, 2));
        Assert.False(selection.Contains(0, 1));
        Assert.False(selection.Contains(-1, 2));
        Assert.False(selection.Contains(0, 6));
    }

    [Fact]
    public void ReplaceRectangleFromInclusiveCorners_ReverseDragMatchesForwardDrag()
    {
        var forward = new PixelSelection(10, 10);
        var reverse = new PixelSelection(10, 10);

        forward.ReplaceRectangleFromInclusiveCorners(1, 2, 7, 6);
        reverse.ReplaceRectangleFromInclusiveCorners(7, 6, 1, 2);

        Assert.Equal(forward.Bounds, reverse.Bounds);
        Assert.Equal(forward.SelectedPixelCount, reverse.SelectedPixelCount);

        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                Assert.Equal(forward.Contains(x, y), reverse.Contains(x, y));
            }
        }
    }

    [Fact]
    public void AddRectangle_CreatesNonRectangularUnionWithoutDoubleCountingOverlap()
    {
        var selection = new PixelSelection(8, 6);
        selection.ReplaceRectangle(new PixelSelectionBounds(1, 1, 2, 2));

        Assert.True(selection.AddRectangle(new PixelSelectionBounds(2, 2, 3, 2)));

        Assert.Equal(new PixelSelectionBounds(1, 1, 4, 3), selection.Bounds);
        Assert.Equal(9, selection.SelectedPixelCount);
        Assert.True(selection.Contains(1, 1));
        Assert.True(selection.Contains(4, 3));
        Assert.False(selection.Contains(4, 1));
        Assert.False(selection.Contains(1, 3));
    }

    [Fact]
    public void SubtractRectangle_CreatesHoleAndRecalculatesBounds()
    {
        var selection = new PixelSelection(8, 6);
        selection.ReplaceRectangle(new PixelSelectionBounds(1, 1, 5, 4));

        Assert.True(selection.SubtractRectangle(new PixelSelectionBounds(3, 1, 1, 3)));

        Assert.Equal(new PixelSelectionBounds(1, 1, 5, 4), selection.Bounds);
        Assert.Equal(17, selection.SelectedPixelCount);
        Assert.False(selection.Contains(3, 1));
        Assert.False(selection.Contains(3, 3));
        Assert.True(selection.Contains(3, 4));

        Assert.True(selection.SubtractRectangle(new PixelSelectionBounds(1, 1, 5, 4)));
        Assert.False(selection.HasSelection);
        Assert.Null(selection.Bounds);
    }

    [Fact]
    public void IntersectRectangle_PreservesDisconnectedPixelsInsideIntersection()
    {
        var selection = new PixelSelection(8, 4);
        selection.ReplaceRectangle(new PixelSelectionBounds(0, 0, 2, 2));
        selection.AddRectangle(new PixelSelectionBounds(3, 0, 2, 2));

        Assert.True(selection.IntersectRectangle(new PixelSelectionBounds(1, 0, 3, 3)));

        Assert.Equal(new PixelSelectionBounds(1, 0, 3, 2), selection.Bounds);
        Assert.Equal(4, selection.SelectedPixelCount);
        Assert.True(selection.Contains(1, 0));
        Assert.True(selection.Contains(3, 1));
        Assert.False(selection.Contains(2, 0));
    }

    [Fact]
    public void Operations_RaiseChangedOnlyWhenSelectedPixelsChange()
    {
        var selection = new PixelSelection(8, 6);
        var bounds = new PixelSelectionBounds(1, 2, 3, 2);
        var changedCount = 0;
        selection.Changed += (_, _) => changedCount++;

        Assert.True(selection.ReplaceRectangle(bounds));
        Assert.False(selection.ReplaceRectangle(bounds));
        Assert.False(selection.AddRectangle(bounds));
        Assert.False(selection.SubtractRectangle(new PixelSelectionBounds(6, 0, 1, 1)));
        Assert.False(selection.IntersectRectangle(new PixelSelectionBounds(0, 0, 8, 6)));
        Assert.True(selection.Clear());
        Assert.False(selection.Clear());

        Assert.Equal(2, changedCount);
    }

    [Fact]
    public void ResetCanvas_ClearsSelectionAndChangesDimensions()
    {
        var selection = new PixelSelection(8, 6);
        selection.ReplaceRectangle(new PixelSelectionBounds(1, 1, 2, 2));

        Assert.True(selection.ResetCanvas(12, 10));

        Assert.Equal(12, selection.Width);
        Assert.Equal(10, selection.Height);
        Assert.False(selection.HasSelection);
        Assert.Null(selection.Bounds);
        Assert.False(selection.Contains(1, 1));
        Assert.False(selection.ResetCanvas(12, 10));
    }

    [Fact]
    public void RectangleOperation_OutsideCanvasThrows()
    {
        var selection = new PixelSelection(8, 6);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            selection.ReplaceRectangle(new PixelSelectionBounds(7, 5, 2, 2)));
    }

    [Theory]
    [InlineData(63, 5)]
    [InlineData(64, 5)]
    [InlineData(65, 5)]
    [InlineData(127, 3)]
    public void RectangleOperations_MatchReferenceMaskAcrossWordBoundaries(
        int width,
        int height)
    {
        var selection = new PixelSelection(width, height);
        var expected = new bool[width, height];
        var random = new Random(20260821 + width);

        for (var iteration = 0; iteration < 80; iteration++)
        {
            var x = random.Next(width);
            var y = random.Next(height);
            var bounds = new PixelSelectionBounds(
                x,
                y,
                random.Next(1, width - x + 1),
                random.Next(1, height - y + 1));
            var operation = iteration % 4;

            if (operation == 0)
            {
                selection.ReplaceRectangle(bounds);
            }
            else if (operation == 1)
            {
                selection.AddRectangle(bounds);
            }
            else if (operation == 2)
            {
                selection.SubtractRectangle(bounds);
            }
            else
            {
                selection.IntersectRectangle(bounds);
            }

            ApplyToReferenceMask(expected, bounds, operation);
            AssertMatchesReferenceMask(selection, expected);
        }
    }

    private static void ApplyToReferenceMask(
        bool[,] pixels,
        PixelSelectionBounds bounds,
        int operation)
    {
        for (var y = 0; y < pixels.GetLength(1); y++)
        {
            for (var x = 0; x < pixels.GetLength(0); x++)
            {
                var isInside = x >= bounds.X &&
                               x < bounds.Right &&
                               y >= bounds.Y &&
                               y < bounds.Bottom;

                pixels[x, y] = operation switch
                {
                    0 => isInside,
                    1 => pixels[x, y] || isInside,
                    2 => pixels[x, y] && !isInside,
                    _ => pixels[x, y] && isInside
                };
            }
        }
    }

    private static void AssertMatchesReferenceMask(
        PixelSelection selection,
        bool[,] expected)
    {
        var selectedCount = 0;
        var left = expected.GetLength(0);
        var top = expected.GetLength(1);
        var right = -1;
        var bottom = -1;

        for (var y = 0; y < expected.GetLength(1); y++)
        {
            for (var x = 0; x < expected.GetLength(0); x++)
            {
                Assert.Equal(expected[x, y], selection.Contains(x, y));

                if (!expected[x, y])
                {
                    continue;
                }

                selectedCount++;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        var expectedBounds = selectedCount == 0
            ? (PixelSelectionBounds?)null
            : new PixelSelectionBounds(
                left,
                top,
                (right - left) + 1,
                (bottom - top) + 1);

        Assert.Equal(selectedCount, selection.SelectedPixelCount);
        Assert.Equal(expectedBounds, selection.Bounds);
        Assert.Equal(selectedCount != 0, selection.HasSelection);
        AssertSpansMatchReferenceMask(selection, expected, selectedCount);
    }

    private static void AssertSpansMatchReferenceMask(
        PixelSelection selection,
        bool[,] expected,
        int expectedPixelCount)
    {
        var spanPixelCount = 0;
        PixelSpan? previous = null;

        foreach (var span in selection.CreateSpans())
        {
            Assert.True(span.Length > 0);

            if (previous is { } previousSpan && previousSpan.Y == span.Y)
            {
                Assert.True(previousSpan.X + previousSpan.Length < span.X);
            }

            for (var x = span.X; x < span.X + span.Length; x++)
            {
                Assert.True(expected[x, span.Y]);
            }

            spanPixelCount += span.Length;
            previous = span;
        }

        Assert.Equal(expectedPixelCount, spanPixelCount);
    }
}
