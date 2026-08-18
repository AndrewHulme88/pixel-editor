using Avalonia;
using pixel_editor.Rendering;
using Xunit;

namespace PixelEditor.App.Tests.Rendering;

public sealed class CanvasViewportControllerTests
{
    [Fact]
    public void ZoomAndReset_SwitchBetweenExplicitScaleAndFit()
    {
        var controller = new CanvasViewportController();
        var availableSize = new Size(400, 300);

        controller.ZoomAt(
            10,
            10,
            availableSize,
            new Point(200, 150),
            zoomIn: true);

        Assert.Equal(32, controller.PixelScale);
        Assert.Equal(
            CanvasLayout.Calculate(10, 10, availableSize, 32, default),
            controller.CalculateLayout(10, 10, availableSize));

        controller.Reset();

        Assert.Null(controller.PixelScale);
        Assert.False(controller.IsPanning);
        Assert.Equal(
            CanvasLayout.Calculate(10, 10, availableSize),
            controller.CalculateLayout(10, 10, availableSize));
    }

    [Fact]
    public void PanTo_OffsetsLayoutFromPointerMovement()
    {
        var controller = new CanvasViewportController();
        var availableSize = new Size(200, 160);
        var original = controller.CalculateLayout(10, 10, availableSize);

        controller.BeginPan(new Point(50, 60), original);
        controller.PanTo(new Point(65, 52));
        var panned = controller.CalculateLayout(10, 10, availableSize);

        Assert.True(controller.IsPanning);
        Assert.Equal(original.PixelScale, controller.PixelScale);
        Assert.Equal(original.Destination.X + 15, panned.Destination.X);
        Assert.Equal(original.Destination.Y - 8, panned.Destination.Y);

        controller.EndPan();
        Assert.False(controller.IsPanning);
    }

    [Fact]
    public void PanTo_WithoutActivePan_Throws()
    {
        var controller = new CanvasViewportController();

        Assert.Throws<InvalidOperationException>(() =>
            controller.PanTo(new Point(10, 10)));
    }
}
