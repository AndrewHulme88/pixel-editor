using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using pixel_editor.Views;
using Xunit;

namespace PixelEditor.Ui.Tests;

public sealed class DialogWorkflowTests
{
    [AvaloniaFact]
    public async Task NewDocumentDialog_CreateReturnsEnteredDimensions()
    {
        var owner = ShowOwner();
        var dialog = new NewDocumentDialog();
        var resultTask = dialog.ShowDialog<NewDocumentSize?>(owner);
        dialog.FindControl<NumericUpDown>("WidthInput")!.Value = 32;
        dialog.FindControl<NumericUpDown>("HeightInput")!.Value = 24;

        UiTestInteraction.ClickButton(dialog, "Create");

        var result = await resultTask;
        Assert.Equal(new NewDocumentSize(32, 24), result);
        owner.Close();
    }

    [AvaloniaTheory]
    [InlineData("Save", (int)UnsavedChangesChoice.Save)]
    [InlineData("Don't Save", (int)UnsavedChangesChoice.DontSave)]
    [InlineData("Cancel", (int)UnsavedChangesChoice.Cancel)]
    public async Task UnsavedChangesDialog_ButtonsReturnExpectedChoice(
        string buttonText,
        int expectedChoice)
    {
        var owner = ShowOwner();
        var dialog = new UnsavedChangesDialog("art.png");
        var resultTask = dialog.ShowDialog<UnsavedChangesChoice>(owner);

        UiTestInteraction.ClickButton(dialog, buttonText);

        Assert.Equal((UnsavedChangesChoice)expectedChoice, await resultTask);
        owner.Close();
    }

    private static Window ShowOwner()
    {
        var owner = new Window();
        owner.Show();
        return owner;
    }
}
