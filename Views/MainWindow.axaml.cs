using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using pixel_editor.Input;
using pixel_editor.Persistence;
using pixel_editor.ViewModels;

namespace pixel_editor.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType PngFileType = new("PNG image")
    {
        Patterns = ["*.png"],
        MimeTypes = ["image/png"],
        AppleUniformTypeIdentifiers = ["public.png"]
    };

    private IStorageFile? _currentFile;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnKeyDown(KeyEventArgs e)
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

        if (shortcut == EditorShortcut.Open)
        {
            e.Handled = true;
            await OpenDocumentAsync();
        }
        else if (shortcut == EditorShortcut.Save)
        {
            e.Handled = true;
            await SaveDocumentAsync();
        }
        else if (shortcut == EditorShortcut.SaveAs)
        {
            e.Handled = true;
            await SaveDocumentAsAsync();
        }
        else if (shortcut == EditorShortcut.Undo && viewModel.UndoCommand.CanExecute(null))
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

    private async void Open_OnClick(object? sender, RoutedEventArgs e) =>
        await OpenDocumentAsync();

    private async void Save_OnClick(object? sender, RoutedEventArgs e) =>
        await SaveDocumentAsync();

    private async void SaveAs_OnClick(object? sender, RoutedEventArgs e) =>
        await SaveDocumentAsAsync();

    private async Task OpenDocumentAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open PNG",
                AllowMultiple = false,
                FileTypeFilter = [PngFileType],
                SuggestedFileType = PngFileType
            });

            if (files.Count == 0 || DataContext is not MainViewModel viewModel)
            {
                return;
            }

            var file = files[0];
            await using var input = await file.OpenReadAsync();
            var document = PngDocumentCodec.Load(input);

            viewModel.ReplaceDocument(document);
            _currentFile = file;
            UpdateFileDisplay(file.Name, $"Opened {file.Name}");
        }
        catch (OperationCanceledException)
        {
            // Some storage providers report cancellation with an exception.
        }
        catch (Exception exception) when (IsExpectedFileError(exception))
        {
            FileStatusText.Text = $"Open failed: {exception.Message}";
        }
    }

    private async Task SaveDocumentAsync()
    {
        if (_currentFile is null)
        {
            await SaveDocumentAsAsync();
            return;
        }

        await SaveToFileAsync(_currentFile);
    }

    private async Task SaveDocumentAsAsync()
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save PNG",
                SuggestedFileName = _currentFile?.Name ?? "untitled.png",
                DefaultExtension = "png",
                FileTypeChoices = [PngFileType],
                SuggestedFileType = PngFileType,
                ShowOverwritePrompt = true
            });

            if (file is not null)
            {
                await SaveToFileAsync(file);
            }
        }
        catch (OperationCanceledException)
        {
            // Some storage providers report cancellation with an exception.
        }
        catch (Exception exception) when (IsExpectedFileError(exception))
        {
            FileStatusText.Text = $"Save failed: {exception.Message}";
        }
    }

    private async Task SaveToFileAsync(IStorageFile file)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        try
        {
            await using var output = await file.OpenWriteAsync();
            PngDocumentCodec.Save(viewModel.Document, output);
            await output.FlushAsync();

            _currentFile = file;
            UpdateFileDisplay(file.Name, $"Saved {file.Name}");
        }
        catch (OperationCanceledException)
        {
            // Some storage providers report cancellation with an exception.
        }
        catch (Exception exception) when (IsExpectedFileError(exception))
        {
            FileStatusText.Text = $"Save failed: {exception.Message}";
        }
    }

    private void UpdateFileDisplay(string fileName, string status)
    {
        Title = $"{fileName} - Pixel Editor";
        FileStatusText.Text = status;
    }

    private static bool IsExpectedFileError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or NotSupportedException or OverflowException;
}
