# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush, eraser, and fill tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: fill bucket

Select the Fill tool or press `G`, then click a region to replace its contiguous colour with the selected colour. Fill uses edge-connected pixels, respects exact RGBA values, and commits the entire region as one undoable action.

Fill, undo, and redo store and apply horizontal pixel spans so large regions avoid per-pixel history and bitmap overhead.

Hold `Shift` before left-dragging to draw a straight line. A non-destructive guide follows the pointer, and releasing commits the line as one undoable action. Straight lines use the current brush or eraser, colour, and brush size.

The brush and eraser support square sizes from 1×1 to 64×64 pixels. Choose a common size from the editable toolbar list, type any supported size, press `-` to decrease it, or press `=` to increase it. Thick strokes remain continuous, clip safely at canvas edges, and show their full footprint in the hover indicator.

The Image menu can resize the canvas from 1×1 to 4096×4096 pixels using nine anchor positions. Growing adds transparent space, while shrinking crops pixels outside the new bounds. Resizing preserves the current filename, marks the document unsaved, clears pixel undo/redo history, and resets the viewport to Fit.

The canvas also supports discrete pixel-perfect zoom levels with nearest-neighbour rendering. Use the mouse wheel to zoom around the pointer, middle-drag to pan, or use the zoom controls in the status bar. The Fit control recentres the document and returns to automatic sizing.

The editor can create transparent canvases from 1×1 to 4096×4096 pixels. New documents are untitled and marked unsaved immediately, while replacing a document clears its undo and redo history and previous file association.

The editor can also open, save, and save as PNG through native file dialogs. PNG encoding and decoding use SkiaSharp and preserve exact RGBA values, including partial transparency.

Unsaved changes are shown with an asterisk in the window title. Opening another file or closing the editor offers to save, discard, or cancel; undoing back to the last saved state removes the unsaved marker.

Use `Ctrl+N`, `Ctrl+O`, `Ctrl+S`, and `Ctrl+Shift+S` for file operations on Windows. On macOS, use the equivalent `Cmd` shortcuts.

## Project structure

- `pixel-editor.csproj` — Avalonia desktop application
- `benchmarks/PixelEditor.Benchmarks` — repeatable performance benchmarks
- [`DEVELOPMENT.md`](DEVELOPMENT.md) — living record of technical decisions, trade-offs, and performance findings
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

Performance benchmarks cover multiple brush sizes, flood fill, undo history, canvas resizing, pixel-buffer conversion, and PNG persistence. Run them in Release mode:

```sh
dotnet run -c Release --project benchmarks/PixelEditor.Benchmarks -- --filter "*"
```

Benchmark results are machine-specific, so comparisons should use the same hardware and power profile. Unit tests remain separate and deterministic.
