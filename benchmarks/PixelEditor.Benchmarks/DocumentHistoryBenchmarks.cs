using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;
using PixelEditor.Core.Tools;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DocumentHistoryBenchmarks
{
    private static readonly PixelColor Color = new(49, 130, 206);

    private PixelDocument _document = null!;
    private DocumentHistory _history = null!;

    [Params(16, 64, 256)]
    public int StrokeLength { get; set; }

    [Params(1, 4, 16, 64)]
    public int BrushSize { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _document = new PixelDocument(StrokeLength, BrushSize);
        _history = new DocumentHistory();
    }

    [Benchmark]
    public void RecordAndUndoStroke()
    {
        _history.BeginChangeSet(_document);
        BrushTool.DrawLine(
            _document,
            0,
            BrushSize / 2,
            StrokeLength - 1,
            BrushSize / 2,
            Color,
            BrushSize);
        _history.CommitChangeSet();
        _history.Undo();
    }
}
