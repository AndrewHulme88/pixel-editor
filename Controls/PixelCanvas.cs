using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using pixel_editor.Rendering;
using pixel_editor.Tools;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;

namespace pixel_editor.Controls;

public sealed class PixelCanvas : Control
{
    public static readonly StyledProperty<PixelDocument?> DocumentProperty =
        AvaloniaProperty.Register<PixelCanvas, PixelDocument?>(nameof(Document));

    public static readonly StyledProperty<string> HoveredPixelTextProperty =
        AvaloniaProperty.Register<PixelCanvas, string>(
            nameof(HoveredPixelText),
            string.Empty);

    public static readonly StyledProperty<PixelColor> BrushColorProperty =
        AvaloniaProperty.Register<PixelCanvas, PixelColor>(
            nameof(BrushColor),
            new PixelColor(49, 130, 206));

    public static readonly StyledProperty<EditorTool> ActiveToolProperty =
        AvaloniaProperty.Register<PixelCanvas, EditorTool>(
            nameof(ActiveTool),
            EditorTool.Brush);

    private static readonly IBrush CheckerLight = new SolidColorBrush(Color.FromRgb(214, 214, 214));
    private static readonly IBrush CheckerDark = new SolidColorBrush(Color.FromRgb(174, 174, 174));
    private static readonly IPen CanvasBorder = new Pen(new SolidColorBrush(Color.FromRgb(96, 96, 96)));
    private static readonly IBrush HoverFill = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255));
    private static readonly IPen HoverOutline = new Pen(Brushes.White, 1);

    private WriteableBitmap? _bitmap;
    private PixelDocument? _subscribedDocument;
    private PixelCoordinate? _hoveredPixel;
    private PixelCoordinate? _lastPaintedPixel;
    private bool _isDrawing;

    public PixelCanvas()
    {
        ClipToBounds = true;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    public PixelDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public string HoveredPixelText
    {
        get => GetValue(HoveredPixelTextProperty);
        private set => SetValue(HoveredPixelTextProperty, value);
    }

    public PixelColor BrushColor
    {
        get => GetValue(BrushColorProperty);
        set => SetValue(BrushColorProperty, value);
    }

    public EditorTool ActiveTool
    {
        get => GetValue(ActiveToolProperty);
        set => SetValue(ActiveToolProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Document is null || _bitmap is null)
        {
            return;
        }

        var layout = CanvasLayout.Calculate(Document.Width, Document.Height, Bounds.Size);
        DrawTransparencyBackground(context, layout, new Rect(Bounds.Size));

        var source = new Rect(0, 0, Document.Width, Document.Height);
        context.DrawImage(_bitmap, source, layout.Destination);
        context.DrawRectangle(null, CanvasBorder, layout.Destination);
        DrawHoveredPixel(context, layout);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointerPosition = e.GetPosition(this);
        UpdateHoveredPixel(pointerPosition);

        if (!_isDrawing)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            EndBrushStroke(e.Pointer);
            return;
        }

        PaintTo(pointerPosition);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            !TryGetPixelCoordinate(e.GetPosition(this), out _))
        {
            return;
        }

        _isDrawing = true;
        _lastPaintedPixel = null;
        e.Pointer.Capture(this);
        PaintTo(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isDrawing)
        {
            return;
        }

        PaintTo(e.GetPosition(this));
        EndBrushStroke(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _isDrawing = false;
        _lastPaintedPixel = null;
        base.OnPointerCaptureLost(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetHoveredPixel(null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DocumentProperty)
        {
            SubscribeToDocument(Document);
            SetHoveredPixel(null);
            RebuildBitmap();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeToDocument(Document);

        if (_bitmap is null && Document is not null)
        {
            RebuildBitmap();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SubscribeToDocument(null);
        DisposeBitmap();
        base.OnDetachedFromVisualTree(e);
    }

    private void RebuildBitmap()
    {
        DisposeBitmap();

        if (Document is null)
        {
            InvalidateVisual();
            return;
        }

        var buffer = PixelBufferBuilder.CreatePremultipliedBgra(Document);
        var bitmap = new WriteableBitmap(
            new PixelSize(Document.Width, Document.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var framebuffer = bitmap.Lock())
        {
            var sourceRowBytes = Document.Width * PixelBufferBuilder.BytesPerPixel;

            for (var y = 0; y < Document.Height; y++)
            {
                var sourceOffset = y * sourceRowBytes;
                var destination = IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes);
                Marshal.Copy(buffer, sourceOffset, destination, sourceRowBytes);
            }
        }

        _bitmap = bitmap;
        InvalidateVisual();
    }

    private static void DrawTransparencyBackground(
        DrawingContext context,
        CanvasLayoutResult layout,
        Rect canvasBounds)
    {
        var destination = layout.Destination;
        using var clip = context.PushClip(destination);
        context.FillRectangle(CheckerLight, destination);

        var visible = destination.Intersect(canvasBounds);
        var firstColumn = Math.Max(0, (int)Math.Floor((visible.X - destination.X) / layout.PixelScale));
        var firstRow = Math.Max(0, (int)Math.Floor((visible.Y - destination.Y) / layout.PixelScale));
        var lastColumn = (int)Math.Ceiling((visible.Right - destination.X) / layout.PixelScale);
        var lastRow = (int)Math.Ceiling((visible.Bottom - destination.Y) / layout.PixelScale);

        for (var row = firstRow; row < lastRow; row++)
        {
            for (var column = firstColumn; column < lastColumn; column++)
            {
                if ((row + column) % 2 == 0)
                {
                    continue;
                }

                context.FillRectangle(
                    CheckerDark,
                    CanvasPixelGrid.GetPixelBounds(layout, column, row));
            }
        }
    }

    private void UpdateHoveredPixel(Point pointerPosition)
    {
        SetHoveredPixel(TryGetPixelCoordinate(pointerPosition, out var coordinate)
            ? coordinate
            : null);
    }

    private bool TryGetPixelCoordinate(Point pointerPosition, out PixelCoordinate coordinate)
    {
        if (Document is null)
        {
            coordinate = default;
            return false;
        }

        var layout = CanvasLayout.Calculate(Document.Width, Document.Height, Bounds.Size);
        return CanvasCoordinateMapper.TryMap(
            pointerPosition,
            layout,
            Document.Width,
            Document.Height,
            out coordinate);
    }

    private void PaintTo(Point pointerPosition)
    {
        if (Document is null || !TryGetPixelCoordinate(pointerPosition, out var coordinate))
        {
            _lastPaintedPixel = null;
            return;
        }

        var color = ToolColorResolver.Resolve(ActiveTool, BrushColor);

        if (_lastPaintedPixel is { } previous)
        {
            BrushTool.DrawLine(
                Document,
                previous.X,
                previous.Y,
                coordinate.X,
                coordinate.Y,
                color);
        }
        else
        {
            Document.SetPixel(coordinate.X, coordinate.Y, color);
        }

        _lastPaintedPixel = coordinate;
        SetHoveredPixel(coordinate);
    }

    private void EndBrushStroke(IPointer pointer)
    {
        _isDrawing = false;
        _lastPaintedPixel = null;
        pointer.Capture(null);
    }

    private void SetHoveredPixel(PixelCoordinate? coordinate)
    {
        if (_hoveredPixel == coordinate)
        {
            return;
        }

        _hoveredPixel = coordinate;
        HoveredPixelText = coordinate is { } pixel
            ? $"{pixel.X}, {pixel.Y}"
            : string.Empty;

        InvalidateVisual();
    }

    private void DrawHoveredPixel(DrawingContext context, CanvasLayoutResult layout)
    {
        if (_hoveredPixel is not { } pixel)
        {
            return;
        }

        context.DrawRectangle(
            HoverFill,
            HoverOutline,
            CanvasPixelGrid.GetPixelBounds(layout, pixel.X, pixel.Y));
    }

    private void SubscribeToDocument(PixelDocument? document)
    {
        if (ReferenceEquals(_subscribedDocument, document))
        {
            return;
        }

        if (_subscribedDocument is not null)
        {
            _subscribedDocument.PixelChanged -= OnPixelChanged;
        }

        _subscribedDocument = document;

        if (_subscribedDocument is not null)
        {
            _subscribedDocument.PixelChanged += OnPixelChanged;
        }
    }

    private void OnPixelChanged(object? sender, PixelChangedEventArgs e)
    {
        if (_bitmap is null || !ReferenceEquals(sender, Document))
        {
            return;
        }

        using var framebuffer = _bitmap.Lock();
        var destination = IntPtr.Add(
            framebuffer.Address,
            (e.Y * framebuffer.RowBytes) + (e.X * PixelBufferBuilder.BytesPerPixel));

        Span<byte> pixel = stackalloc byte[PixelBufferBuilder.BytesPerPixel];
        PixelBufferBuilder.WritePremultipliedBgra(e.Color, pixel);

        for (var index = 0; index < pixel.Length; index++)
        {
            Marshal.WriteByte(destination, index, pixel[index]);
        }

        InvalidateVisual();
    }

    private void DisposeBitmap()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
