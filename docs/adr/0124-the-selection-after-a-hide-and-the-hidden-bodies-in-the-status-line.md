---
id: 0124
title: The selection after a hide, and the hidden bodies in the status line
status: Accepted
topic: Clients, rendering
date: 2026-11-12
supersedes: []
superseded-by: []
amends: ['0029', '0093']
amended-by: []
affects:
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.ImageTests/HiddenBodiesTests.cs
  - 3DEngine.ImageTests/BodyActionsTests.cs
enforced-by: 3DEngine.ImageTests/HiddenBodiesTests.cs
---

# ADR-0124 — The selection after a hide, and the hidden bodies in the status line

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The UX test of 2026-11-10 (`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`, problem 5) found that a hidden
body is hard to bring back. Hide ends the selection (ADR-0029 item 3), so the list of the bodies comes back over the model.
The status line (ADR-0093 item 3) counts the live bodies and does not say that some are hidden. The only way back is the
button "Show all" at the end of the list (ADR-0029 item 7), and it shows each hidden body.

The owner took improvement B on 2026-11-10 (roadmap row R20b): after a Hide the selection stays on a neighbour or on
nothing, the status line shows "N hidden · Show", and a ring of the document has "Show hidden". ADR-0122 holds the first
part of R20b, "Dimensions" on the ring. This record holds the hidden bodies.

## Decision

1. **The selection after a hide (amends ADR-0029 item 3).** When the selected body hides, the selection goes to the next
   visible live body in the order of the list of the bodies. When no visible body follows it, the selection goes to the
   nearest visible body before it. When no body stays visible, nothing is selected. A hide of a body that is not selected
   does not change the selection. The face, the edge and the focus end, as for each change of the selected body.
2. **The count of the hidden bodies (amends ADR-0093 item 3).** While at least one live body is hidden, the status line
   ends with the part "N hidden · Show", in the accent colour, after " · ". The part takes a tap; its target is as high as
   the anchors (48 pixels). The status line gives the text that fits: the whole line, else the line with no address, else
   the part alone, else none. So a landscape phone and a small window keep the way back. A portrait phone of 390 pixels
   has 38 pixels of room right of Home, so neither shows there; the panel keeps "Show all" (R-0064).
3. **The list of the hidden bodies.** A tap on the part opens a list above the status line, with its right edge on the
   right edge of the status line. It has a row for each hidden body, in the order of the list of the bodies, with the name
   of the body, or its kind and a short identifier, as the row of the panel (ADR-0029 item 7). With two or more hidden
   bodies, "Show all" is the last row. Each row is at least 44 pixels high (ADR-0027 item 6). A tap on the row of a body
   sends the command `SetBodyVisibility` with `visible` true for that body; "Show all" sends the same commands as the
   button of the panel (ADR-0029 item 6). The list closes on a second tap on the part, on a release outside the list and
   the part, and when no body is hidden. The list is a part of the panels for a tap.
4. **No ring of the document in this record.** With nothing selected, no ring shows, and the place of such a ring is not
   decided. Register entry R-0064 holds the question, with the place of the part on a portrait phone. The panel button
   "Show all" and the command "Show all" stay.
5. **No contract change, no engine change.** The selection and the list are states of the client (ADR-0007). A show is the
   command of ADR-0029 item 6.

## Consequences

**Good:** a person who hides a body keeps a selection and the tools of the ring, sees how many bodies are hidden, and brings
back one of them or each of them from the corner of the screen, with no visit to the panel.

**Bad:** the key H hides the selected body, so two presses of H hide two bodies: the second press hides the neighbour. The
neighbour comes from the order of the list, not from the screen, so it can be a body far from the hidden one. A portrait
phone of 390 pixels does not show the part, so there the way back is still the panel.

## Alternatives considered

- **The nearest body on the screen.** Refused for now: it needs the boxes of the bodies on the screen in the client of the
  selection, and the order of the list is the order that the panel shows.
- **Nothing selected after a hide.** Refused: the list of the bodies then comes back over the model (problem 5).
- **A tap on "Show" shows each hidden body.** Refused: the person often wants one body back, and "Show all" stays one tap
  further.

## History

- 2026-11-12 · TASK-0351 · The record, from improvement B of the UX test of 2026-11-10.
- 2026-11-12 · TASK-0351 · Shipped at v0.336.
