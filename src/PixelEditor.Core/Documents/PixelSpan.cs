namespace PixelEditor.Core.Documents;

// Identifies a horizontal run of pixels within a document.
public readonly record struct PixelSpan(int X, int Y, int Length);
