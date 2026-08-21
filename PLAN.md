# Pixel Editor development plan

This file tracks upcoming development. Completed technical decisions, trade-offs, blockers, and performance findings belong in [`DEVELOPMENT.md`](DEVELOPMENT.md).

## Current status

The initial MVP and stability pass are complete. Phase 2 now includes exact colour sampling, outline and single-colour filled shapes, and a non-destructive rectangular selection foundation.

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

- [x] Add outline rectangle and ellipse tools.
- [x] Use the selected colour and brush size.
- [x] Show a non-destructive guide while dragging.
- [x] Commit each completed shape as one undoable action.
- [x] Clip shapes safely at document boundaries.
- [x] Test click-only, reverse-direction, edge, and maximum-size gestures.
- [x] Add focused maximum-canvas benchmarks for both outline shapes.
- [x] Add Outline and Filled modes using the current selected colour.
- [x] Keep brush size specific to Outline mode.
- [x] Store previous mixed-colour rows in bounded patch history for filled-shape undo.
- [x] Test filled previews, single-pixel clicks, undo, redo, and exact colour restoration.
- [x] Benchmark filled rectangle and ellipse patch history through 4096×4096.

Later shape colour enhancement:

- [ ] Allow separate outline and fill colour selection.
- [ ] Integrate those colours with a reusable primary/secondary colour model rather than adding a shape-only second colour picker.
- [ ] Add an `Outline + Fill` mode while retaining the existing Outline-only and Filled-only modes.
- [ ] Define transparent-fill and colour-swap behaviour before implementing the UI.

### 3. Selection foundation

- [x] Add a UI-independent selection model.
- [x] Support creating, replacing, and clearing a marquee selection.
- [x] Assign `M` as the rectangular marquee shortcut.
- [x] Use `Escape` to cancel an active drag or clear the committed selection.
- [x] Render the marquee as an overlay without changing document pixels.
- [x] Keep selection-only changes out of document history and dirty-state tracking.
- [x] Test pointer mapping, reverse-direction selection, clipping, and zoom/pan alignment.

Selection combination groundwork:

- [x] Store arbitrary selected-pixel regions in a packed bit mask with cached bounds.
- [x] Add tested rectangle replace, add, subtract, and intersect operations to the core model.
- [x] Benchmark packed selection operations through 4096×4096.
- [x] Keep `Alt`/`Option` alone available for temporary eyedropper sampling.
- [x] Choose `Shift` for addition and `Shift+Alt`/`Shift+Option` for subtraction while the Selection tool is active.
- [x] Render the true boundary of a combined, potentially non-rectangular selection.
- [x] Connect the chosen add and subtract modifiers to the rectangular selection gesture.
- [ ] Decide whether and how to expose intersection as a shortcut when the feature is needed.

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
- More drawing tools
- Layer opacity and blend modes
- Animation and timeline support
- Autosave and crash recovery
- Preferences and workspace persistence
- Packaging, installers, update delivery, and platform release testing
- Accessibility and localisation review

These items should be prioritised after evaluating the completed editing and layer workflows rather than committed to a fixed order now.
