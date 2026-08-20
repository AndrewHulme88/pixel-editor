using PixelEditor.Core.Selections;
using Xunit;

namespace PixelEditor.Core.Tests.Selections;

public sealed class RectangularSelectionTests
{
    [Fact]
    public void NewSelection_HasNoBounds()
    {
        var selection = new RectangularSelection();

        Assert.False(selection.HasSelection);
        Assert.Null(selection.Bounds);
        Assert.False(selection.Clear());
    }

    [Fact]
    public void SelectFromInclusiveCorners_CreatesHalfOpenBounds()
    {
        var selection = new RectangularSelection();

        Assert.True(selection.SelectFromInclusiveCorners(2, 3, 6, 8, 10, 12));

        Assert.Equal(new PixelSelectionBounds(2, 3, 5, 6), selection.Bounds);
        Assert.Equal(7, selection.Bounds!.Value.Right);
        Assert.Equal(9, selection.Bounds!.Value.Bottom);
    }

    [Fact]
    public void SelectFromInclusiveCorners_WithReverseDragMatchesForwardDrag()
    {
        var forward = new RectangularSelection();
        var reverse = new RectangularSelection();

        forward.SelectFromInclusiveCorners(1, 2, 7, 6, 10, 10);
        reverse.SelectFromInclusiveCorners(7, 6, 1, 2, 10, 10);

        Assert.Equal(forward.Bounds, reverse.Bounds);
    }

    [Fact]
    public void SelectFromInclusiveCorners_ClipsToDocumentBounds()
    {
        var selection = new RectangularSelection();

        selection.SelectFromInclusiveCorners(-5, -2, 20, 30, 8, 6);

        Assert.Equal(new PixelSelectionBounds(0, 0, 8, 6), selection.Bounds);
    }

    [Fact]
    public void SelectFromInclusiveCorners_WithOnePointSelectsOnePixel()
    {
        var selection = new RectangularSelection();

        selection.SelectFromInclusiveCorners(4, 5, 4, 5, 8, 8);

        Assert.Equal(new PixelSelectionBounds(4, 5, 1, 1), selection.Bounds);
    }

    [Fact]
    public void ReplaceAndClear_RaiseChangedOnlyForStateChanges()
    {
        var selection = new RectangularSelection();
        var bounds = new PixelSelectionBounds(1, 2, 3, 4);
        var changedCount = 0;
        selection.Changed += (_, _) => changedCount++;

        Assert.True(selection.Replace(bounds));
        Assert.False(selection.Replace(bounds));
        Assert.True(selection.Clear());
        Assert.False(selection.Clear());

        Assert.Equal(2, changedCount);
    }
}
