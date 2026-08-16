using Avalonia.Controls;
using Avalonia.Interactivity;

namespace pixel_editor.Views;

internal enum UnsavedChangesChoice
{
    Cancel,
    Save,
    DontSave
}

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    internal UnsavedChangesDialog(string documentName)
        : this()
    {
        MessageText.Text = $"Do you want to save the changes to {documentName} before continuing?";
    }

    private void Save_OnClick(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Save);

    private void DontSave_OnClick(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.DontSave);

    private void Cancel_OnClick(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Cancel);
}
