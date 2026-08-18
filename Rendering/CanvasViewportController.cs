using System;
using Avalonia;

namespace pixel_editor.Rendering;

internal sealed class CanvasViewportController
{
    private double? _pixelScale;
    private Vector _panOffset;
    private Point _lastPanPosition;

    public bool IsPanning { get; private set; }

    public double? PixelScale => _pixelScale;

    public CanvasLayoutResult CalculateLayout(
        int documentWidth,
        int documentHeight,
        Size availableSize) =>
        _pixelScale is { } pixelScale
            ? CanvasLayout.Calculate(
                documentWidth,
                documentHeight,
                availableSize,
                pixelScale,
                _panOffset)
            : CanvasLayout.Calculate(documentWidth, documentHeight, availableSize);

    public void ZoomAt(
        int documentWidth,
        int documentHeight,
        Size availableSize,
        Point anchor,
        bool zoomIn)
    {
        var viewport = CanvasViewport.ZoomAt(
            CalculateLayout(documentWidth, documentHeight, availableSize),
            documentWidth,
            documentHeight,
            availableSize,
            anchor,
            zoomIn);

        _pixelScale = viewport.PixelScale;
        _panOffset = viewport.PanOffset;
    }

    public void BeginPan(Point pointerPosition, CanvasLayoutResult currentLayout)
    {
        _pixelScale ??= currentLayout.PixelScale;
        _lastPanPosition = pointerPosition;
        IsPanning = true;
    }

    public void PanTo(Point pointerPosition)
    {
        if (!IsPanning)
        {
            throw new InvalidOperationException("Panning has not started.");
        }

        _panOffset += pointerPosition - _lastPanPosition;
        _lastPanPosition = pointerPosition;
    }

    public void EndPan() => IsPanning = false;

    public void Reset()
    {
        _pixelScale = null;
        _panOffset = default;
        IsPanning = false;
    }
}
