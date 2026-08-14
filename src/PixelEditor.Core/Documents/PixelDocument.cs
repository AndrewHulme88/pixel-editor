namespace PixelEditor.Core.Documents;

// Stores the editable pixel data for a single image.
public sealed class PixelDocument
{
    private readonly PixelColor[] _pixels;

    public event EventHandler<PixelChangedEventArgs>? PixelChanged;

    public PixelDocument(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        Width = width;
        Height = height;
        _pixels = new PixelColor[checked(width * height)];
    }

    public int Width { get; }

    public int Height { get; }

    public PixelColor GetPixel(int x, int y) => _pixels[GetIndex(x, y)];

    public void SetPixel(int x, int y, PixelColor color)
    {
        var index = GetIndex(x, y);
        var previousColor = _pixels[index];

        if (previousColor == color)
        {
            return;
        }

        _pixels[index] = color;
        PixelChanged?.Invoke(this, new PixelChangedEventArgs(x, y, previousColor, color));
    }

    private int GetIndex(int x, int y)
    {
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be within the document bounds.");
        }

        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be within the document bounds.");
        }

        return (y * Width) + x;
    }
}
