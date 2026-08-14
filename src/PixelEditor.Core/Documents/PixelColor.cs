namespace PixelEditor.Core.Documents;

// Represents a pixel using red, green, blue, and alpha channels.
public readonly record struct PixelColor(
    byte Red,
    byte Green,
    byte Blue,
    byte Alpha = byte.MaxValue)
{
    public static PixelColor Transparent { get; } = new(0, 0, 0, 0);
}
