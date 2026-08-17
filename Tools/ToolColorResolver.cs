using System;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;

namespace pixel_editor.Tools;

internal static class ToolColorResolver
{
    public static PixelColor Resolve(EditorTool tool, PixelColor brushColor) => tool switch
    {
        EditorTool.Brush => brushColor,
        EditorTool.Eraser => PixelColor.Transparent,
        EditorTool.Fill => brushColor,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unsupported editor tool.")
    };
}
