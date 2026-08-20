using System;
using pixel_editor.Rendering;
using PixelEditor.Core.Tools;

namespace pixel_editor.Tools;

internal sealed class ShapeGesture
{
    public ShapeGestureState? Current { get; private set; }

    public bool IsActive => Current is not null;

    public void Begin(
        EditorTool tool,
        ShapeDrawMode mode,
        PixelCoordinate start)
    {
        if (tool is not EditorTool.Rectangle and not EditorTool.Ellipse)
        {
            throw new ArgumentOutOfRangeException(nameof(tool), tool, "Tool must draw a shape.");
        }

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        Current = new ShapeGestureState(tool, mode, start, start);
    }

    public void Update(PixelCoordinate end)
    {
        if (Current is { } current)
        {
            Current = current with { End = end };
        }
    }

    public void Cancel() => Current = null;
}

internal readonly record struct ShapeGestureState(
    EditorTool Tool,
    ShapeDrawMode Mode,
    PixelCoordinate Start,
    PixelCoordinate End);
