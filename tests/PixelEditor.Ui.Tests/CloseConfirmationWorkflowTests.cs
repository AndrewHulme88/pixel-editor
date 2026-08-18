using Avalonia.Headless.XUnit;
using pixel_editor.ViewModels;
using pixel_editor.Views;
using Xunit;

namespace PixelEditor.Ui.Tests;

public sealed class CloseConfirmationWorkflowTests
{
    [AvaloniaFact]
    public async Task ClosingDirtyWindow_WhenCancelled_KeepsEditorOpen()
    {
        var (window, viewModel) = ShowDirtyWindow();

        window.Close();
        var dialog = Assert.Single(window.OwnedWindows.OfType<UnsavedChangesDialog>());
        UiTestInteraction.ClickButton(dialog, "Cancel");
        await Task.Yield();

        Assert.True(window.IsVisible);

        viewModel.MarkDocumentSaved("test.png");
        window.Close();
    }

    [AvaloniaFact]
    public async Task ClosingDirtyWindow_WhenDiscarded_ClosesEditor()
    {
        var (window, _) = ShowDirtyWindow();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.SetResult();

        window.Close();
        var dialog = Assert.Single(window.OwnedWindows.OfType<UnsavedChangesDialog>());
        UiTestInteraction.ClickButton(dialog, "Don't Save");
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(window.IsVisible);
    }

    private static (MainWindow Window, MainViewModel ViewModel) ShowDirtyWindow()
    {
        var viewModel = new MainViewModel();
        viewModel.CreateNewDocument(16, 16);
        var window = new MainWindow
        {
            DataContext = viewModel
        };
        window.Show();
        return (window, viewModel);
    }
}
