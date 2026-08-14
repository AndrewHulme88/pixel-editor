# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush and eraser tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: undo and redo

The canvas supports one-pixel brush and eraser tools using the primary mouse button. Each click or drag is recorded as one reversible edit, with repeated changes to the same pixel compacted into a single history entry. History stores only changed pixels rather than full document snapshots. On Windows, use `Ctrl+Z` to undo and `Ctrl+Y` or `Ctrl+Shift+Z` to redo. On macOS, use `Cmd+Z` to undo and `Cmd+Shift+Z` or `Cmd+Y` to redo. The brush uses a temporary fixed blue colour until the colour picker is added.

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

Performance benchmarks cover continuous brush strokes, undo history, and full pixel-buffer conversion. Run them in Release mode:

```sh
dotnet run -c Release --project benchmarks/PixelEditor.Benchmarks -- --filter "*"
```

Benchmark results are machine-specific, so comparisons should use the same hardware and power profile. Unit tests remain separate and deterministic.
