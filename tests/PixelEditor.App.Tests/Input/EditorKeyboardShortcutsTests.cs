using Avalonia.Input;
using pixel_editor.Input;
using Xunit;

namespace PixelEditor.App.Tests.Input;

public sealed class EditorKeyboardShortcutsTests
{
    [Theory]
    [InlineData(Key.Z, KeyModifiers.Control, false, (int)EditorShortcut.Undo)]
    [InlineData(Key.Y, KeyModifiers.Control, false, (int)EditorShortcut.Redo)]
    [InlineData(Key.Z, KeyModifiers.Control | KeyModifiers.Shift, false, (int)EditorShortcut.Redo)]
    [InlineData(Key.Z, KeyModifiers.Meta, true, (int)EditorShortcut.Undo)]
    [InlineData(Key.Y, KeyModifiers.Meta, true, (int)EditorShortcut.Redo)]
    [InlineData(Key.Z, KeyModifiers.Meta | KeyModifiers.Shift, true, (int)EditorShortcut.Redo)]
    public void Resolve_WithSupportedShortcut_ReturnsExpectedAction(
        Key key,
        KeyModifiers modifiers,
        bool isMacOs,
        int expected)
    {
        var shortcut = EditorKeyboardShortcuts.Resolve(key, modifiers, isMacOs);

        Assert.Equal((EditorShortcut)expected, shortcut);
    }

    [Theory]
    [InlineData(Key.Z, KeyModifiers.Meta, false)]
    [InlineData(Key.Z, KeyModifiers.Control, true)]
    [InlineData(Key.Z, KeyModifiers.Shift, false)]
    [InlineData(Key.Y, KeyModifiers.Control | KeyModifiers.Shift, false)]
    [InlineData(Key.Y, KeyModifiers.Meta | KeyModifiers.Shift, true)]
    [InlineData(Key.X, KeyModifiers.Control, false)]
    public void Resolve_WithUnsupportedShortcut_ReturnsNone(
        Key key,
        KeyModifiers modifiers,
        bool isMacOs)
    {
        var shortcut = EditorKeyboardShortcuts.Resolve(key, modifiers, isMacOs);

        Assert.Equal(EditorShortcut.None, shortcut);
    }
}
