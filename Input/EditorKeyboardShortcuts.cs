using Avalonia.Input;

namespace pixel_editor.Input;

internal enum EditorShortcut
{
    None,
    New,
    Open,
    Save,
    SaveAs,
    Undo,
    Redo,
    ClearSelection,
    DecreaseBrushSize,
    IncreaseBrushSize
}

internal static class EditorKeyboardShortcuts
{
    public static EditorShortcut Resolve(Key key, KeyModifiers modifiers, bool isMacOs)
    {
        var commandModifier = isMacOs
            ? KeyModifiers.Meta
            : KeyModifiers.Control;

        if (modifiers == KeyModifiers.None && key == Key.Escape)
        {
            return EditorShortcut.ClearSelection;
        }

        if (modifiers == KeyModifiers.None && key is Key.OemMinus or Key.Subtract)
        {
            return EditorShortcut.DecreaseBrushSize;
        }

        if (modifiers == KeyModifiers.None && key is Key.OemPlus or Key.Add)
        {
            return EditorShortcut.IncreaseBrushSize;
        }

        if (key == Key.N && modifiers == commandModifier)
        {
            return EditorShortcut.New;
        }

        if (key == Key.O && modifiers == commandModifier)
        {
            return EditorShortcut.Open;
        }

        if (key == Key.S && modifiers == commandModifier)
        {
            return EditorShortcut.Save;
        }

        if (key == Key.S && modifiers == (commandModifier | KeyModifiers.Shift))
        {
            return EditorShortcut.SaveAs;
        }

        if (key == Key.Z && modifiers == commandModifier)
        {
            return EditorShortcut.Undo;
        }

        if ((key == Key.Z && modifiers == (commandModifier | KeyModifiers.Shift)) ||
            (key == Key.Y && modifiers == commandModifier))
        {
            return EditorShortcut.Redo;
        }

        return EditorShortcut.None;
    }
}
