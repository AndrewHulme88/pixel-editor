namespace PixelEditor.Core.Documents;

// Stores the editable pixel data for a single image.
public sealed class PixelDocument
{
    private readonly PixelColor[] _pixels;

    public event EventHandler<PixelChangedEventArgs>? PixelChanged;

    public event EventHandler<PixelSpansChangedEventArgs>? PixelSpansChanged;

    public event EventHandler<PixelPatchChangedEventArgs>? PixelPatchChanged;

    public PixelDocument(int width, int height)
    {
        PixelDocumentLimits.ValidateDimensions(width, height);

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

    public void SetPixelSpans(IReadOnlyList<PixelSpan> spans, PixelColor color)
    {
        ArgumentNullException.ThrowIfNull(spans);

        foreach (var span in spans)
        {
            ValidateSpan(span);
        }

        ApplyPixelSpans(spans, color);
    }

    internal void CopyRegionTo(
        PixelDocument destination,
        int sourceX,
        int sourceY,
        int destinationX,
        int destinationY,
        int width,
        int height)
    {
        for (var row = 0; row < height; row++)
        {
            Array.Copy(
                _pixels,
                ((sourceY + row) * Width) + sourceX,
                destination._pixels,
                ((destinationY + row) * destination.Width) + destinationX,
                width);
        }
    }

    internal void SetPixelSpanWithoutNotification(PixelSpan span, PixelColor color)
    {
        Array.Fill(
            _pixels,
            color,
            (span.Y * Width) + span.X,
            span.Length);
    }

    internal void SetPixelSpanWithoutNotification(
        PixelSpan span,
        ReadOnlySpan<PixelColor> colors)
    {
        colors.CopyTo(_pixels.AsSpan((span.Y * Width) + span.X, span.Length));
    }

    internal PixelColor[] CopyPixelSpan(PixelSpan span)
    {
        var colors = new PixelColor[span.Length];
        _pixels.AsSpan((span.Y * Width) + span.X, span.Length).CopyTo(colors);
        return colors;
    }

    internal void ApplyPixelSpans(
        IReadOnlyList<PixelSpan> spans,
        PixelColor color)
    {
        foreach (var span in spans)
        {
            SetPixelSpanWithoutNotification(span, color);
        }

        NotifyPixelSpansChanged(spans, color);
    }

    internal void NotifyPixelSpansChanged(
        IReadOnlyList<PixelSpan> spans,
        PixelColor color)
    {
        if (spans.Count > 0)
        {
            PixelSpansChanged?.Invoke(this, new PixelSpansChangedEventArgs(spans, color));
        }
    }

    internal void NotifyPixelPatchChanged(IReadOnlyList<PixelSpan> spans)
    {
        if (spans.Count > 0)
        {
            PixelPatchChanged?.Invoke(this, new PixelPatchChangedEventArgs(spans));
        }
    }

    private void ValidateSpan(PixelSpan span)
    {
        if (span.X < 0 ||
            (uint)span.Y >= (uint)Height ||
            span.Length <= 0 ||
            span.X > Width - span.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(span),
                "Every span must fit within the document.");
        }
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
