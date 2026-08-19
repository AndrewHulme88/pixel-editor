using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class OutlineShapeToolBenchmarks
{
    private const int CanvasSize = PixelDocumentLimits.MaximumDimension;

    private static readonly PixelColor FirstColor = new(255, 50, 100, 180);
    private static readonly PixelColor SecondColor = new(255, 190, 80, 40);

    private PixelDocument _document = null!;
    private bool _useFirstColor;

    [Params(1, BrushTool.MaximumSize)]
    public int BrushSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = new PixelDocument(CanvasSize, CanvasSize);
        _document.PixelChanged += OnPixelChanged;
    }

    [Benchmark]
    public void DrawMaximumRectangle()
    {
        OutlineShapeTool.DrawRectangle(
            _document,
            0,
            0,
            CanvasSize - 1,
            CanvasSize - 1,
            NextColor(),
            BrushSize);
    }

    [Benchmark]
    public void DrawMaximumEllipse()
    {
        OutlineShapeTool.DrawEllipse(
            _document,
            0,
            0,
            CanvasSize - 1,
            CanvasSize - 1,
            NextColor(),
            BrushSize);
    }

    private PixelColor NextColor()
    {
        _useFirstColor = !_useFirstColor;
        return _useFirstColor ? FirstColor : SecondColor;
    }

    private static void OnPixelChanged(object? sender, PixelChangedEventArgs e)
    {
    }
}
