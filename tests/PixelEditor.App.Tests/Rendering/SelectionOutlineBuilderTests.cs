using pixel_editor.Rendering;
using PixelEditor.Core.Selections;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class SelectionOutlineBuilderTests
{
    [Fact]
    public void Create_FullRectangleMergesOutlineIntoFourSegments()
    {
        var selection = new PixelSelection(8, 6);
        selection.ReplaceRectangle(new PixelSelectionBounds(1, 2, 5, 3));

        var segments = SelectionOutlineBuilder.Create(selection);

        Assert.Equal(4, segments.Count);
        AssertOutlineMatchesSelection(selection, segments);
    }

    [Fact]
    public void Create_DisconnectedRegionAndHoleFollowExactSelectedPixels()
    {
        var selection = new PixelSelection(10, 8);
        selection.ReplaceRectangle(new PixelSelectionBounds(1, 1, 6, 5));
        selection.SubtractRectangle(new PixelSelectionBounds(3, 2, 2, 2));
        selection.AddRectangle(new PixelSelectionBounds(8, 5, 2, 2));

        var segments = SelectionOutlineBuilder.Create(selection);

        AssertOutlineMatchesSelection(selection, segments);
    }

    private static void AssertOutlineMatchesSelection(
        PixelSelection selection,
        IReadOnlyList<SelectionOutlineSegment> segments)
    {
        var expected = new HashSet<string>();

        for (var y = 0; y < selection.Height; y++)
        {
            for (var x = 0; x < selection.Width; x++)
            {
                if (!selection.Contains(x, y))
                {
                    continue;
                }

                if (!selection.Contains(x, y - 1))
                {
                    expected.Add($"H:{x}:{y}");
                }

                if (!selection.Contains(x, y + 1))
                {
                    expected.Add($"H:{x}:{y + 1}");
                }

                if (!selection.Contains(x - 1, y))
                {
                    expected.Add($"V:{x}:{y}");
                }

                if (!selection.Contains(x + 1, y))
                {
                    expected.Add($"V:{x + 1}:{y}");
                }
            }
        }

        var actual = ExpandToUnitEdges(segments);
        Assert.Equal(expected, actual);
    }

    private static HashSet<string> ExpandToUnitEdges(
        IReadOnlyList<SelectionOutlineSegment> segments)
    {
        var edges = new HashSet<string>();

        foreach (var segment in segments)
        {
            if (segment.StartY == segment.EndY)
            {
                for (var x = segment.StartX; x < segment.EndX; x++)
                {
                    edges.Add($"H:{x}:{segment.StartY}");
                }
            }
            else
            {
                for (var y = segment.StartY; y < segment.EndY; y++)
                {
                    edges.Add($"V:{segment.StartX}:{y}");
                }
            }
        }

        return edges;
    }
}
