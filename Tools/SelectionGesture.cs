using pixel_editor.Rendering;
using PixelEditor.Core.Selections;

namespace pixel_editor.Tools;

internal sealed class SelectionGesture
{
    public SelectionGestureState? Current { get; private set; }

    public bool IsActive => Current is not null;

    public void Begin(PixelCoordinate start, SelectionCombineMode combineMode) =>
        Current = new SelectionGestureState(start, start, combineMode);

    public void Update(PixelCoordinate end)
    {
        if (Current is { } current)
        {
            Current = current with { End = end };
        }
    }

    public void Cancel() => Current = null;
}

internal readonly record struct SelectionGestureState(
    PixelCoordinate Start,
    PixelCoordinate End,
    SelectionCombineMode CombineMode);
