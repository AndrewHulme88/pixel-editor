using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using pixel_editor.Controls;
using pixel_editor.Rendering;
using pixel_editor.ViewModels;
using pixel_editor.Views;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Selections;
using PixelEditor.Core.Tools;
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
    public void EyedropperShortcutAndClick_UpdateSelectedColorWithoutEditing()
    {
        var viewModel = new MainViewModel();
        var sampledColor = new PixelColor(15, 80, 145, 96);
        var pixel = new PixelCoordinate(8, 8);
        viewModel.Document.SetPixel(pixel.X, pixel.Y, sampledColor);
        var historyStateId = viewModel.History.CurrentStateId;
        var window = CreateWindow(viewModel);
        var canvas = window.FindControl<PixelCanvas>("EditorCanvas")!;
        var point = GetWindowPixelCentre(window, canvas, viewModel.Document, pixel);

        PressAndRelease(window, PhysicalKey.I, RawInputModifiers.None);

        Assert.Equal(EditorTool.Eyedropper, viewModel.ActiveTool);

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(sampledColor, viewModel.BrushColor);
        Assert.Equal(sampledColor, viewModel.Document.GetPixel(pixel.X, pixel.Y));
        Assert.Equal(historyStateId, viewModel.History.CurrentStateId);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.CanUndo);
        window.Close();
    }

    [AvaloniaFact]
    public void SelectionShortcutAndEscape_SelectAndClearWithoutEditing()
    {
        var viewModel = new MainViewModel();
        var historyStateId = viewModel.History.CurrentStateId;
        var window = CreateWindow(viewModel);
        var canvas = window.FindControl<PixelCanvas>("EditorCanvas")!;
        var selector = window.FindControl<ComboBox>("BrushSizeSelector")!;
        var pixel = new PixelCoordinate(8, 8);
        var point = GetWindowPixelCentre(window, canvas, viewModel.Document, pixel);

        PressAndRelease(window, PhysicalKey.M, RawInputModifiers.None);

        Assert.Equal(EditorTool.Selection, viewModel.ActiveTool);

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(
            new PixelSelectionBounds(pixel.X, pixel.Y, 1, 1),
            viewModel.Selection.Bounds);
        Assert.Equal(historyStateId, viewModel.History.CurrentStateId);
        Assert.False(viewModel.IsDirty);

        selector.Focus();
        PressAndRelease(window, PhysicalKey.Escape, RawInputModifiers.None);

        Assert.False(viewModel.Selection.HasSelection);
        Assert.Equal(historyStateId, viewModel.History.CurrentStateId);
        Assert.False(viewModel.IsDirty);
        window.Close();
    }

    [AvaloniaFact]
    public void DrawingAfterBrushSizeSelection_RestoresDocumentUndoShortcut()
    {
        var viewModel = new MainViewModel();
        var window = CreateWindow(viewModel);
        var selector = window.FindControl<ComboBox>("BrushSizeSelector")!;
        var canvas = window.FindControl<PixelCanvas>("EditorCanvas")!;
        selector.SelectedItem = 4;
        selector.Focus();
        var pixel = new PixelCoordinate(8, 8);
        var layout = CanvasLayout.Calculate(
            viewModel.Document.Width,
            viewModel.Document.Height,
            canvas.Bounds.Size);
        var localPoint = CanvasPixelGrid.GetPixelBounds(layout, pixel.X, pixel.Y).Center;
        var windowPoint = canvas.TranslatePoint(localPoint, window)
            ?? throw new InvalidOperationException("Canvas position could not be mapped to the window.");
        var commandModifier = OperatingSystem.IsMacOS()
            ? RawInputModifiers.Meta
            : RawInputModifiers.Control;

        window.MouseDown(windowPoint, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(windowPoint, MouseButton.Left, RawInputModifiers.None);

        Assert.True(canvas.IsFocused);
        Assert.NotEqual(PixelColor.Transparent, viewModel.Document.GetPixel(pixel.X, pixel.Y));

        PressAndRelease(window, PhysicalKey.Z, commandModifier);

        Assert.Equal(PixelColor.Transparent, viewModel.Document.GetPixel(pixel.X, pixel.Y));
        viewModel.MarkDocumentSaved("test.png");
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

    private static Point GetWindowPixelCentre(
        MainWindow window,
        PixelCanvas canvas,
        PixelDocument document,
        PixelCoordinate pixel)
    {
        var layout = CanvasLayout.Calculate(
            document.Width,
            document.Height,
            canvas.Bounds.Size);
        var localPoint = CanvasPixelGrid.GetPixelBounds(layout, pixel.X, pixel.Y).Center;
        return canvas.TranslatePoint(localPoint, window)
            ?? throw new InvalidOperationException("Canvas position could not be mapped to the window.");
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
