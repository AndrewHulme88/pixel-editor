using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BrushToolBenchmarks
{
    private static readonly PixelColor FirstColor = new(49, 130, 206);
    private static readonly PixelColor SecondColor = new(230, 96, 72);

    private PixelDocument _document = null!;
    private bool _useFirstColor;

    [Params(16, 64, 256)]
    public int StrokeLength { get; set; }

    [Params(1, 4, 16, 64)]
    public int BrushSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = new PixelDocument(StrokeLength, StrokeLength);
        _document.PixelChanged += OnPixelChanged;
    }

    [Benchmark]
    public void DrawHorizontalStroke()
    {
        BrushTool.DrawLine(
            _document,
            0,
            StrokeLength / 2,
            StrokeLength - 1,
            StrokeLength / 2,
            NextColor(),
            BrushSize);
    }

    [Benchmark]
    public void DrawDiagonalStroke()
    {
        BrushTool.DrawLine(
            _document,
            0,
            0,
            StrokeLength - 1,
            StrokeLength - 1,
            NextColor(),
            BrushSize);
    }

    private PixelColor NextColor()
    {
        _useFirstColor = !_useFirstColor;
        return _useFirstColor ? FirstColor : SecondColor;
    }

    private static void OnPixelChanged(object? sender, PixelChangedEventArgs change)
    {
    }
}
