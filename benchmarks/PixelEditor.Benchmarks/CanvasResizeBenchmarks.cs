using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelEditor.Core.Documents;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CanvasResizeBenchmarks
{
    private PixelDocument _document = null!;

    [Params(64, 256, 1024)]
    public int CanvasSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = new PixelDocument(CanvasSize, CanvasSize);

        for (var y = 0; y < CanvasSize; y++)
        {
            for (var x = 0; x < CanvasSize; x++)
            {
                _document.SetPixel(
                    x,
                    y,
                    new PixelColor((byte)x, (byte)y, (byte)(x + y), 255));
            }
        }
    }

    [Benchmark]
    public PixelDocument GrowFromCenter() => PixelDocumentResizer.Resize(
        _document,
        CanvasSize + (CanvasSize / 2),
        CanvasSize + (CanvasSize / 2),
        CanvasAnchor.Center);

    [Benchmark]
    public PixelDocument ShrinkFromCenter() => PixelDocumentResizer.Resize(
        _document,
        CanvasSize / 2,
        CanvasSize / 2,
        CanvasAnchor.Center);
}
