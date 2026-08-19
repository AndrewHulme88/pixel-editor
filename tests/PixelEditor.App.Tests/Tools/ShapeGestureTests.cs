using pixel_editor.Tools;
using pixel_editor.Rendering;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.App.Tests.Tools;

public sealed class ShapeGestureTests
{
    [Theory]
    [InlineData((int)EditorTool.Rectangle)]
    [InlineData((int)EditorTool.Ellipse)]
    public void BeginAndUpdate_TrackShapePreview(int toolValue)
    {
        var gesture = new ShapeGesture();
        var tool = (EditorTool)toolValue;
        var start = new PixelCoordinate(5, 6);
        var end = new PixelCoordinate(2, 1);

        gesture.Begin(tool, start);
        gesture.Update(end);

        Assert.True(gesture.IsActive);
        Assert.Equal(new ShapeGestureState(tool, start, end), gesture.Current);

        gesture.Cancel();

        Assert.False(gesture.IsActive);
        Assert.Null(gesture.Current);
    }

    [Fact]
    public void Begin_WithNonShapeToolThrows()
    {
        var gesture = new ShapeGesture();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            gesture.Begin(EditorTool.Brush, new PixelCoordinate(1, 1)));
    }
}
