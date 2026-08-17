using PixelEditor.Core.Documents;

namespace PixelEditor.Core.Tools;

public sealed class FillResult
{
    internal FillResult(
        PixelSpan[] spans,
        PixelColor previousColor,
        PixelColor color,
        int filledPixelCount)
    {
        Spans = spans;
        PreviousColor = previousColor;
        Color = color;
        FilledPixelCount = filledPixelCount;
    }

    public IReadOnlyList<PixelSpan> Spans { get; }

    public PixelColor PreviousColor { get; }

    public PixelColor Color { get; }

    public int FilledPixelCount { get; }
}
