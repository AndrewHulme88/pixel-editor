using Avalonia;

namespace pixel_editor.Rendering;

internal readonly record struct CheckerboardRenderLayoutResult(
    Rect DocumentBounds,
    Matrix DocumentToScreen);

internal static class CheckerboardRenderLayout
{
    public static CheckerboardRenderLayoutResult Calculate(CanvasLayoutResult canvasLayout)
    {
        var destination = canvasLayout.Destination;
        var documentBounds = new Rect(
            0,
            0,
            destination.Width / canvasLayout.PixelScale,
            destination.Height / canvasLayout.PixelScale);
        var documentToScreen = Matrix
            .CreateScale(canvasLayout.PixelScale, canvasLayout.PixelScale)
            .Append(Matrix.CreateTranslation(destination.X, destination.Y));

        return new CheckerboardRenderLayoutResult(documentBounds, documentToScreen);
    }
}
