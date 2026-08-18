using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using pixel_editor.Input;
using pixel_editor.Rendering;
using pixel_editor.Tools;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
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

    public static readonly StyledProperty<string> ZoomTextProperty =
        AvaloniaProperty.Register<PixelCanvas, string>(nameof(ZoomText), "Fit");

    public static readonly StyledProperty<PixelColor> BrushColorProperty =
        AvaloniaProperty.Register<PixelCanvas, PixelColor>(
            nameof(BrushColor),
            new PixelColor(49, 130, 206));

    public static readonly StyledProperty<int> BrushSizeProperty =
        AvaloniaProperty.Register<PixelCanvas, int>(
            nameof(BrushSize),
            BrushTool.MinimumSize);

    public static readonly StyledProperty<EditorTool> ActiveToolProperty =
        AvaloniaProperty.Register<PixelCanvas, EditorTool>(
            nameof(ActiveTool),
            EditorTool.Brush);

    public static readonly StyledProperty<DocumentHistory?> HistoryProperty =
        AvaloniaProperty.Register<PixelCanvas, DocumentHistory?>(nameof(History));

    private static readonly IPen CanvasBorder = new Pen(new SolidColorBrush(Color.FromRgb(96, 96, 96)));
    private static readonly IBrush HoverFill = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255));
    private static readonly IPen HoverOutline = new Pen(Brushes.White, 1);

    private WriteableBitmap? _bitmap;
    private readonly CheckerboardBrushCache _checkerboardBrushCache = new();
    private PixelDocument? _subscribedDocument;
    private PixelCoordinate? _hoveredPixel;
    private PixelCoordinate? _lastPaintedPixel;
    private PixelCoordinate? _straightLineStart;
    private PixelCoordinate? _straightLineEnd;
    private DocumentHistory? _activeHistory;
    private BitmapUpdateBatch? _bitmapUpdateBatch;
    private double? _pixelScale;
    private Vector _panOffset;
    private Point _lastPanPosition;
    private bool _isDrawing;
    private bool _isPanning;
    private BrushStrokeMode _strokeMode;

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

    public string ZoomText
    {
        get => GetValue(ZoomTextProperty);
        private set => SetValue(ZoomTextProperty, value);
    }

    public PixelColor BrushColor
    {
        get => GetValue(BrushColorProperty);
        set => SetValue(BrushColorProperty, value);
    }

    public int BrushSize
    {
        get => GetValue(BrushSizeProperty);
        set => SetValue(BrushSizeProperty, value);
    }

    public EditorTool ActiveTool
    {
        get => GetValue(ActiveToolProperty);
        set => SetValue(ActiveToolProperty, value);
    }

    public DocumentHistory? History
    {
        get => GetValue(HistoryProperty);
        set => SetValue(HistoryProperty, value);
    }

    public void ZoomIn() => ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), true);

    public void ZoomOut() => ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), false);

    public void ResetView()
    {
        ResetViewport();
        SetHoveredPixel(null);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Document is null || _bitmap is null)
        {
            return;
        }

        var layout = GetCanvasLayout();
        DrawTransparencyBackground(context, layout);

        var source = new Rect(0, 0, Document.Width, Document.Height);
        context.DrawImage(_bitmap, source, layout.Destination);
        context.DrawRectangle(null, CanvasBorder, layout.Destination);
        DrawStraightLineGuide(context, layout);
        DrawHoveredPixel(context, layout);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointerPosition = e.GetPosition(this);

        if (_isPanning)
        {
            if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                EndPan(e.Pointer);
                return;
            }

            var delta = pointerPosition - _lastPanPosition;
            _panOffset += delta;
            _lastPanPosition = pointerPosition;
            UpdateHoveredPixel(pointerPosition);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        UpdateHoveredPixel(pointerPosition);

        if (!_isDrawing)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (_strokeMode == BrushStrokeMode.StraightLine)
            {
                UpdateStraightLineEnd(pointerPosition);
                PaintStraightLine();
            }

            EndBrushStroke(e.Pointer);
            return;
        }

        if (_strokeMode == BrushStrokeMode.StraightLine)
        {
            UpdateStraightLineEnd(pointerPosition);
            return;
        }

        PaintTo(pointerPosition);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pointerPoint = e.GetCurrentPoint(this);

        if (Document is not null && pointerPoint.Properties.IsMiddleButtonPressed)
        {
            BeginPan(e.Pointer, e.GetPosition(this));
            e.Handled = true;
            return;
        }

        if (Document is not { } document ||
            !pointerPoint.Properties.IsLeftButtonPressed ||
            !TryGetPixelCoordinate(e.GetPosition(this), out var coordinate))
        {
            return;
        }

        if (ActiveTool == EditorTool.Fill)
        {
            FillAt(document, coordinate);
            e.Handled = true;
            return;
        }

        _activeHistory = History;
        _activeHistory?.BeginChangeSet(document);
        _isDrawing = true;
        _lastPaintedPixel = null;
        _strokeMode = BrushStrokeModeResolver.Resolve(e.KeyModifiers);
        e.Pointer.Capture(this);

        if (_strokeMode == BrushStrokeMode.StraightLine)
        {
            _straightLineStart = coordinate;
            _straightLineEnd = coordinate;
            SetHoveredPixel(coordinate);
            InvalidateVisual();
        }
        else
        {
            PaintTo(e.GetPosition(this));
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                EndPan(e.Pointer);
                UpdateHoveredPixel(e.GetPosition(this));
            }

            e.Handled = true;
            return;
        }

        if (!_isDrawing)
        {
            return;
        }

        if (_strokeMode == BrushStrokeMode.StraightLine)
        {
            UpdateStraightLineEnd(e.GetPosition(this));
            PaintStraightLine();
        }
        else
        {
            PaintTo(e.GetPosition(this));
        }

        EndBrushStroke(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        CompleteStroke();
        _isPanning = false;
        base.OnPointerCaptureLost(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (Document is null || _isDrawing || _isPanning || e.Delta.Y == 0)
        {
            return;
        }

        ZoomAt(e.GetPosition(this), e.Delta.Y > 0);
        e.Handled = true;
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
            CompleteStroke();
            ResetViewport();
            SubscribeToDocument(Document);
            SetHoveredPixel(null);
            RebuildBitmap();
        }
        else if (change.Property == BrushSizeProperty)
        {
            InvalidateVisual();
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
        CompleteStroke();
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

    private void DrawTransparencyBackground(
        DrawingContext context,
        CanvasLayoutResult layout)
    {
        var checkerboardLayout = CheckerboardRenderLayout.Calculate(layout);
        using var transform = context.PushTransform(checkerboardLayout.DocumentToScreen);
        context.FillRectangle(
            _checkerboardBrushCache.GetBrush(),
            checkerboardLayout.DocumentBounds);
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

        var layout = GetCanvasLayout();
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

        var start = _lastPaintedPixel ?? coordinate;
        PaintBrushLine(
            Document,
            start,
            coordinate,
            color,
            BrushSize);

        _lastPaintedPixel = coordinate;
        SetHoveredPixel(coordinate);
    }

    private void UpdateStraightLineEnd(Point pointerPosition)
    {
        if (!TryGetPixelCoordinate(pointerPosition, out var coordinate))
        {
            return;
        }

        _straightLineEnd = coordinate;
        SetHoveredPixel(coordinate);
    }

    private void PaintStraightLine()
    {
        if (Document is not { } document ||
            _straightLineStart is not { } start ||
            _straightLineEnd is not { } end)
        {
            return;
        }

        PaintBrushLine(
            document,
            start,
            end,
            ToolColorResolver.Resolve(ActiveTool, BrushColor),
            BrushSize);

        SetHoveredPixel(end);
    }

    private void PaintBrushLine(
        PixelDocument document,
        PixelCoordinate start,
        PixelCoordinate end,
        PixelColor color,
        int brushSize)
    {
        if (_bitmap is null)
        {
            BrushTool.DrawLine(
                document,
                start.X,
                start.Y,
                end.X,
                end.Y,
                color,
                brushSize);
            return;
        }

        using (var framebuffer = _bitmap.Lock())
        {
            _bitmapUpdateBatch = new BitmapUpdateBatch(framebuffer.Address, framebuffer.RowBytes);

            try
            {
                BrushTool.DrawLine(
                    document,
                    start.X,
                    start.Y,
                    end.X,
                    end.Y,
                    color,
                    brushSize);
            }
            finally
            {
                _bitmapUpdateBatch = null;
            }
        }

        InvalidateVisual();
    }

    private void FillAt(PixelDocument document, PixelCoordinate coordinate)
    {
        var color = ToolColorResolver.Resolve(ActiveTool, BrushColor);

        if (document.GetPixel(coordinate.X, coordinate.Y) == color)
        {
            SetHoveredPixel(coordinate);
            return;
        }

        var result = FillTool.Fill(document, coordinate.X, coordinate.Y, color);
        History?.RecordSpanChange(
            document,
            result.Spans,
            result.PreviousColor,
            result.Color);

        SetHoveredPixel(coordinate);
    }

    private void EndBrushStroke(IPointer pointer)
    {
        CompleteStroke();
        pointer.Capture(null);
    }

    private void BeginPan(IPointer pointer, Point pointerPosition)
    {
        CompleteStroke();

        if (_pixelScale is null)
        {
            var pixelScale = GetCanvasLayout().PixelScale;
            _pixelScale = pixelScale;
            ZoomText = $"{pixelScale * 100:0}%";
        }

        _isPanning = true;
        _lastPanPosition = pointerPosition;
        pointer.Capture(this);
    }

    private void EndPan(IPointer pointer)
    {
        _isPanning = false;
        pointer.Capture(null);
    }

    private void CompleteStroke()
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        _lastPaintedPixel = null;
        _straightLineStart = null;
        _straightLineEnd = null;
        _strokeMode = BrushStrokeMode.Freehand;
        _activeHistory?.CommitChangeSet();
        _activeHistory = null;
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
            CanvasPixelGrid.GetBrushBounds(
                layout,
                pixel.X,
                pixel.Y,
                ActiveTool == EditorTool.Fill ? BrushTool.MinimumSize : BrushSize,
                Document!.Width,
                Document.Height));
    }

    private void DrawStraightLineGuide(DrawingContext context, CanvasLayoutResult layout)
    {
        if (_strokeMode != BrushStrokeMode.StraightLine ||
            _straightLineStart is not { } start ||
            _straightLineEnd is not { } end)
        {
            return;
        }

        var startBounds = CanvasPixelGrid.GetPixelBounds(layout, start.X, start.Y);
        var endBounds = CanvasPixelGrid.GetPixelBounds(layout, end.X, end.Y);

        context.DrawLine(HoverOutline, startBounds.Center, endBounds.Center);
        context.DrawRectangle(
            HoverFill,
            HoverOutline,
            CanvasPixelGrid.GetBrushBounds(
                layout,
                start.X,
                start.Y,
                BrushSize,
                Document!.Width,
                Document.Height));
    }

    private CanvasLayoutResult GetCanvasLayout()
    {
        var document = Document ?? throw new InvalidOperationException("A document is required for canvas layout.");

        return _pixelScale is { } pixelScale
            ? CanvasLayout.Calculate(
                document.Width,
                document.Height,
                Bounds.Size,
                pixelScale,
                _panOffset)
            : CanvasLayout.Calculate(document.Width, document.Height, Bounds.Size);
    }

    private void ZoomAt(Point anchor, bool zoomIn)
    {
        if (Document is not { } document)
        {
            return;
        }

        var viewport = CanvasViewport.ZoomAt(
            GetCanvasLayout(),
            document.Width,
            document.Height,
            Bounds.Size,
            anchor,
            zoomIn);

        _pixelScale = viewport.PixelScale;
        _panOffset = viewport.PanOffset;
        ZoomText = $"{viewport.PixelScale * 100:0}%";
        UpdateHoveredPixel(anchor);
        InvalidateVisual();
    }

    private void ResetViewport()
    {
        _pixelScale = null;
        _panOffset = default;
        _isPanning = false;
        ZoomText = "Fit";
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
            _subscribedDocument.PixelSpansChanged -= OnPixelSpansChanged;
        }

        _subscribedDocument = document;

        if (_subscribedDocument is not null)
        {
            _subscribedDocument.PixelChanged += OnPixelChanged;
            _subscribedDocument.PixelSpansChanged += OnPixelSpansChanged;
        }
    }

    private void OnPixelChanged(object? sender, PixelChangedEventArgs e)
    {
        if (_bitmap is null || !ReferenceEquals(sender, Document))
        {
            return;
        }

        if (_bitmapUpdateBatch is { } batch)
        {
            WriteBitmapPixel(batch.Address, batch.RowBytes, e);
            return;
        }

        using var framebuffer = _bitmap.Lock();
        WriteBitmapPixel(framebuffer.Address, framebuffer.RowBytes, e);
        InvalidateVisual();
    }

    private void OnPixelSpansChanged(object? sender, PixelSpansChangedEventArgs e)
    {
        if (_bitmap is null ||
            !ReferenceEquals(sender, Document) ||
            e.Spans.Count == 0)
        {
            return;
        }

        var maximumLength = 0;

        foreach (var span in e.Spans)
        {
            maximumLength = Math.Max(maximumLength, span.Length);
        }

        var maximumByteCount = checked(maximumLength * PixelBufferBuilder.BytesPerPixel);
        var spanBuffer = ArrayPool<byte>.Shared.Rent(maximumByteCount);

        try
        {
            FillPixelBuffer(spanBuffer.AsSpan(0, maximumByteCount), e.Color);

            using var framebuffer = _bitmap.Lock();

            foreach (var span in e.Spans)
            {
                var destination = IntPtr.Add(
                    framebuffer.Address,
                    (span.Y * framebuffer.RowBytes) +
                    (span.X * PixelBufferBuilder.BytesPerPixel));
                var byteCount = span.Length * PixelBufferBuilder.BytesPerPixel;
                Marshal.Copy(spanBuffer, 0, destination, byteCount);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(spanBuffer);
        }

        InvalidateVisual();
    }

    private static void FillPixelBuffer(Span<byte> buffer, PixelColor color)
    {
        Span<byte> pixel = stackalloc byte[PixelBufferBuilder.BytesPerPixel];
        PixelBufferBuilder.WritePremultipliedBgra(color, pixel);

        for (var offset = 0; offset < buffer.Length; offset += pixel.Length)
        {
            pixel.CopyTo(buffer[offset..]);
        }
    }

    private static void WriteBitmapPixel(
        IntPtr framebufferAddress,
        int rowBytes,
        PixelChangedEventArgs change)
    {
        var destination = IntPtr.Add(
            framebufferAddress,
            (change.Y * rowBytes) + (change.X * PixelBufferBuilder.BytesPerPixel));

        Span<byte> pixel = stackalloc byte[PixelBufferBuilder.BytesPerPixel];
        PixelBufferBuilder.WritePremultipliedBgra(change.Color, pixel);

        for (var index = 0; index < pixel.Length; index++)
        {
            Marshal.WriteByte(destination, index, pixel[index]);
        }
    }

    private void DisposeBitmap()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }

    private readonly record struct BitmapUpdateBatch(IntPtr Address, int RowBytes);
}
