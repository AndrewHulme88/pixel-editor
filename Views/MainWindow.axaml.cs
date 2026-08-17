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
    private bool _allowClose;
    private bool _closeConfirmationIsOpen;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
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

        if (shortcut == EditorShortcut.New)
        {
            e.Handled = true;
            await NewDocumentAsync();
        }
        else if (shortcut == EditorShortcut.Open)
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

    private async void New_OnClick(object? sender, RoutedEventArgs e) =>
        await NewDocumentAsync();

    private async void Open_OnClick(object? sender, RoutedEventArgs e) =>
        await OpenDocumentAsync();

    private async void Save_OnClick(object? sender, RoutedEventArgs e) =>
        await SaveDocumentAsync();

    private async void SaveAs_OnClick(object? sender, RoutedEventArgs e) =>
        await SaveDocumentAsAsync();

    private async void ResizeCanvas_OnClick(object? sender, RoutedEventArgs e) =>
        await ResizeCanvasAsync();

    private void ZoomOut_OnClick(object? sender, RoutedEventArgs e) =>
        EditorCanvas.ZoomOut();

    private void ZoomIn_OnClick(object? sender, RoutedEventArgs e) =>
        EditorCanvas.ZoomIn();

    private void ResetZoom_OnClick(object? sender, RoutedEventArgs e) =>
        EditorCanvas.ResetView();

    private async Task ResizeCanvasAsync()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new CanvasResizeDialog(
            viewModel.Document.Width,
            viewModel.Document.Height);
        var settings = await dialog.ShowDialog<CanvasResizeSettings?>(this);

        if (settings is { } selected &&
            viewModel.ResizeDocument(selected.Width, selected.Height, selected.Anchor))
        {
            FileStatusText.Text = $"Resized canvas to {selected.Width} × {selected.Height}";
        }
    }

    private async Task NewDocumentAsync()
    {
        var dialog = new NewDocumentDialog();
        var size = await dialog.ShowDialog<NewDocumentSize?>(this);

        if (size is not { } selectedSize || !await ConfirmCanDiscardChangesAsync())
        {
            return;
        }

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.CreateNewDocument(selectedSize.Width, selectedSize.Height);
            _currentFile = null;
            FileStatusText.Text = $"Created {selectedSize.Width} × {selectedSize.Height} document";
        }
    }

    private async Task OpenDocumentAsync()
    {
        if (!await ConfirmCanDiscardChangesAsync())
        {
            return;
        }

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

            viewModel.ReplaceDocument(document, file.Name);
            _currentFile = file;
            FileStatusText.Text = $"Opened {file.Name}";
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

    private async Task<bool> SaveDocumentAsync()
    {
        if (_currentFile is null)
        {
            return await SaveDocumentAsAsync();
        }

        return await SaveToFileAsync(_currentFile);
    }

    private async Task<bool> SaveDocumentAsAsync()
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
                return await SaveToFileAsync(file);
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

        return false;
    }

    private async Task<bool> SaveToFileAsync(IStorageFile file)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return false;
        }

        try
        {
            await using var output = await file.OpenWriteAsync();
            PngDocumentCodec.Save(viewModel.Document, output);
            await output.FlushAsync();

            _currentFile = file;
            viewModel.MarkDocumentSaved(file.Name);
            FileStatusText.Text = $"Saved {file.Name}";
            return true;
        }
        catch (OperationCanceledException)
        {
            // Some storage providers report cancellation with an exception.
        }
        catch (Exception exception) when (IsExpectedFileError(exception))
        {
            FileStatusText.Text = $"Save failed: {exception.Message}";
        }

        return false;
    }

    private async Task<bool> ConfirmCanDiscardChangesAsync()
    {
        if (DataContext is not MainViewModel { IsDirty: true } viewModel)
        {
            return true;
        }

        var dialog = new UnsavedChangesDialog(viewModel.DocumentName);
        var choice = await dialog.ShowDialog<UnsavedChangesChoice>(this);

        return choice switch
        {
            UnsavedChangesChoice.Save => await SaveDocumentAsync(),
            UnsavedChangesChoice.DontSave => true,
            _ => false
        };
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not MainViewModel { IsDirty: true })
        {
            return;
        }

        e.Cancel = true;

        if (_closeConfirmationIsOpen)
        {
            return;
        }

        _closeConfirmationIsOpen = true;

        try
        {
            if (await ConfirmCanDiscardChangesAsync())
            {
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _closeConfirmationIsOpen = false;
        }
    }

    private static bool IsExpectedFileError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or NotSupportedException or OverflowException;
}
