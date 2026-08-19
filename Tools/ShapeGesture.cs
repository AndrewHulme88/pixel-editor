using System;
using pixel_editor.Rendering;
using PixelEditor.Core.Tools;

namespace pixel_editor.Tools;

internal sealed class ShapeGesture
{
    public ShapeGestureState? Current { get; private set; }

    public bool IsActive => Current is not null;

    public void Begin(EditorTool tool, PixelCoordinate start)
    {
        if (tool is not EditorTool.Rectangle and not EditorTool.Ellipse)
        {
            throw new ArgumentOutOfRangeException(nameof(tool), tool, "Tool must draw an outline shape.");
        }

        Current = new ShapeGestureState(tool, start, start);
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
    PixelCoordinate Start,
    PixelCoordinate End);
