using Avalonia.Input;
using pixel_editor.Input;
using PixelEditor.Core.Selections;
using Xunit;

namespace PixelEditor.App.Tests.Input;

public sealed class SelectionInputResolverTests
{
    [Theory]
    [InlineData(KeyModifiers.None, false, (int)SelectionCombineMode.Replace)]
    [InlineData(KeyModifiers.Control, false, (int)SelectionCombineMode.Replace)]
    [InlineData(KeyModifiers.Shift, false, (int)SelectionCombineMode.Add)]
    [InlineData(KeyModifiers.Shift | KeyModifiers.Control, false, (int)SelectionCombineMode.Add)]
    [InlineData(KeyModifiers.Alt, true, (int)SelectionCombineMode.Replace)]
    [InlineData(KeyModifiers.Alt | KeyModifiers.Control, true, (int)SelectionCombineMode.Replace)]
    [InlineData(KeyModifiers.Shift | KeyModifiers.Alt, false, (int)SelectionCombineMode.Subtract)]
    public void Resolve_ReturnsContextualSelectionAction(
        KeyModifiers modifiers,
        bool shouldSampleColor,
        int expectedCombineMode)
    {
        Assert.Equal(
            shouldSampleColor,
            SelectionInputResolver.ShouldSampleColor(modifiers));
        Assert.Equal(
            (SelectionCombineMode)expectedCombineMode,
            SelectionInputResolver.ResolveCombineMode(modifiers));
    }
}
