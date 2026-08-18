using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class FillToolBenchmarks
{
    private static readonly PixelColor FirstColor = new(49, 130, 206);
    private static readonly PixelColor SecondColor = new(230, 96, 72);

    private PixelDocument _document = null!;
    private bool _useFirstColor;

    [Params(64, 256, 1024)]
    public int CanvasSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = new PixelDocument(CanvasSize, CanvasSize);
        _document.PixelSpansChanged += OnPixelSpansChanged;
    }

    [Benchmark]
    public int FillCanvas() =>
        FillTool.Fill(_document, 0, 0, NextColor()).FilledPixelCount;

    private PixelColor NextColor()
    {
        _useFirstColor = !_useFirstColor;
        return _useFirstColor ? FirstColor : SecondColor;
    }

    private static void OnPixelSpansChanged(object? sender, PixelSpansChangedEventArgs change)
    {
    }
}
