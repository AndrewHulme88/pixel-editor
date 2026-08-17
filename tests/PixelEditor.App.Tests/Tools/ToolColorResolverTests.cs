using pixel_editor.Tools;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;
using Xunit;

namespace PixelEditor.App.Tests.Tools;

public sealed class ToolColorResolverTests
{
    private static readonly PixelColor BrushColor = new(10, 20, 30);

    [Fact]
    public void Resolve_WithBrush_ReturnsBrushColor()
    {
        var color = ToolColorResolver.Resolve(EditorTool.Brush, BrushColor);

        Assert.Equal(BrushColor, color);
    }

    [Fact]
    public void Resolve_WithEraser_ReturnsTransparent()
    {
        var color = ToolColorResolver.Resolve(EditorTool.Eraser, BrushColor);

        Assert.Equal(PixelColor.Transparent, color);
    }

    [Fact]
    public void Resolve_WithFill_ReturnsBrushColor()
    {
        var color = ToolColorResolver.Resolve(EditorTool.Fill, BrushColor);

        Assert.Equal(BrushColor, color);
    }

    [Fact]
    public void Resolve_WithUnsupportedTool_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ToolColorResolver.Resolve((EditorTool)999, BrushColor));
    }
}
