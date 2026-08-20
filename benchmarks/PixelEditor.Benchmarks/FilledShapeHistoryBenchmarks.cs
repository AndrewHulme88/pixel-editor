using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class FilledShapeHistoryBenchmarks
{
    private static readonly PixelColor FirstColor = new(50, 120, 210, 180);
    private static readonly PixelColor SecondColor = new(220, 80, 45, 130);

    private PixelDocument _rectangleDocument = null!;
    private PixelDocument _ellipseDocument = null!;
    private DocumentHistory _rectangleHistory = null!;
    private DocumentHistory _ellipseHistory = null!;
    private IReadOnlyList<PixelSpan> _rectangleSpans = null!;
    private IReadOnlyList<PixelSpan> _ellipseSpans = null!;
    private bool _rectangleUsesFirstColor;
    private bool _ellipseUsesFirstColor;

    [Params(256, 1024, PixelDocumentLimits.MaximumDimension)]
    public int CanvasSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rectangleDocument = new PixelDocument(CanvasSize, CanvasSize);
        _ellipseDocument = new PixelDocument(CanvasSize, CanvasSize);
        _rectangleHistory = new DocumentHistory();
        _ellipseHistory = new DocumentHistory();
        _rectangleSpans = FilledShapeTool.CreateRectangleSpans(
            _rectangleDocument,
            0,
            0,
            CanvasSize - 1,
            CanvasSize - 1);
        _ellipseSpans = FilledShapeTool.CreateEllipseSpans(
            _ellipseDocument,
            0,
            0,
            CanvasSize - 1,
            CanvasSize - 1);
        _rectangleDocument.PixelSpansChanged += OnPixelSpansChanged;
        _rectangleDocument.PixelPatchChanged += OnPixelPatchChanged;
        _ellipseDocument.PixelSpansChanged += OnPixelSpansChanged;
        _ellipseDocument.PixelPatchChanged += OnPixelPatchChanged;
    }

    [Benchmark]
    public bool RectangleApplyUndoRedo()
    {
        _rectangleUsesFirstColor = !_rectangleUsesFirstColor;
        return ApplyUndoRedo(
            _rectangleDocument,
            _rectangleHistory,
            _rectangleSpans,
            _rectangleUsesFirstColor ? FirstColor : SecondColor);
    }

    [Benchmark]
    public bool EllipseApplyUndoRedo()
    {
        _ellipseUsesFirstColor = !_ellipseUsesFirstColor;
        return ApplyUndoRedo(
            _ellipseDocument,
            _ellipseHistory,
            _ellipseSpans,
            _ellipseUsesFirstColor ? FirstColor : SecondColor);
    }

    private static bool ApplyUndoRedo(
        PixelDocument document,
        DocumentHistory history,
        IReadOnlyList<PixelSpan> spans,
        PixelColor color)
    {
        var wasRecorded = history.ApplyAndRecordUniformPatch(document, spans, color);
        var wasUndone = history.Undo();
        var wasRedone = history.Redo();
        history.Clear();
        return wasRecorded && wasUndone && wasRedone;
    }

    private static void OnPixelSpansChanged(object? sender, PixelSpansChangedEventArgs e)
    {
    }

    private static void OnPixelPatchChanged(object? sender, PixelPatchChangedEventArgs e)
    {
    }
}
