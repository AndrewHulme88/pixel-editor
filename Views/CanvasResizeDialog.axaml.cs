using System;
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
    }

    internal CanvasResizeDialog(int width, int height)
        : this()
    {
        var maximum = Math.Max(4096, Math.Max(width, height));
        WidthInput.Maximum = maximum;
        HeightInput.Maximum = maximum;
        WidthInput.Value = width;
        HeightInput.Value = height;
        RangeText.Text = $"1–{maximum} pixels";
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
