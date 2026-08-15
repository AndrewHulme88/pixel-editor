using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using pixel_editor.Persistence;
using PixelEditor.Core.Documents;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PngDocumentCodecBenchmarks
{
    private PixelDocument _document = null!;
    private byte[] _encodedPng = null!;

    [Params(16, 64, 256)]
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
                    new PixelColor((byte)x, (byte)y, (byte)(x + y), (byte)(x ^ y)));
            }
        }

        using var stream = new MemoryStream();
        PngDocumentCodec.Save(_document, stream);
        _encodedPng = stream.ToArray();
    }

    [Benchmark]
    public long SavePng()
    {
        using var stream = new MemoryStream();
        PngDocumentCodec.Save(_document, stream);
        return stream.Length;
    }

    [Benchmark]
    public PixelDocument LoadPng()
    {
        using var stream = new MemoryStream(_encodedPng, writable: false);
        return PngDocumentCodec.Load(stream);
    }
}
