using Avalonia;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using pixel_editor.Rendering;

namespace PixelEditor.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CheckerboardRenderBenchmarks
{
    private CanvasLayoutResult _layout;

    [Params(1024, 4096)]
    public int VisiblePixelsPerAxis { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _layout = new CanvasLayoutResult(
            new Rect(0, 0, VisiblePixelsPerAxis, VisiblePixelsPerAxis),
            1);
    }

    [Benchmark]
    public double EnumerateDarkPixelBounds()
    {
        var checksum = 0d;

        for (var row = 0; row < VisiblePixelsPerAxis; row++)
        {
            for (var column = 0; column < VisiblePixelsPerAxis; column++)
            {
                if ((row + column) % 2 == 0)
                {
                    continue;
                }

                var bounds = CanvasPixelGrid.GetPixelBounds(_layout, column, row);
                checksum += bounds.X + bounds.Y;
            }
        }

        return checksum;
    }
}
