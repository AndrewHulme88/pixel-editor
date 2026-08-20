namespace PixelEditor.Core.Selections;

public sealed class RectangularSelection
{
    public event EventHandler? Changed;

    public PixelSelectionBounds? Bounds { get; private set; }

    public bool HasSelection => Bounds is not null;

    public bool SelectFromInclusiveCorners(
        int startX,
        int startY,
        int endX,
        int endY,
        int documentWidth,
        int documentHeight) => Replace(
            PixelSelectionBounds.FromInclusiveCorners(
                startX,
                startY,
                endX,
                endY,
                documentWidth,
                documentHeight));

    public bool Replace(PixelSelectionBounds bounds)
    {
        if (Bounds == bounds)
        {
            return false;
        }

        Bounds = bounds;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Clear()
    {
        if (Bounds is null)
        {
            return false;
        }

        Bounds = null;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
