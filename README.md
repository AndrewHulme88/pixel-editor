# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush and eraser tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: pointer coordinate mapping

The canvas translates pointer positions into exact document coordinates while accounting for centring, integer scaling, clipping, and canvas boundaries. The hovered pixel is highlighted and its coordinates appear in the status bar. The document remains read-only until drawing tools are added.

## Project structure

- `pixel-editor.csproj` — Avalonia desktop application
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

Performance benchmarks will be added with brush strokes, when editing creates meaningful rendering work to measure. Benchmarks will be kept separate from unit tests so test results remain deterministic.
