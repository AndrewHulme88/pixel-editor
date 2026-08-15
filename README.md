# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush and eraser tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: PNG saving and loading

The editor has a tested PNG persistence foundation built on SkiaSharp. Documents can be encoded to and decoded from lossless PNG streams while preserving exact RGBA values, including partial transparency. File-dialog integration is the next part of this milestone.

## Project structure

- `pixel-editor.csproj` — Avalonia desktop application
- `benchmarks/PixelEditor.Benchmarks` — repeatable performance benchmarks
- `src/PixelEditor.Core` — UI-independent editor and document logic
- `tests/PixelEditor.App.Tests` — unit tests for rendering and application logic
- `tests/PixelEditor.Core.Tests` — unit tests for the core library

## Requirements

- .NET 10 SDK

## Build and test

```sh
dotnet build pixel-editor.slnx
dotnet test pixel-editor.slnx
```

Run the desktop application with:

```sh
dotnet run --project pixel-editor.csproj
```

## Performance testing

Performance benchmarks cover continuous brush strokes, undo history, pixel-buffer conversion, and PNG persistence. Run them in Release mode:

```sh
dotnet run -c Release --project benchmarks/PixelEditor.Benchmarks -- --filter "*"
```

Benchmark results are machine-specific, so comparisons should use the same hardware and power profile. Unit tests remain separate and deterministic.
