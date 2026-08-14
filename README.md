# Pixel Editor

A desktop pixel art editor built with C# and Avalonia.

## MVP scope

- Pixel canvas
- Brush and eraser tools
- Colour picker
- Undo and redo
- Basic editor layout
- Save and load

## Current milestone: document foundation

The user can represent an image as a fixed size grid of RGBA pixels. New documents are transparent, and individual pixels can be read and changed. This logic lives in a UI-independent library so it can be tested and extended without depending on Avalonia.

## Project structure

- `pixel-editor.csproj` — Avalonia desktop application
- `src/PixelEditor.Core` — UI-independent editor and document logic
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

Performance benchmarks will be added when brush strokes and canvas rendering provide meaningful operations to measure. Benchmarks will be kept separate from unit tests so test results remain deterministic.
