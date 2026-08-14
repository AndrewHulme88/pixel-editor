# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush and eraser tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: read-only canvas rendering

The application displays a `PixelDocument` on a centred canvas with an integer pixel scale. Transparent areas use a checkerboard background and bitmap interpolation is disabled to keep pixel edges sharp. A temporary sample image makes the rendering behaviour visible until drawing tools are added.

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
