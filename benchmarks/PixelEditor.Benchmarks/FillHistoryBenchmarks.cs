using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class FillHistoryBenchmarks
{
    private static readonly PixelColor FirstColor = new(49, 130, 206);
    private static readonly PixelColor SecondColor = new(230, 96, 72);

    private PixelDocument _document = null!;
    private DocumentHistory _history = null!;
    private bool _useFirstColor;

    [Params(256, 1024, 4096)]
    public int CanvasSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = new PixelDocument(CanvasSize, CanvasSize);
        _history = new DocumentHistory();
    }

    [Benchmark]
    public bool FillRecordUndoRedo()
    {
        var result = FillTool.Fill(_document, 0, 0, NextColor());
        var wasRecorded = _history.RecordSpanChange(
            _document,
            result.Spans,
            result.PreviousColor,
            result.Color);
        var wasUndone = _history.Undo();
        var wasRedone = _history.Redo();
        _history.Clear();
        return wasRecorded && wasUndone && wasRedone;
    }

    private PixelColor NextColor()
    {
        _useFirstColor = !_useFirstColor;
        return _useFirstColor ? FirstColor : SecondColor;
    }
}
