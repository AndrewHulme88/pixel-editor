using pixel_editor.Rendering;

namespace pixel_editor.Tools;

internal sealed class SelectionGesture
{
    public SelectionGestureState? Current { get; private set; }

    public bool IsActive => Current is not null;

    public void Begin(PixelCoordinate start) =>
        Current = new SelectionGestureState(start, start);

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
    PixelCoordinate End);
