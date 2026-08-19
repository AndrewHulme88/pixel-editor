# Pixel Editor development plan

This file tracks upcoming development. Completed technical decisions, trade-offs, blockers, and performance findings belong in [`DEVELOPMENT.md`](DEVELOPMENT.md).

## Current status

The initial MVP and stability pass are complete. Phase 2 is underway, beginning with an eyedropper that samples exact canvas colours without editing the document.

The next phase will improve everyday editing while retaining the current single-canvas document model.

## Phase 2: Editing essentials

Work through these milestones individually. Each milestone should include focused automated tests, relevant performance coverage, manual testing, and a development-record update when it introduces an important decision or trade-off.

### 1. Eyedropper tool

- [x] Add an eyedropper that samples exact RGBA values from the canvas.
- [x] Assign `I` as its keyboard shortcut.
- [x] Update the selected colour without changing document pixels, history, or dirty state.
- [x] Test opaque, partially transparent, and fully transparent pixels.
- [x] Add a headless pointer and shortcut workflow test.
- [x] Temporarily sample with `Alt` on Windows/Linux or `Option` on macOS without changing the selected tool.

### 2. Rectangle and ellipse tools

- [ ] Add outline rectangle and ellipse tools.
- [ ] Use the selected colour and brush size.
- [ ] Show a non-destructive guide while dragging.
- [ ] Commit each completed shape as one undoable action.
- [ ] Clip shapes safely at document boundaries.
- [ ] Test click-only, reverse-direction, edge, and maximum-size gestures.
- [ ] Benchmark large outline shapes if profiling shows meaningful cost.

Filled rectangle and ellipse modes should be separate follow-up work so the first shape milestone remains small.

### 3. Rectangular selection foundation

- [ ] Add a UI-independent rectangular selection model.
- [ ] Support creating, replacing, and clearing a marquee selection.
- [ ] Use `Escape` to clear the active selection.
- [ ] Render the marquee as an overlay without changing document pixels.
- [ ] Keep selection-only changes out of document history and dirty-state tracking.
- [ ] Test pointer mapping, reverse-direction selection, clipping, and zoom/pan alignment.

### 4. Selection editing and clipboard

- [ ] Delete the selected pixels as one history action.
- [ ] Copy and cut exact RGBA pixel data.
- [ ] Paste into a movable, non-destructive floating selection.
- [ ] Support committing and cancelling a floating selection.
- [ ] Move selected pixels as one history action.
- [ ] Investigate PNG clipboard interoperability alongside an application-native pixel format.
- [ ] Add platform-aware keyboard shortcuts and headless workflows where supported.
- [ ] Benchmark maximum-size copy, paste, and move operations.

### 5. Phase review

- [ ] Run all unit and headless UI tests.
- [ ] Run relevant Release benchmarks on the same hardware and power profile.
- [ ] Manually test the completed workflows on supported desktop platforms.
- [ ] Review memory behavior on the maximum canvas size.
- [ ] Update `README.md` with the completed milestone.
- [ ] Update `DEVELOPMENT.md` with final decisions, limitations, and measured findings.

## Phase 2 engineering considerations

### History representation

Selections, pasted images, and moved regions contain multiple colours. The existing uniform-colour span entry is not sufficient for these operations. Add a bounded region, patch, or tile-based history representation that participates in the existing 128 MiB history budget.

One completed user gesture should remain one undo action.

### Canvas responsibilities

The canvas currently coordinates pointer events, drawing sessions, overlays, and bitmap updates. New shape and selection gesture state should move into focused, testable collaborators when those features are introduced instead of adding many more branches directly to `PixelCanvas`.

Avoid creating a broad tool plug-in framework before the concrete interaction requirements are known.

### Preview rendering

Shape guides, selection marquees, and floating pasted content should render as overlays. Pointer movement must not repeatedly mutate and undo document pixels. Apply the final pixel changes only when the user commits the gesture.

### Performance and memory

A 4096×4096 RGBA selection requires approximately 64 MiB before temporary buffers or history data. Selection and clipboard operations therefore need explicit memory accounting, bulk pixel operations, and maximum-canvas tests.

Prefer contiguous rows, spans, or tiles over per-pixel objects and notifications.

### Keyboard shortcuts and focus

New shortcuts should follow platform conventions and use the established shortcut-resolution path. Editable toolbar controls must not consume document shortcuts after the user returns to the canvas. Add headless regression coverage for each new shortcut workflow.

### PNG scope

Selections and tool previews are temporary editor state and do not need to be stored in PNG files. PNG remains suitable throughout Phase 2 because all committed results are still a single flattened image.

## Phase 3: Layers and editable project files

Layers and a custom project format should be designed together. PNG cannot preserve layers, and using it as the only Save format after introducing layers could silently discard editable information.

Proposed order:

- [ ] Introduce a project model that owns canvas dimensions and ordered layers.
- [ ] Preserve the current single-canvas behavior as an initial one-layer project.
- [ ] Add active-layer selection and a basic layer panel.
- [ ] Add layer creation, deletion, naming, visibility, and ordering.
- [ ] Add cached compositing with explicit maximum-canvas memory measurements.
- [ ] Extend history so layer operations remain bounded and undoable.
- [ ] Design a versioned custom project format.
- [ ] Make project Save/Open preserve all editable information.
- [ ] Treat PNG as flattened Import/Export once layered projects are supported.

Opacity, blend modes, masks, groups, and animation should follow after the basic layer and project-file architecture is stable.

## Later phases

Potential later work includes:

- Palette and swatch management
- Additional selection modes and transforms
- Filled shapes and more drawing tools
- Layer opacity and blend modes
- Animation and timeline support
- Autosave and crash recovery
- Preferences and workspace persistence
- Packaging, installers, update delivery, and platform release testing
- Accessibility and localisation review

These items should be prioritised after evaluating the completed editing and layer workflows rather than committed to a fixed order now.
