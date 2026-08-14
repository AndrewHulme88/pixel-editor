using Avalonia.Input;

namespace pixel_editor.Input;

internal enum EditorShortcut
{
    None,
    Undo,
    Redo
}

internal static class EditorKeyboardShortcuts
{
    public static EditorShortcut Resolve(Key key, KeyModifiers modifiers, bool isMacOs)
    {
        var commandModifier = isMacOs
            ? KeyModifiers.Meta
            : KeyModifiers.Control;

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
