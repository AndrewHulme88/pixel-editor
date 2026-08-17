# Performance benchmarks

These benchmarks measure operations that sit on the editor's active drawing path:

- Continuous horizontal and diagonal brush strokes at representative brush sizes, including document change notifications
- Flood-filling solid regions at representative canvas sizes
- Recording, undoing, and redoing fills up to the maximum 4096×4096 canvas size
- Recording and undoing complete strokes at representative brush sizes
- Full conversion of a document into the premultiplied BGRA format used by Avalonia
- PNG encoding and decoding at representative canvas sizes
- Growing and shrinking canvases while preserving anchored pixels

Run the suite in Release mode:

```sh
dotnet run -c Release --project benchmarks/PixelEditor.Benchmarks -- --filter "*"
```

Run one benchmark group while iterating:

```sh
dotnet run -c Release --project benchmarks/PixelEditor.Benchmarks -- --filter "*BrushToolBenchmarks*"
```

Replace `BrushToolBenchmarks` with `FillToolBenchmarks` to focus on flood-fill performance.
Use `FillHistoryBenchmarks` to measure the complete fill history path, including the maximum canvas size.

Use BenchmarkDotNet's short job for a quicker local comparison:

```sh
dotnet run -c Release --project benchmarks/PixelEditor.Benchmarks -- --filter "*" --job short
```

BenchmarkDotNet writes detailed results to `BenchmarkDotNet.Artifacts`. Compare results on the same machine and power profile because absolute timings vary between environments.
