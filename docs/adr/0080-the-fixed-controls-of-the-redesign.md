---
id: 0080
title: The title of the document, Undo, Redo and the status line are fixed controls of the redesign
status: Accepted
topic: Clients, rendering
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0030', '0031']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.Vulkan/Window.cs
enforced-by: 3DEngine.ImageTests/FixedControlsTests.cs
---

# ADR-0080 — The title of the document, Undo, Redo and the status line are fixed controls of the redesign

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening); the first is "the fixed controls
(name, Undo, Redo)". The pictures of the "mix" direction of the designer brief show the name of the program and of the
document at the top left, Undo and Redo as two round buttons at the bottom left, and a status line at the bottom right.

Today the panel holds the address of the surface, the line "File: name", and the buttons Undo and Redo (ADR-0030 item
3, ADR-0031 item 11). At 1280 by 720 the panel has no free line (finding V50): a title line above it, or two buttons
under it, push Show all out of the panel unless the panel loses lines.

### Q1. Where the title goes

- **A.** The title bar of the panel: "3DEngine | name | unit". The line "File: name" leaves the panel.
- **B.** A line of its own above the panel.

A gives a line back to the panel; B takes one. The bitmap font has no middle dot, so the parts are joined by a bar.
**Recommended: A.**

### Q2. Where the status line goes

- **A.** On the row of Undo and Redo, right of Redo, with the address of the surface. The address leaves the panel.
- **B.** At the bottom right, as in the brief.

The window of a sketch fills the right column of the display today and hides the bottom right. **Recommended: A** until a
later slice moves the window of the sketch.

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The title (Q1 A).** The title bar of the panel is "3DEngine | " and the length unit, then " | " and the name of the
   file or "unsaved", so that a long name that the bar cuts does not cut the unit (finding V115, TASK-0252); a host with
   no files, as the phone, gives "3DEngine | " and the unit. The window keeps its
   identifier, the name of the panel. The line "File: name" leaves the panel.
2. **Undo and Redo.** Two round anchors of 48 pixels (`size-anchor` of the design system) at the bottom left, 16 pixels
   from the edges, with a bent arrow that the draw list draws. They keep the actions, the keys and the gestures of
   ADR-0031 item 11. The panel ends above them: a constraint ends its height above the anchors, and each frame keeps its
   place between the top of the safe area and the row of the anchors, so a drag or a smaller display does not put it
   over them; the person can still move it (finding V114, TASK-0252).
3. **The status line (Q2 A).** The address of the surface, on the row of the anchors, right of Redo, in the colour of the
   text of the panels.
4. **The taps.** Each fixed control is a rectangle that a tap does not pass, beside the rectangles of the panels.
5. **The amendments.** This record amends ADR-0030 item 3 (the name of the file is in the title) and ADR-0031 item 11
   (Undo and Redo are fixed controls, not buttons of the panel).
6. **The safe area.** The host gives the insets of the safe area of the window (`SDL_GetWindowSafeArea`). The panel
   starts 16 pixels inside the top and the left inset, and the anchors and the status line are 16 pixels above the
   bottom inset, so the system bars of the phone do not cover them (finding V118, TASK-0252).

## Consequences

**Good:** the panel holds two lines fewer, and the fixed controls stay in one place on each display, as in the brief.

**Bad:** the status line is not at the bottom right yet; the arrows of Undo and Redo are drawn shapes, not glyphs.

**Next:** the tool ring that follows the selection; the fonts of ADR-0078 item 3.

## History

- 2026-11-06 · TASK-0248 · The record, Accepted under the standing order.
- 2026-11-06 · TASK-0248 · Shipped at v0.233.
- 2026-11-06 · TASK-0252 · Item 1: the unit comes before the name (V115). Item 2: the place and the height of the panel
  hold each frame (V114). Item 6: the safe area (V118). Findings of the codebase review of 2026-11-06 (e).
