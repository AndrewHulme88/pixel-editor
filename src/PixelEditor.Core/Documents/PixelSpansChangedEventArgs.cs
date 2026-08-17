namespace PixelEditor.Core.Documents;

public sealed class PixelSpansChangedEventArgs : EventArgs
{
    public PixelSpansChangedEventArgs(
        IReadOnlyList<PixelSpan> spans,
        PixelColor color)
    {
        ArgumentNullException.ThrowIfNull(spans);

        Spans = spans;
        Color = color;
    }

    public IReadOnlyList<PixelSpan> Spans { get; }

    public PixelColor Color { get; }
}
