using System;
using System.Buffers;
using System.Collections.Generic;
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
using PixelEditor.Core.Selections;
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

    public static readonly StyledProperty<ShapeDrawMode> ShapeModeProperty =
        AvaloniaProperty.Register<PixelCanvas, ShapeDrawMode>(
            nameof(ShapeMode),
            ShapeDrawMode.Outline);

    public static readonly StyledProperty<DocumentHistory?> HistoryProperty =
        AvaloniaProperty.Register<PixelCanvas, DocumentHistory?>(nameof(History));

    public static readonly StyledProperty<PixelSelection?> SelectionProperty =
        AvaloniaProperty.Register<PixelCanvas, PixelSelection?>(nameof(Selection));

    private static readonly IPen CanvasBorder = new Pen(new SolidColorBrush(Color.FromRgb(96, 96, 96)));
    private static readonly IBrush HoverFill = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255));
    private static readonly IBrush SelectionAddFill = new SolidColorBrush(Color.FromArgb(48, 80, 220, 120));
    private static readonly IBrush SelectionSubtractFill = new SolidColorBrush(Color.FromArgb(48, 235, 90, 90));
    private static readonly IPen HoverOutline = new Pen(Brushes.White, 1);
    private static readonly IPen SelectionBorder = new Pen(Brushes.Black, 1);
    private static readonly IPen SelectionDashes = new Pen(Brushes.White, 1, DashStyle.Dash);

    private WriteableBitmap? _bitmap;
    private readonly CheckerboardBrushCache _checkerboardBrushCache = new();
    private readonly CanvasViewportController _viewport = new();
    private readonly ShapeGesture _shapeGesture = new();
    private readonly SelectionGesture _selectionGesture = new();
    private PixelDocument? _subscribedDocument;
    private PixelSelection? _subscribedSelection;
    private IReadOnlyList<SelectionOutlineSegment> _selectionOutline = [];
    private IPointer? _selectionPointer;
    private PixelCoordinate? _hoveredPixel;
    private PixelCoordinate? _lastPaintedPixel;
    private PixelCoordinate? _straightLineStart;
    private PixelCoordinate? _straightLineEnd;
    private DocumentHistory? _activeHistory;
    private BitmapUpdateBatch? _bitmapUpdateBatch;
    private bool _isDrawing;
    private BrushStrokeMode _strokeMode;

    public PixelCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
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

    public ShapeDrawMode ShapeMode
    {
        get => GetValue(ShapeModeProperty);
        set => SetValue(ShapeModeProperty, value);
    }

    public DocumentHistory? History
    {
        get => GetValue(HistoryProperty);
        set => SetValue(HistoryProperty, value);
    }

    public PixelSelection? Selection
    {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    public void ZoomIn() => ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), true);

    public void ZoomOut() => ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), false);

    public void ResetView()
    {
        _viewport.Reset();
        UpdateZoomText();
        SetHoveredPixel(null);
        InvalidateVisual();
    }

    public bool CancelOrClearSelection()
    {
        if (_selectionGesture.IsActive)
        {
            var pointer = _selectionPointer;
            CompleteStroke();
            pointer?.Capture(null);
            return true;
        }

        return Selection?.Clear() == true;
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
        DrawSelectionMarquee(context, layout);
        DrawSelectionGuide(context, layout);
        DrawShapeGuide(context, layout);
        DrawStraightLineGuide(context, layout);
        DrawHoveredPixel(context, layout);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointerPosition = e.GetPosition(this);

        if (_viewport.IsPanning)
        {
            if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                EndPan(e.Pointer);
                return;
            }

            _viewport.PanTo(pointerPosition);
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
            if (_selectionGesture.IsActive)
            {
                UpdateSelectionEnd(pointerPosition);
                CommitSelection();
            }
            else if (_shapeGesture.IsActive)
            {
                UpdateShapeEnd(pointerPosition);
                PaintShape();
            }
            else if (_strokeMode == BrushStrokeMode.StraightLine)
            {
                UpdateStraightLineEnd(pointerPosition);
                PaintStraightLine();
            }

            EndBrushStroke(e.Pointer);
            return;
        }

        if (_selectionGesture.IsActive)
        {
            UpdateSelectionEnd(pointerPosition);
            return;
        }

        if (_shapeGesture.IsActive)
        {
            UpdateShapeEnd(pointerPosition);
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
            Focus(NavigationMethod.Pointer);
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

        Focus(NavigationMethod.Pointer);

        if (ActiveTool == EditorTool.Selection)
        {
            if (SelectionInputResolver.ShouldSampleColor(e.KeyModifiers))
            {
                SampleColorAt(document, coordinate);
            }
            else
            {
                BeginSelection(
                    e.Pointer,
                    coordinate,
                    SelectionInputResolver.ResolveCombineMode(e.KeyModifiers));
            }

            e.Handled = true;
            return;
        }

        if (ActiveTool == EditorTool.Eyedropper ||
            (e.KeyModifiers & KeyModifiers.Alt) != 0)
        {
            SampleColorAt(document, coordinate);
            e.Handled = true;
            return;
        }

        if (ActiveTool == EditorTool.Fill)
        {
            FillAt(document, coordinate);
            e.Handled = true;
            return;
        }

        if (ActiveTool is EditorTool.Rectangle or EditorTool.Ellipse)
        {
            BeginShape(e.Pointer, coordinate);
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

        if (_viewport.IsPanning)
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

        if (_selectionGesture.IsActive)
        {
            UpdateSelectionEnd(e.GetPosition(this));
            CommitSelection();
        }
        else if (_shapeGesture.IsActive)
        {
            UpdateShapeEnd(e.GetPosition(this));
            PaintShape();
        }
        else if (_strokeMode == BrushStrokeMode.StraightLine)
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
        _viewport.EndPan();
        base.OnPointerCaptureLost(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (Document is null || _isDrawing || _viewport.IsPanning || e.Delta.Y == 0)
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled &&
            e.Key == Key.Escape &&
            e.KeyModifiers == KeyModifiers.None &&
            CancelOrClearSelection())
        {
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DocumentProperty)
        {
            CompleteStroke();
            _viewport.Reset();
            UpdateZoomText();
            SubscribeToDocument(Document);
            SetHoveredPixel(null);
            RebuildBitmap();
        }
        else if (change.Property == SelectionProperty)
        {
            CompleteStroke();
            SubscribeToSelection(Selection);
            InvalidateVisual();
        }
        else if (change.Property == BrushSizeProperty ||
                 change.Property == ActiveToolProperty ||
                 change.Property == ShapeModeProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeToDocument(Document);
        SubscribeToSelection(Selection);

        if (_bitmap is null && Document is not null)
        {
            RebuildBitmap();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CompleteStroke();
        SubscribeToDocument(null);
        SubscribeToSelection(null);
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

    private void BeginShape(IPointer pointer, PixelCoordinate coordinate)
    {
        _isDrawing = true;
        _shapeGesture.Begin(ActiveTool, ShapeMode, coordinate);
        SetHoveredPixel(coordinate);
        pointer.Capture(this);
        InvalidateVisual();
    }

    private void BeginSelection(
        IPointer pointer,
        PixelCoordinate coordinate,
        SelectionCombineMode combineMode)
    {
        _isDrawing = true;
        _selectionGesture.Begin(coordinate, combineMode);
        _selectionPointer = pointer;
        SetHoveredPixel(coordinate);
        pointer.Capture(this);
        InvalidateVisual();
    }

    private void UpdateSelectionEnd(Point pointerPosition)
    {
        if (Document is not { } document)
        {
            return;
        }

        var coordinate = CanvasCoordinateMapper.MapClamped(
            pointerPosition,
            GetCanvasLayout(),
            document.Width,
            document.Height);
        _selectionGesture.Update(coordinate);
        SetHoveredPixel(coordinate);
        InvalidateVisual();
    }

    private void CommitSelection()
    {
        if (Selection is not { } selection ||
            _selectionGesture.Current is not { } gesture)
        {
            return;
        }

        var bounds = PixelSelectionBounds.FromInclusiveCorners(
            gesture.Start.X,
            gesture.Start.Y,
            gesture.End.X,
            gesture.End.Y,
            selection.Width,
            selection.Height);
        selection.ApplyRectangle(bounds, gesture.CombineMode);
    }

    private void UpdateShapeEnd(Point pointerPosition)
    {
        if (!TryGetPixelCoordinate(pointerPosition, out var coordinate))
        {
            return;
        }

        _shapeGesture.Update(coordinate);
        SetHoveredPixel(coordinate);
        InvalidateVisual();
    }

    private void PaintShape()
    {
        if (Document is not { } document ||
            _shapeGesture.Current is not { } shape)
        {
            return;
        }

        if (shape.Mode == ShapeDrawMode.Filled)
        {
            PaintFilledShape(document, shape);
            return;
        }

        _activeHistory = History;
        _activeHistory?.BeginChangeSet(document);

        if (_bitmap is null)
        {
            DrawOutlineShape(document, shape);
            return;
        }

        using (var framebuffer = _bitmap.Lock())
        {
            _bitmapUpdateBatch = new BitmapUpdateBatch(framebuffer.Address, framebuffer.RowBytes);

            try
            {
                DrawOutlineShape(document, shape);
            }
            finally
            {
                _bitmapUpdateBatch = null;
            }
        }

        InvalidateVisual();
    }

    private void DrawOutlineShape(PixelDocument document, ShapeGestureState shape)
    {
        var color = ToolColorResolver.Resolve(shape.Tool, BrushColor);

        if (shape.Tool == EditorTool.Rectangle)
        {
            OutlineShapeTool.DrawRectangle(
                document,
                shape.Start.X,
                shape.Start.Y,
                shape.End.X,
                shape.End.Y,
                color,
                BrushSize);
        }
        else
        {
            OutlineShapeTool.DrawEllipse(
                document,
                shape.Start.X,
                shape.Start.Y,
                shape.End.X,
                shape.End.Y,
                color,
                BrushSize);
        }
    }

    private void PaintFilledShape(
        PixelDocument document,
        ShapeGestureState shape)
    {
        var spans = shape.Tool == EditorTool.Rectangle
            ? FilledShapeTool.CreateRectangleSpans(
                document,
                shape.Start.X,
                shape.Start.Y,
                shape.End.X,
                shape.End.Y)
            : FilledShapeTool.CreateEllipseSpans(
                document,
                shape.Start.X,
                shape.Start.Y,
                shape.End.X,
                shape.End.Y);
        var color = ToolColorResolver.Resolve(shape.Tool, BrushColor);

        if (History is { } history)
        {
            history.ApplyAndRecordUniformPatch(document, spans, color);
        }
        else
        {
            document.SetPixelSpans(spans, color);
        }
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

    private void SampleColorAt(PixelDocument document, PixelCoordinate coordinate)
    {
        SetCurrentValue(
            BrushColorProperty,
            document.GetPixel(coordinate.X, coordinate.Y));
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

        _viewport.BeginPan(pointerPosition, GetCanvasLayout());
        UpdateZoomText();
        pointer.Capture(this);
    }

    private void EndPan(IPointer pointer)
    {
        _viewport.EndPan();
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
        _shapeGesture.Cancel();
        _selectionGesture.Cancel();
        _selectionPointer = null;
        _strokeMode = BrushStrokeMode.Freehand;
        _activeHistory?.CommitChangeSet();
        _activeHistory = null;
        InvalidateVisual();
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
                ActiveTool is EditorTool.Fill or EditorTool.Eyedropper or EditorTool.Selection ||
                (ActiveTool is EditorTool.Rectangle or EditorTool.Ellipse &&
                 ShapeMode == ShapeDrawMode.Filled)
                    ? BrushTool.MinimumSize
                    : BrushSize,
                Document!.Width,
                Document.Height));
    }

    private void DrawSelectionMarquee(
        DrawingContext context,
        CanvasLayoutResult layout)
    {
        if (_selectionGesture.Current is { CombineMode: SelectionCombineMode.Replace })
        {
            return;
        }

        foreach (var outlineSegment in _selectionOutline)
        {
            var segment = SelectionRenderLayout.Calculate(outlineSegment, layout);
            context.DrawLine(SelectionBorder, segment.Start, segment.End);
            context.DrawLine(SelectionDashes, segment.Start, segment.End);
        }
    }

    private void DrawSelectionGuide(
        DrawingContext context,
        CanvasLayoutResult layout)
    {
        if (Document is not { } document ||
            _selectionGesture.Current is not { } gesture)
        {
            return;
        }

        var bounds = PixelSelectionBounds.FromInclusiveCorners(
            gesture.Start.X,
            gesture.Start.Y,
            gesture.End.X,
            gesture.End.Y,
            document.Width,
            document.Height);

        DrawSelectionBounds(
            context,
            SelectionRenderLayout.Calculate(bounds, layout),
            gesture.CombineMode switch
            {
                SelectionCombineMode.Add => SelectionAddFill,
                SelectionCombineMode.Subtract => SelectionSubtractFill,
                _ => HoverFill
            });
    }

    private static void DrawSelectionBounds(
        DrawingContext context,
        Rect bounds,
        IBrush? fill)
    {
        context.DrawRectangle(fill, SelectionBorder, bounds);
        context.DrawRectangle(null, SelectionDashes, bounds);
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

    private void DrawShapeGuide(DrawingContext context, CanvasLayoutResult layout)
    {
        if (_shapeGesture.Current is not { } shape)
        {
            return;
        }

        if (shape.Start == shape.End)
        {
            var clickPreviewSize = shape.Mode == ShapeDrawMode.Filled
                ? BrushTool.MinimumSize
                : BrushSize;
            context.DrawRectangle(
                HoverFill,
                HoverOutline,
                CanvasPixelGrid.GetBrushBounds(
                    layout,
                    shape.Start.X,
                    shape.Start.Y,
                    clickPreviewSize,
                    Document!.Width,
                    Document.Height));
            return;
        }

        var start = CanvasPixelGrid.GetPixelBounds(
            layout,
            shape.Start.X,
            shape.Start.Y).Center;
        var end = CanvasPixelGrid.GetPixelBounds(
            layout,
            shape.End.X,
            shape.End.Y).Center;
        var bounds = new Rect(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));
        var previewSize = shape.Mode == ShapeDrawMode.Filled
            ? BrushTool.MinimumSize
            : BrushSize;
        var pen = new Pen(Brushes.White, Math.Max(1, previewSize * layout.PixelScale));
        var fill = shape.Mode == ShapeDrawMode.Filled ? HoverFill : null;

        if (bounds.Width == 0 || bounds.Height == 0)
        {
            context.DrawLine(pen, start, end);
            return;
        }

        if (shape.Tool == EditorTool.Rectangle)
        {
            context.DrawRectangle(fill, pen, bounds);
        }
        else
        {
            context.DrawEllipse(fill, pen, bounds);
        }
    }

    private CanvasLayoutResult GetCanvasLayout()
    {
        var document = Document ?? throw new InvalidOperationException("A document is required for canvas layout.");

        return _viewport.CalculateLayout(
            document.Width,
            document.Height,
            Bounds.Size);
    }

    private void ZoomAt(Point anchor, bool zoomIn)
    {
        if (Document is not { } document)
        {
            return;
        }

        _viewport.ZoomAt(
            document.Width,
            document.Height,
            Bounds.Size,
            anchor,
            zoomIn);

        UpdateZoomText();
        UpdateHoveredPixel(anchor);
        InvalidateVisual();
    }

    private void UpdateZoomText() =>
        ZoomText = _viewport.PixelScale is { } pixelScale
            ? $"{pixelScale * 100:0}%"
            : "Fit";

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
            _subscribedDocument.PixelPatchChanged -= OnPixelPatchChanged;
        }

        _subscribedDocument = document;

        if (_subscribedDocument is not null)
        {
            _subscribedDocument.PixelChanged += OnPixelChanged;
            _subscribedDocument.PixelSpansChanged += OnPixelSpansChanged;
            _subscribedDocument.PixelPatchChanged += OnPixelPatchChanged;
        }
    }

    private void SubscribeToSelection(PixelSelection? selection)
    {
        if (ReferenceEquals(_subscribedSelection, selection))
        {
            return;
        }

        if (_subscribedSelection is not null)
        {
            _subscribedSelection.Changed -= OnSelectionChanged;
        }

        _subscribedSelection = selection;

        if (_subscribedSelection is not null)
        {
            _subscribedSelection.Changed += OnSelectionChanged;
        }

        RebuildSelectionOutline();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, Selection))
        {
            RebuildSelectionOutline();
            InvalidateVisual();
        }
    }

    private void RebuildSelectionOutline() =>
        _selectionOutline = Selection is { } selection
            ? SelectionOutlineBuilder.Create(selection)
            : [];

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

    private void OnPixelPatchChanged(object? sender, PixelPatchChangedEventArgs e)
    {
        if (_bitmap is null ||
            Document is not { } document ||
            !ReferenceEquals(sender, document) ||
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
            using var framebuffer = _bitmap.Lock();

            foreach (var span in e.Spans)
            {
                var byteCount = span.Length * PixelBufferBuilder.BytesPerPixel;
                var pixels = spanBuffer.AsSpan(0, byteCount);

                for (var offset = 0; offset < span.Length; offset++)
                {
                    PixelBufferBuilder.WritePremultipliedBgra(
                        document.GetPixel(span.X + offset, span.Y),
                        pixels.Slice(
                            offset * PixelBufferBuilder.BytesPerPixel,
                            PixelBufferBuilder.BytesPerPixel));
                }

                var destination = IntPtr.Add(
                    framebuffer.Address,
                    (span.Y * framebuffer.RowBytes) +
                    (span.X * PixelBufferBuilder.BytesPerPixel));
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
