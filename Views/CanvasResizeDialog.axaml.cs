using Avalonia.Controls;
using Avalonia.Interactivity;
using PixelEditor.Core.Documents;

namespace pixel_editor.Views;

internal readonly record struct CanvasResizeSettings(
    int Width,
    int Height,
    CanvasAnchor Anchor);

public partial class CanvasResizeDialog : Window
{
    private static readonly CanvasAnchor[] Anchors =
    [
        CanvasAnchor.TopLeft,
        CanvasAnchor.Top,
        CanvasAnchor.TopRight,
        CanvasAnchor.Left,
        CanvasAnchor.Center,
        CanvasAnchor.Right,
        CanvasAnchor.BottomLeft,
        CanvasAnchor.Bottom,
        CanvasAnchor.BottomRight
    ];

    public CanvasResizeDialog()
    {
        InitializeComponent();
        WidthInput.Minimum = PixelDocumentLimits.MinimumDimension;
        WidthInput.Maximum = PixelDocumentLimits.MaximumDimension;
        HeightInput.Minimum = PixelDocumentLimits.MinimumDimension;
        HeightInput.Maximum = PixelDocumentLimits.MaximumDimension;
        RangeText.Text =
            $"{PixelDocumentLimits.MinimumDimension}–{PixelDocumentLimits.MaximumDimension} pixels";
    }

    internal CanvasResizeDialog(int width, int height)
        : this()
    {
        WidthInput.Value = width;
        HeightInput.Value = height;
    }

    private void Resize_OnClick(object? sender, RoutedEventArgs e)
    {
        var anchorIndex = AnchorInput.SelectedIndex;

        if (WidthInput.Value is not { } width ||
            HeightInput.Value is not { } height ||
            anchorIndex < 0 ||
            anchorIndex >= Anchors.Length)
        {
            return;
        }

        Close(new CanvasResizeSettings(
            decimal.ToInt32(width),
            decimal.ToInt32(height),
            Anchors[anchorIndex]));
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e) =>
        Close((CanvasResizeSettings?)null);
}
