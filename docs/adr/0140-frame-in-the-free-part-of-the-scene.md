---
id: 0140
title: Frame fits the bodies in the free part of the scene, and the centre of the picture moves there
status: Amended
topic: Rendering, clients
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0023', '0137']
amended-by: ['0145']
affects:
  - 3DEngine.Core/Models/Camera.cs
  - 3DEngine.Core/Picking/Picker.cs
  - 3DEngine.Core/Gizmo/TranslateGizmo.cs
  - 3DEngine.App/SketchOverlay.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.Tests/FrameFreePartTests.cs
  - 3DEngine.ImageTests/FrameFreePartWindowTests.cs
enforced-by: 3DEngine.Tests/FrameFreePartTests.cs
---

# ADR-0140 — Frame fits the bodies in the free part of the scene, and the centre of the picture moves there

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R21c (ADR-0137 item 5) and problem 6 of the UX test of 2026-11-10: Frame fits the bodies in the whole picture, so the
panel of the document at the left, about 440 pixels wide at 1280 by 720, covers a part of them, and on a phone the rows
under the part cover its lower half. The camera had its centre at the centre of the picture only.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The shift of the centre (amends ADR-0023 item 1 and ADR-0137 item 3).** The camera has a shift: the centre of the
   picture in normalized coordinates, with y up. The projection with and without perspective, the ray of a pick, the overlay
   of the window and the gizmo move each point by it, so the target shows at the shift. A frame of the whole picture sets
   it to 0.
2. **A frame in a part.** The camera fits a box in a part of the picture, a rectangle in normalized coordinates: the target
   goes to the centre of the box, the shift to the centre of the part, and the sphere around the box fills FrameFill of the
   smaller half of the part (ADR-0094).
3. **The free part.** The window gives the free part of the scene from its windows of the last frame: inside the safe area
   less 16 pixels; on a landscape display right of a window at the left that reaches below the upper fifth (the panel or the
   rows), 16 pixels from it; under each window of the upper fifth (the top bar, the path line, the command line) and above
   each window of the lower fifth (the anchors), 8 pixels from them; on a portrait display above the rows under the part. A
   free part of less than 100 pixels on a side gives the whole picture.
4. **Who frames in it.** Frame of the window and the key F frame the bodies in the free part. The first frame after an open
   keeps the whole picture, because the window has no windows of the document yet.
5. **No contract change.** The camera is render state (ADR-0007).

## Consequences

**Good:** after Frame the bodies are in the part of the scene that the person sees, right of the panel or the rows, and an
orbit turns them there.

**Bad:** while the panels change after a Frame, the bodies stay where Frame put them; a new Frame moves them. No person used
it.

## Alternatives considered

- **A target off the centre of the bodies.** Refused: an orbit then turns the bodies about a point outside them.
- **The free part from the windows of this frame.** Refused: Frame runs between two frames of the window, so the windows of
  the last frame are the ones that the person sees.

## History

- 2026-11-14 · TASK-0372 · The record, from row R21c and problem 6 of the UX test of 2026-11-10, under the order of the owner
  of 2026-11-14.
- 2026-11-14 · TASK-0372 · Shipped at v0.358.
- 2026-11-14 · TASK-0379 · ADR-0145 item 3 amends item 3: a tall window at the right edge cuts the free part, as the panel
  at the left does.
