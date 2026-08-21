using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using pixel_editor.Rendering;
using PixelEditor.Core.Documents;
using PixelEditor.Core.Selections;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PixelSelectionBenchmarks
{
    private PixelSelection _selection = null!;
    private PixelSelection _selectionWithInsetHole = null!;
    private PixelSelectionBounds _fullBounds;
    private PixelSelectionBounds _insetBounds;
    private bool _useInsetBounds;

    [Params(256, 1024, PixelDocumentLimits.MaximumDimension)]
    public int CanvasSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _selection = new PixelSelection(CanvasSize, CanvasSize);
        _fullBounds = new PixelSelectionBounds(0, 0, CanvasSize, CanvasSize);
        var inset = CanvasSize / 4;
        _insetBounds = new PixelSelectionBounds(
            inset,
            inset,
            CanvasSize - (inset * 2),
            CanvasSize - (inset * 2));
        _selection.ReplaceRectangle(_fullBounds);
        _selectionWithInsetHole = new PixelSelection(CanvasSize, CanvasSize);
        _selectionWithInsetHole.ReplaceRectangle(_fullBounds);
        _selectionWithInsetHole.SubtractRectangle(_insetBounds);
    }

    [Benchmark]
    public int ReplaceAlternatingRectangles()
    {
        _useInsetBounds = !_useInsetBounds;
        _selection.ReplaceRectangle(_useInsetBounds ? _insetBounds : _fullBounds);
        return _selection.SelectedPixelCount;
    }

    [Benchmark]
    public int SubtractAndRestoreInsetRectangle()
    {
        _selection.SubtractRectangle(_insetBounds);
        _selection.AddRectangle(_insetBounds);
        return _selection.SelectedPixelCount;
    }

    [Benchmark]
    public int BuildFullSelectionOutline() =>
        SelectionOutlineBuilder.Create(_selection).Count;

    [Benchmark]
    public int BuildInsetHoleOutline() =>
        SelectionOutlineBuilder.Create(_selectionWithInsetHole).Count;
}
