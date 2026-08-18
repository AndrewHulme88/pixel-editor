# Development decisions and engineering notes

This is the living technical record for Pixel Editor. Update it when a milestone introduces an important design choice, trade-off, blocker, performance finding, or change in direction.

## Engineering principles

- Prefer clear, maintainable C# over premature native optimisation.
- Keep document and tool logic independent from Avalonia wherever practical.
- Add tests with each feature, especially around document correctness and history.
- Measure performance with repeatable benchmarks before and after substantial optimisation.
- Optimise bulk operations as bulk operations rather than repeatedly invoking single-pixel infrastructure.
- Use short `//` comments only when they preserve intent or explain a non-obvious trade-off. Do not use XML summary comments for straightforward code.
- Keep the README focused and record detailed reasoning here.

## Technical decisions

### D001: C# and Avalonia remain the primary stack

C# provides fast iteration, memory-safe application code, mature tooling, and a good fit for desktop application architecture. Avalonia provides cross-platform desktop UI while keeping the project in one language. C++ would offer lower-level control but would increase development and maintenance cost. Rust remains a possible future option for isolated, measured native workloads, but it is not justified until profiling identifies work that cannot be addressed effectively in managed code.

This choice also supports using the same language across potential future tools. Shared document, command, persistence, and UI infrastructure can remain in .NET.

### D002: UI-independent logic belongs in PixelEditor.Core

`PixelEditor.Core` owns pixel documents, tools, resizing, and history. The Avalonia application owns windows, controls, pointer input, rendering, dialogs, and view models. This keeps core operations deterministic and easy to test without starting a UI framework.

Dependencies should generally point inward:

```text
Avalonia application -> PixelEditor.Core
Tests and benchmarks -> application or PixelEditor.Core
PixelEditor.Core      -> no Avalonia dependency
```

### D003: Pixels use exact RGBA values in a contiguous array

`PixelColor` is a four-byte value containing red, green, blue, and alpha channels. `PixelDocument` stores pixels in a one-dimensional row-major array. This keeps lookup predictable, reduces object allocation, and makes row copies and bulk fills efficient.

Tool comparisons use exact RGBA equality. For example, the fill bucket treats two pixels with matching RGB but different alpha as different colours.

### D004: Rendering uses an Avalonia WriteableBitmap

The document is displayed through a `WriteableBitmap` with nearest-neighbour interpolation. This preserves hard pixel edges at every zoom level. Each brush segment and each bulk span change updates the bitmap under one lock and invalidates the canvas once.

The transparency checkerboard is calculated from document pixel bounds so it remains aligned with the actual pixels at all scales.

### D005: View models use CommunityToolkit.Mvvm generation

Observable properties and commands use CommunityToolkit.Mvvm source generators to reduce repetitive notification and command code. Generated members such as `UndoCommand`, `RedoCommand`, and property implementations do not appear directly in the source file but are created during compilation.

Avalonia similarly generates `InitializeComponent()` from the associated AXAML file. An IDE may temporarily report generated members as missing if source generation or design-time builds have not completed; a successful project build is the authoritative check.

### D006: Undo history tracks state identity as well as edits

Normal brush strokes are recorded as change sets and committed as one undoable action. History state IDs allow the view model to recognise when undo returns to the last saved state, so the unsaved marker reflects document history rather than only whether an edit ever occurred.

Fill operations use compact uniform-colour spans rather than individual pixel records. Undo and redo apply those exact spans, which avoids accidentally including pixels that merely became connected to the fill colour later.

Resizing currently replaces the document and clears pixel history. Supporting undoable resize would require history entries capable of retaining document dimensions and cropped pixel data and is deferred beyond the current MVP.

### D007: Drawing tools share a common raster line operation

Brush and eraser strokes use an integer line rasteriser between pointer samples so fast pointer movement does not leave gaps. Brush size is represented by square stamps from 1×1 to 64×64 pixels and is clipped safely at document edges. The eraser reuses the brush path with transparent as its colour.

Shift-drag straight lines preview without changing the document and rasterise once on release. This keeps the preview reversible and commits the completed line as one history action.

### D008: Fill uses four-direction scanline flood fill

The fill bucket connects pixels through shared edges, not corners. It uses an iterative scanline algorithm rather than recursion, avoiding stack overflow on large documents and keeping the pending-seed collection relatively small.

The algorithm records horizontal spans as it discovers the region. A full 4096×4096 canvas is represented by about 4,096 spans rather than 16,777,216 individual pixel changes.

### D009: Keyboard shortcuts follow platform conventions

Windows uses Ctrl-based file and history shortcuts; macOS uses Cmd-based shortcuts. Redo supports the common alternatives on both platforms. Tool hotkeys are `B` for brush, `E` for eraser, and `G` for fill. `-` and `=` decrease and increase brush size.

