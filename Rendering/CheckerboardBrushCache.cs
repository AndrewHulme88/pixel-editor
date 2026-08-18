using Avalonia;
using Avalonia.Media;

namespace pixel_editor.Rendering;

internal sealed class CheckerboardBrushCache
{
    private static readonly IBrush LightBrush =
        new SolidColorBrush(Color.FromRgb(214, 214, 214));

    private static readonly IBrush DarkBrush =
        new SolidColorBrush(Color.FromRgb(174, 174, 174));

    private static readonly Drawing TileDrawing = CreateTileDrawing();

    private readonly DrawingBrush _brush = new(TileDrawing)
    {
        AlignmentX = AlignmentX.Left,
        AlignmentY = AlignmentY.Top,
        DestinationRect = new RelativeRect(
            new Rect(0, 0, 2, 2),
            RelativeUnit.Absolute),
        SourceRect = new RelativeRect(
            new Rect(0, 0, 2, 2),
            RelativeUnit.Absolute),
        Stretch = Stretch.Fill,
        TileMode = TileMode.Tile
    };

    public DrawingBrush GetBrush() => _brush;

    private static Drawing CreateTileDrawing()
    {
        var drawing = new DrawingGroup();
        drawing.Children.Add(CreateRectangle(LightBrush, new Rect(0, 0, 2, 2)));
        drawing.Children.Add(CreateRectangle(DarkBrush, new Rect(1, 0, 1, 1)));
        drawing.Children.Add(CreateRectangle(DarkBrush, new Rect(0, 1, 1, 1)));
        return drawing;
    }

    private static GeometryDrawing CreateRectangle(IBrush brush, Rect bounds) => new()
    {
        Brush = brush,
        Geometry = new RectangleGeometry(bounds)
    };
}
