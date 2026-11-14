---
id: 0129
title: The list of the hidden bodies fits the display, a stuck plate stays at its place, and a step text follows its command
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0109', '0124', '0125', '0127']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Overlay.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/DimensionOverlay.cs
  - 3DEngine.App/StepTexts.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/SketchActions.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.Vulkan/EdgeLines.cs
  - 3DEngine.ImageTests/HiddenBodiesTests.cs
  - 3DEngine.ImageTests/DimensionLabelsTests.cs
  - 3DEngine.ImageTests/FixedStatusTests.cs
  - 3DEngine.ImageTests/FixedStatusImageTests.cs
  - 3DEngine.ImageTests/BodyEdgesImageTests.cs
enforced-by: 3DEngine.ImageTests/HiddenBodiesTests.cs
---

# ADR-0129 — The list of the hidden bodies fits the display, a stuck plate stays at its place, and a step text follows its command

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (`docs/reviews/2026-11-14-codebase-review.md`) gives five findings, V173 to V177, in the
window, the dimension plates and the edge lines. TASK-0356 (P0.146) corrects them. Three corrections change a decision:

- **V174.** ADR-0124 item 3 gives a list of each hidden body and then "Show all". The list has no scroll bar, and a touch
  does not scroll a window of the library, so at 844 by 390 with six hidden bodies "Show all" was under the display.
- **V176.** ADR-0125 accepts a plate under a control when no move frees it, but the code left it where the moves up ended,
  at the top edge, on the other plates.
- **V177.** ADR-0127 item 2 gives a step text to the next applied command with its name, so a command of another client
  with the same name could take it.

The fixes of V173 (the covers of the plates) and V175 (the edge lines) keep their decisions and correct the code. On
2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The list of the hidden bodies (amends ADR-0124 item 3).** When each row of the list fits in the room above the
   anchors, the list keeps its order: each hidden body, then "Show all". When the rows do not fit, "Show all" comes first,
   then the hidden bodies that fit, then the line "N more", which takes no tap. So "Show all" and each row are in the
   display. The room counts the rows of 44 pixels with the spacing and the padding of the style.
2. **A stuck plate (amends ADR-0125 item 4).** A plate that no move of item 4 frees stays at its place, under the control.
   The covers of a plate are the fixed controls and the panels of this frame and of the last frame, so a window that the
   frame draws after the dimensions, as the window of the live sketches, the card of a proposal, the window of the images
   or the list of the hidden bodies, is a cover too (V173).
3. **A step text follows its command (amends ADR-0127 item 2).** The window gives the text of a step for the identifier of
   its command, which it gives the command before the apply, and which each event of that command carries
   (`CauseCommandId`). The card of a proposal holds the identifier of each command (`ProposalCard.CommandIds`). A command
   of another client with the same name gets its name in words.
4. **A T-vertex (amends ADR-0109 item 1).** A triangle whose cross product is at most 1e-9 of the square of its longest
   side has no area, so the rounding of a turned body does not give it an edge. A side P-Q of one triangle and a chain of
   collinear sides of one triangle each from P to Q are one edge with two triangles: each part takes the triangle of P-Q,
   and P-Q goes. So a T-vertex with no sliver, two T-vertices side by side and a turned T-vertex add no line inside a flat
   face, and a crease along P-Q draws its parts (V175).
5. **No contract change, no engine change.** Each change is presentation (ADR-0007). The identifier of a command exists in
   the contract (`Command.CommandId`).

## Consequences

**Good:** a finger reaches "Show all" on a landscape phone; plates do not pile at the top edge; "Undo:" names the step of
the window and not a step of an agent; a turned body keeps clean faces.

**Bad:** with a long list on a small display, the hidden bodies that do not fit have no row; the person uses "Show all" or
the panel. A plate that a move along one axis cannot free stays under its control, also when a free place exists in
another direction: in the probe of V173 at 1280 by 720 this is three of 96 views. A collinear side of a real sliver of
less than 1e-9 of its length draws no line.

## Alternatives considered

- **A scroll of the list by a drag of the finger.** Refused: the library scrolls a window by the wheel only, and the fixed
  controls take a drag nowhere else.
- **A fifth move of a plate, down or up from its place right or left of the cover.** Refused for this fix: the rule of
  ADR-0125 item 4 moves a plate along one axis, and the review asked for the place of a stuck plate only.

## History

- 2026-11-14 · TASK-0356 · The record, from the findings V173 to V177 of the codebase review of 2026-11-14.
- 2026-11-14 · TASK-0356 · Shipped at v0.342.
