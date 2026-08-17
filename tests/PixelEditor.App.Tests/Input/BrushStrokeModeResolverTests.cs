using Avalonia.Input;
using pixel_editor.Input;
using Xunit;

namespace PixelEditor.App.Tests.Input;

public sealed class BrushStrokeModeResolverTests
{
    [Theory]
    [InlineData(KeyModifiers.None, (int)BrushStrokeMode.Freehand)]
    [InlineData(KeyModifiers.Control, (int)BrushStrokeMode.Freehand)]
    [InlineData(KeyModifiers.Shift, (int)BrushStrokeMode.StraightLine)]
    [InlineData(KeyModifiers.Shift | KeyModifiers.Alt, (int)BrushStrokeMode.StraightLine)]
    public void Resolve_ReturnsModeForModifiers(
        KeyModifiers modifiers,
        int expected)
    {
        var mode = BrushStrokeModeResolver.Resolve(modifiers);

        Assert.Equal((BrushStrokeMode)expected, mode);
    }
}
