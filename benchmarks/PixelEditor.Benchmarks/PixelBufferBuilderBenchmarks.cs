using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using pixel_editor.Rendering;
using PixelEditor.Core.Documents;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PixelBufferBuilderBenchmarks
{
    private PixelDocument _document = null!;

    [Params(16, 64, 256, 512)]
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
                    new PixelColor((byte)x, (byte)y, (byte)(x + y), 192));
            }
        }
    }

    [Benchmark]
    public byte[] BuildBitmapBuffer()
        => PixelBufferBuilder.CreatePremultipliedBgra(_document);
}