### D010: View navigation uses discrete pixel-perfect zoom

Zoom levels are discrete and rendered with nearest-neighbour interpolation. Mouse-wheel zoom keeps the pixel beneath the pointer stable, middle-drag pans, and Fit returns to automatic centring and sizing. Pointer-to-document mapping always uses the current layout and viewport transform.

### D011: PNG is the first persistence format

SkiaSharp handles PNG encoding and decoding because it preserves RGBA data and fits naturally into the C# application. PNG provides interoperability for the MVP. A custom project format may be introduced later when features such as layers, animation, metadata, or non-destructive editing require information that PNG cannot store.

### D012: Testing and performance testing have different roles

xUnit tests verify deterministic behaviour and regressions. BenchmarkDotNet measures execution time and allocations under controlled runs; benchmark results are not correctness tests and should be compared on the same machine and power profile.

Benchmarks cover brush work, document history, canvas resizing, pixel-buffer creation, PNG persistence, core flood fill, and span-based fill history. The fill history benchmarks include fill recording, undo, and redo up to a 4096×4096 canvas.

### D013: Overlapping brush stamps are merged into row coverage

The visible brush remains a sequence of square stamps along the same integer raster line. Internally, overlapping stamps are merged into one covered interval per affected row before the document is changed. This preserves the existing raster output while avoiding repeated work on pixels covered by several adjacent stamps.

The canvas keeps one bitmap lock for the complete pointer segment and requests one redraw after that segment. Pixel notifications still occur for genuinely changed pixels so existing history behaviour remains unchanged.

## Performance findings and blockers

### Maximum-size brush and line drawing

Status: addressed with merged brush coverage and batched bitmap updates

Observed behaviour: drawing with a size-64 brush on a 4096×4096 canvas produced noticeable input lag.

Root causes:

- The rasteriser applied a complete 64×64 square at every coordinate along the line, repeatedly checking the large overlap between adjacent stamps.
- Every changed pixel independently locked the Avalonia bitmap and requested a canvas redraw.
- Existing brush benchmarks used documents up to 256×256 and did not isolate the maximum supported stroke.

Resolution:

- The rasteriser now collects the combined horizontal coverage for each affected row and changes every covered pixel once.
- Pointer segments and Shift-drag lines update the bitmap under one lock and invalidate the canvas once.
- An exhaustive small-document regression test compares the optimised output with the original square-stamp implementation across all endpoint pairs and brush sizes 1–5.
- A dedicated benchmark measures horizontal and diagonal size-64 strokes across the maximum canvas.

Before-and-after smoke measurement on 18 August 2026:

- Machine: Apple M4 running .NET 10.0.9.
- Benchmark: one BenchmarkDotNet Dry iteration on a 4096×4096 document with a size-64 brush.
- Horizontal stroke: approximately 171.7 ms before and 7.4 ms after, about 23 times faster.
- Diagonal stroke: approximately 176.9 ms before and 12.2 ms after, about 14 times faster.

These are cold-start smoke measurements, not statistically rigorous results, and they measure the core raster and document notification path rather than Avalonia presentation. Managed allocation remains approximately 8 MiB for the horizontal case and 15.75 MiB for the diagonal case because genuinely changed pixels still raise individual notifications. A normal Release benchmark run and manual input testing remain the authoritative checks for perceived drawing responsiveness.

### Maximum-canvas fill and undo

Status: addressed with span-based bulk edits

Observed behaviour: filling a uniform 4096×4096 canvas and undoing it each took several seconds.

Root causes:

- The canvas contains 16,777,216 pixels.
- The first implementation raised and recorded one event per changed pixel.
- History inserted every pixel into a dictionary and later copied those entries into an array. The final change array alone was roughly 256 MiB, before dictionary overhead and temporary allocations.
- Undo called `SetPixel` for every entry.
- Although the initial fill held one bitmap lock, undo locked and invalidated the bitmap once per pixel.
- The first fill benchmark measured the algorithm with a no-op notification but did not include history or canvas work, so it did not expose the complete application cost.

Resolution:

- Flood fill now changes and records horizontal pixel spans.
- Span mutation uses `Array.Fill` on contiguous document storage.
- Fill, undo, and redo each raise one bulk notification.
- The canvas updates all affected spans under one bitmap lock and redraws once.
- Dedicated history benchmarks measure fill recording, undo, and redo at representative sizes including the maximum canvas.

Post-change smoke measurement on 17 August 2026:

- Machine: Apple M4 running .NET 10.0.9.
- Benchmark: one BenchmarkDotNet Dry iteration of core fill, history recording, undo, and redo together.
- 256×256: approximately 5.3 ms and 12.5 KiB allocated.
- 1024×1024: approximately 9.3 ms and 48.5 KiB allocated.
- 4096×4096: approximately 73.5 ms and 192.5 KiB allocated.

