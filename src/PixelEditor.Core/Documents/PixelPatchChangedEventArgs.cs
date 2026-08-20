namespace PixelEditor.Core.Documents;

public sealed class PixelPatchChangedEventArgs : EventArgs
{
    public PixelPatchChangedEventArgs(IReadOnlyList<PixelSpan> spans)
    {
        ArgumentNullException.ThrowIfNull(spans);
        Spans = spans;
    }

    public IReadOnlyList<PixelSpan> Spans { get; }
}
