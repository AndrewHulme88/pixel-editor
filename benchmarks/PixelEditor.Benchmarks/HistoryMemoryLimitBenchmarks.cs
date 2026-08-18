using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;
using PixelEditor.Core.History;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class HistoryMemoryLimitBenchmarks
{
    private static readonly PixelColor Color = new(49, 130, 206);

    private PixelDocument _document = null!;
    private DocumentHistory _history = null!;

    [Params(256, 4096)]
    public int EditCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _document = new PixelDocument(EditCount, 1);
        _history = new DocumentHistory(16 * 1024);
    }

    [Benchmark]
    public long RecordAndTrimPixelEdits()
    {
        for (var x = 0; x < EditCount; x++)
        {
            _history.BeginChangeSet(_document);
            _document.SetPixel(x, 0, Color);
            _history.CommitChangeSet();
        }

        return _history.EstimatedMemoryUsageBytes;
    }
}
