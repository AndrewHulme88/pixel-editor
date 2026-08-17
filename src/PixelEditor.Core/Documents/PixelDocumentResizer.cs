namespace PixelEditor.Core.Documents;

public static class PixelDocumentResizer
{
    public static PixelDocument Resize(
        PixelDocument source,
        int width,
        int height,
        CanvasAnchor anchor)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!Enum.IsDefined(anchor))
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        var resized = new PixelDocument(width, height);
        var offsetX = GetHorizontalOffset(width - source.Width, anchor);
        var offsetY = GetVerticalOffset(height - source.Height, anchor);
        var sourceX = Math.Max(0, -offsetX);
        var sourceY = Math.Max(0, -offsetY);
        var destinationX = Math.Max(0, offsetX);
        var destinationY = Math.Max(0, offsetY);
        var copyWidth = Math.Min(source.Width - sourceX, width - destinationX);
        var copyHeight = Math.Min(source.Height - sourceY, height - destinationY);

        if (copyWidth > 0 && copyHeight > 0)
        {
            source.CopyRegionTo(
                resized,
                sourceX,
                sourceY,
                destinationX,
                destinationY,
                copyWidth,
                copyHeight);
        }

        return resized;
    }

    private static int GetHorizontalOffset(int difference, CanvasAnchor anchor) => anchor switch
    {
        CanvasAnchor.TopLeft or CanvasAnchor.Left or CanvasAnchor.BottomLeft => 0,
        CanvasAnchor.Top or CanvasAnchor.Center or CanvasAnchor.Bottom => difference / 2,
        CanvasAnchor.TopRight or CanvasAnchor.Right or CanvasAnchor.BottomRight => difference,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor))
    };

    private static int GetVerticalOffset(int difference, CanvasAnchor anchor) => anchor switch
    {
        CanvasAnchor.TopLeft or CanvasAnchor.Top or CanvasAnchor.TopRight => 0,
        CanvasAnchor.Left or CanvasAnchor.Center or CanvasAnchor.Right => difference / 2,
        CanvasAnchor.BottomLeft or CanvasAnchor.Bottom or CanvasAnchor.BottomRight => difference,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor))
    };
}
