using pixel_editor.Rendering;
using pixel_editor.Tools;
using PixelEditor.Core.Selections;
using Xunit;

namespace PixelEditor.App.Tests.Tools;

public sealed class SelectionGestureTests
{
    [Fact]
    public void BeginUpdateAndCancel_TrackPreviewWithoutSelectionState()
    {
        var gesture = new SelectionGesture();
        var start = new PixelCoordinate(6, 5);
        var end = new PixelCoordinate(1, 2);

        gesture.Begin(start, SelectionCombineMode.Subtract);
        gesture.Update(end);

        Assert.True(gesture.IsActive);
        Assert.Equal(
            new SelectionGestureState(start, end, SelectionCombineMode.Subtract),
            gesture.Current);

        gesture.Cancel();

        Assert.False(gesture.IsActive);
        Assert.Null(gesture.Current);
    }
}
