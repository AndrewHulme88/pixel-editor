using Avalonia.Input;
using PixelEditor.Core.Selections;

namespace pixel_editor.Input;

internal static class SelectionInputResolver
{
    public static bool ShouldSampleColor(KeyModifiers modifiers) =>
        modifiers.HasFlag(KeyModifiers.Alt) &&
        !modifiers.HasFlag(KeyModifiers.Shift);

    public static SelectionCombineMode ResolveCombineMode(KeyModifiers modifiers)
    {
        var isShiftPressed = modifiers.HasFlag(KeyModifiers.Shift);
        var isAltPressed = modifiers.HasFlag(KeyModifiers.Alt);

        if (isShiftPressed && isAltPressed)
        {
            return SelectionCombineMode.Subtract;
        }

        return isShiftPressed
            ? SelectionCombineMode.Add
            : SelectionCombineMode.Replace;
    }
}
