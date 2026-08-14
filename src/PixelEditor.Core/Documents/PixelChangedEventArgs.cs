namespace PixelEditor.Core.Documents;

public sealed class PixelChangedEventArgs : EventArgs
{
    public PixelChangedEventArgs(
        int x,
        int y,
        PixelColor previousColor,
        PixelColor color)
    {
        X = x;
        Y = y;
        PreviousColor = previousColor;
        Color = color;
    }

    public int X { get; }

    public int Y { get; }

    public PixelColor PreviousColor { get; }

    public PixelColor Color { get; }
}
