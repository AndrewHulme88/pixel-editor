using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class MaximumBrushStrokeBenchmarks
{
    private const int CanvasSize = 4096;

    private static readonly PixelColor FirstColor = new(255, 40, 80, 120);
    private static readonly PixelColor SecondColor = new(255, 120, 80, 40);

    private PixelDocument _document = null!;
    private bool _useFirstColor;

    [GlobalSetup]
    public void Setup()
    {
        _document = new PixelDocument(CanvasSize, CanvasSize);
        _document.PixelChanged += OnPixelChanged;
    }

    [Benchmark]
    public void DrawHorizontalStroke()
    {
        BrushTool.DrawLine(
            _document,
            0,
            CanvasSize / 2,
            CanvasSize - 1,
            CanvasSize / 2,
            NextColor(),
            BrushTool.MaximumSize);
    }

    [Benchmark]
    public void DrawDiagonalStroke()
    {
        BrushTool.DrawLine(
            _document,
            0,
            0,
            CanvasSize - 1,
            CanvasSize - 1,
            NextColor(),
            BrushTool.MaximumSize);
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
