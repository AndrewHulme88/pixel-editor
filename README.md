# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush and eraser tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: pixel brush

The canvas supports a one-pixel brush using the primary mouse button. Clicking paints one pixel, while dragging interpolates between pointer samples to produce continuous lines. Pixel change notifications update only the affected bitmap pixels. The brush uses a temporary fixed blue colour until the colour picker is added.

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

Performance benchmarks will be added after the initial brush workflow is stable. Benchmarks will be kept separate from unit tests so test results remain deterministic.
