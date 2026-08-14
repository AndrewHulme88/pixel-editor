using System;
using Avalonia.Controls;
using Avalonia.Input;
using pixel_editor.Input;
using pixel_editor.ViewModels;

namespace pixel_editor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var shortcut = EditorKeyboardShortcuts.Resolve(
            e.Key,
            e.KeyModifiers,
            OperatingSystem.IsMacOS());

        if (shortcut == EditorShortcut.Undo && viewModel.UndoCommand.CanExecute(null))
        {
            viewModel.UndoCommand.Execute(null);
            e.Handled = true;
        }
        else if (shortcut == EditorShortcut.Redo && viewModel.RedoCommand.CanExecute(null))
        {
            viewModel.RedoCommand.Execute(null);
            e.Handled = true;
        }
    }
}
