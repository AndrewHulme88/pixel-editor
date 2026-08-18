using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using pixel_editor.ViewModels;
using pixel_editor.Views;
using PixelEditor.Core.Documents;
using Xunit;

namespace PixelEditor.Ui.Tests;

public sealed class KeyboardWorkflowTests
{
    [AvaloniaFact]
    public void PlatformUndoAndRedoShortcuts_UpdateDocument()
    {
        var viewModel = new MainViewModel();
        var color = new PixelColor(30, 90, 150);
        viewModel.History.BeginChangeSet(viewModel.Document);
        viewModel.Document.SetPixel(0, 0, color);
        viewModel.History.CommitChangeSet();
        var window = CreateWindow(viewModel);
        var commandModifier = OperatingSystem.IsMacOS()
            ? RawInputModifiers.Meta
            : RawInputModifiers.Control;

        PressAndRelease(window, PhysicalKey.Z, commandModifier);

        Assert.Equal(PixelColor.Transparent, viewModel.Document.GetPixel(0, 0));

        PressAndRelease(
            window,
            PhysicalKey.Z,
            commandModifier | RawInputModifiers.Shift);

        Assert.Equal(color, viewModel.Document.GetPixel(0, 0));

        viewModel.MarkDocumentSaved("test.png");
        window.Close();
    }

    [AvaloniaFact]
    public void BrushSizeShortcuts_UpdateBoundCanvasSetting()
    {
        var viewModel = new MainViewModel();
        var window = CreateWindow(viewModel);

        PressAndRelease(window, PhysicalKey.Equal, RawInputModifiers.None);

        Assert.Equal(2, viewModel.BrushSize);

        PressAndRelease(window, PhysicalKey.Minus, RawInputModifiers.None);

        Assert.Equal(1, viewModel.BrushSize);
        window.Close();
    }

    [AvaloniaFact]
    public async Task PlatformNewShortcut_CreatesDocumentThroughDialog()
    {
        var viewModel = new MainViewModel();
        var window = CreateWindow(viewModel);
        var commandModifier = OperatingSystem.IsMacOS()
            ? RawInputModifiers.Meta
            : RawInputModifiers.Control;

        PressAndRelease(window, PhysicalKey.N, commandModifier);
        var dialog = Assert.Single(window.OwnedWindows.OfType<NewDocumentDialog>());
        dialog.FindControl<NumericUpDown>("WidthInput")!.Value = 20;
        dialog.FindControl<NumericUpDown>("HeightInput")!.Value = 12;
        UiTestInteraction.ClickButton(dialog, "Create");
        await Task.Yield();

        Assert.Equal(20, viewModel.Document.Width);
        Assert.Equal(12, viewModel.Document.Height);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(
            "Created 20 × 12 document",
            window.FindControl<TextBlock>("FileStatusText")!.Text);

        viewModel.MarkDocumentSaved("test.png");
        window.Close();
    }

    [AvaloniaFact]
    public async Task DocumentWorkflow_RejectsReentryAndCloseUntilDialogCompletes()
    {
        var viewModel = new MainViewModel();
        var window = CreateWindow(viewModel);
        var commandModifier = OperatingSystem.IsMacOS()
            ? RawInputModifiers.Meta
            : RawInputModifiers.Control;

        PressAndRelease(window, PhysicalKey.N, commandModifier);
        var firstDialog = Assert.Single(window.OwnedWindows.OfType<NewDocumentDialog>());

        PressAndRelease(window, PhysicalKey.N, commandModifier);
        window.Close();

        Assert.True(window.IsVisible);
        Assert.Same(
            firstDialog,
            Assert.Single(window.OwnedWindows.OfType<NewDocumentDialog>()));

        UiTestInteraction.ClickButton(firstDialog, "Cancel");
        await Task.Yield();

        PressAndRelease(window, PhysicalKey.N, commandModifier);
        var secondDialog = Assert.Single(window.OwnedWindows.OfType<NewDocumentDialog>());
        Assert.NotSame(firstDialog, secondDialog);

        UiTestInteraction.ClickButton(secondDialog, "Cancel");
        await Task.Yield();
        window.Close();
    }

    private static MainWindow CreateWindow(MainViewModel viewModel)
    {
        var window = new MainWindow
        {
            DataContext = viewModel
        };
        window.Show();
        return window;
    }

    private static void PressAndRelease(
        MainWindow window,
        PhysicalKey key,
        RawInputModifiers modifiers)
    {
        window.KeyPressQwerty(key, modifiers);
        window.KeyReleaseQwerty(key, modifiers);
    }
}