These are cold-start smoke measurements rather than statistically rigorous benchmark results, and they exclude Avalonia bitmap presentation. Use a normal Release benchmark run on the same machine for reliable comparisons.

Remaining consideration: brush history still records individual changed pixels. This is appropriate for small strokes, but very large brushes or future shape and selection operations may benefit from the same span or tile-based history infrastructure.

### Large-canvas memory use

Status: monitor

A 4096×4096 `PixelDocument` requires about 64 MiB for RGBA pixels. Its displayed bitmap requires a similar amount, and persistence or conversion can temporarily allocate additional buffers. Undo data is additional. Features such as layers and animation will multiply this cost, so future work should establish memory budgets and consider pooled buffers, tiles, or compressed inactive history entries.

### Long operations currently run on the UI thread

Status: acceptable for MVP, revisit after measuring bulk edits

Document mutations and rendering notifications are synchronous. Bulk spans should make current fill operations much faster, but future effects, large image imports, or multi-layer operations may still require cancellable background computation followed by an atomic UI-thread commit. Threading should not be added until the remaining latency is measured because it introduces document ownership and cancellation complexity.

### Generated-code diagnostics

Status: tooling consideration

Source-generated commands, observable properties, and `InitializeComponent()` can appear missing in the editor when design-time generation is stale. Rebuilding the solution normally resolves this. These are not handwritten methods and should not be duplicated manually.

## Review findings backlog

Reviewed on 18 August 2026 after the first drawing, history, persistence, and editing milestones. No critical correctness defects were found, and the automated test and Release build baselines passed. The findings below should be handled individually so each change remains measurable and reviewable.

- **Maximum-size brush lag — addressed:** merged overlapping brush coverage and batched bitmap updates as recorded above.
- **PNG import limits — planned:** decoded image dimensions are not currently capped at the 4096×4096 editor limit. Document size rules should be centralised and shared by creation, resizing, and import before untrusted or very large files are supported.
- **Checkerboard rendering cost — planned:** the transparency background draws rectangles for visible document pixels. Large visible canvases can therefore add substantial render work; cache or tile the checkerboard after the brush fix is manually verified.
- **UI workflow coverage — planned:** core behaviour has good unit coverage, but pointer gestures, platform hotkeys, dialogs, and save/close workflows need headless Avalonia integration tests.
- **Initial sample document state — planned:** the populated startup sample is treated as clean and can close without a save prompt. Decide whether startup should use a blank document or mark sample content as unsaved.
- **File workflow hardening — planned:** async file commands have no re-entry guard, and saving writes directly to the target path. Add command gating and an atomic temporary-file replacement strategy before persistence becomes more complex.
- **History memory budget — monitor:** brush history stores individual pixel changes and has no memory cap. Establish a measurable budget before layers, animation, or larger operations multiply document and history memory.
- **UI class growth — monitor:** `PixelCanvas` and `MainWindow` code-behind are becoming coordination hotspots. Extract focused collaborators when the next features make their responsibilities harder to follow, rather than splitting them solely by line count.
- **Project cleanup — planned:** remove the unused `ViewLocator` scaffold or make it intentional, correct its existing formatting issue, and update the fill benchmark's obsolete single-pixel event subscription.

## Milestone record

1. Established the Avalonia application, core library, test projects, benchmark project, README, and `.gitignore`.
2. Added the pixel document and RGBA colour model.
3. Added canvas layout, coordinate mapping, aligned transparency checkers, and bitmap rendering.
4. Added brush and eraser tools with continuous rasterised strokes.
5. Added document history, undo/redo, dirty-state tracking, and platform-specific shortcuts.
6. Added colour selection and PNG loading/saving through SkiaSharp.
7. Added new-document and unsaved-change confirmation workflows.
8. Added pixel-perfect zoom, pointer-centred zooming, panning, and Fit.
9. Added anchored canvas resizing.
10. Added editable brush-size selection and keyboard adjustment.
11. Added Shift-drag straight-line drawing with a non-destructive guide.
12. Added a four-connected fill bucket with the `G` shortcut.
13. Reworked fill, history, and canvas updates around bulk pixel spans after maximum-canvas performance testing exposed per-pixel overhead.
14. Optimised maximum-size brush strokes by merging overlapping coverage, batching bitmap updates, and adding a maximum-canvas benchmark.

## Deferred or open decisions

- Layers, animation, selections, shapes, and clipboard operations are outside the current MVP.
- A custom editable project format should be designed only when PNG can no longer represent required project state.
- Undoable canvas resizing remains deferred.
- Native Rust components remain an option only for a measured hotspot with a stable, coarse API boundary.
- Layer and animation work will require explicit memory limits and likely a more advanced storage strategy.
- Benchmark results should be recorded when making performance-sensitive changes, especially before changing data representation again.
