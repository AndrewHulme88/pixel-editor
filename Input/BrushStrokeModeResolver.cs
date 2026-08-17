using Avalonia.Input;

namespace pixel_editor.Input;

internal enum BrushStrokeMode
{
    Freehand,
    StraightLine
}

internal static class BrushStrokeModeResolver
{
    public static BrushStrokeMode Resolve(KeyModifiers modifiers) =>
        modifiers.HasFlag(KeyModifiers.Shift)
            ? BrushStrokeMode.StraightLine
            : BrushStrokeMode.Freehand;
}
