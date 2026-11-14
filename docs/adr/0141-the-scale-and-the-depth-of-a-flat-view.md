---
id: 0141
title: A flat view has a scale of its own and a far plane behind the box, and the rows of a portrait proposal stay under the middle
status: Accepted
topic: Rendering, clients
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0135', '0137']
amended-by: []
affects:
  - 3DEngine.Core/Models/Camera.cs
  - 3DEngine.Core/Picking/Picker.cs
  - 3DEngine.Core/Gizmo/TranslateGizmo.cs
  - 3DEngine.App/SketchOverlay.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.Tests/FlatViewTests.cs
  - 3DEngine.ImageTests/FlatViewWindowTests.cs
  - 3DEngine.ImageTests/ProposalPortraitTests.cs
enforced-by: 3DEngine.Tests/FlatViewTests.cs
---

# ADR-0141 — A flat view has a scale of its own and a far plane behind the box, and the rows of a portrait proposal stay under the middle

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (d) gave two medium findings on the flat views of ADR-0137 and a low finding on ADR-0135:

- V188: a flat view took its scale from the distance of the eye, so a zoom of ten times put the eye inside the part, cut its
  front face and started a pick behind it.
- V189: the edge lines pull their depth by 0.002 of the depth range. The depth of a flat view is linear, and the far plane
  was at least 1000, so the pull was about 2 mm on a part of 20 mm.
- V190: the rows under the part took their floor from the sheet of the proposal of the last frame, which the first frame of a
  proposal does not have.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **A scale of its own (amends ADR-0137 item 3).** A flat view takes its scale from a distance of its own: the distance of
   the eye when the flat view starts, and the distance that Frame gives. A zoom in a flat view changes that scale only; the eye
   stays where Frame put it. The projection, the pick, the overlay, the gizmo and the pan take the scale.
2. **A far plane behind the box (amends ADR-0137 item 3).** In a flat view the far plane is 1.05 times the distance from the
   eye to the far side of the box of the scene, so the pull of the edge lines is a small part of the box. A frame in a flat
   view is not held at half of the far plane.
3. **The rows of a portrait proposal (amends ADR-0135 item 3).** While a proposal waits on a portrait display, the rows under
   the part do not go above the middle of the display, which the sheet never passes; so the rule holds in the first frame of a
   proposal, and the window keeps no bottom of the sheet of the last frame.
4. **No contract change.** The camera and the rows are presentation (ADR-0007).

## Consequences

**Good:** a person can zoom far into a flat view, an edge line behind a face stays hidden, and no row covers the sheet.

**Bad:** a flat view that starts from an eye inside the part keeps that eye until a Frame. No edge was seen on a GPU in a flat
view; the pull is computed.

## Alternatives considered

- **A pull for a flat view in the shader of the edges.** Refused for now: the shader is compiled to SPIR-V, and the far plane
  gives the same result with no new compiled file.

## History

- 2026-11-14 · TASK-0374 · The record, from the findings V188 to V190 of the codebase review of 2026-11-14 (d), under the
  order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0374 · Shipped at v0.359.
