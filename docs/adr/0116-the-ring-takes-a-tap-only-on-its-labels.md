---
id: 0116
title: Only the icon and the label of a segment of a body ring take a tap, and the ring fades while the view moves
status: Accepted
topic: Clients, input
date: 2026-11-11
supersedes: []
superseded-by: []
amends: ['0081', '0104']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.History.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/GestureFadeTimer.cs
  - 3DEngine.ImageTests/RingTapTests.cs
enforced-by: 3DEngine.ImageTests/RingTapTests.cs
---

# ADR-0116 — Only the icon and the label of a segment of a body ring take a tap, and the ring fades while the view moves

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked for a test of the window on a real project of many pieces. The proposal
`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md` gives the test on a copy of `Bibliotheque120.3de` (19 bodies)
at 1280 by 720. Problem 1: with "Fond" selected, a click on the module to its right, under the band of the ring, opened the
pad of Move of "Fond", and no click selected the module. The owner took improvement A of the proposal on 2026-11-10 ("All
three").

ADR-0081 item 2 says that the band of the ring is a part of the panels for a tap. A band is 68 pixels deep and spans about
105 degrees (ADR-0119 item 2), so on a project of many pieces it covers the pieces next to the selected one. The icon and the
label of a segment use a small part of the band.

The history arc is on the band too (ADR-0104). While the view turns, the ring and the arc stay over the model, and the person
cannot see the pieces under them.

## Decision

1. **The target of a tap (amends ADR-0081 item 2).** On a ring of a body, a segment takes a tap only on its target: the box of
   its icon and its label, 4 pixels larger at each side, and at least 44 by 44 pixels about its centre (ADR-0027 item 6,
   ADR-0113), inside the segment. The press and the release must both be on the target of the same segment. The rest of the
   band is the scene: a tap there selects the body under it, or ends the selection on no body. A segment shows its hover only
   on its target.
2. **The fade (amends ADR-0081 and ADR-0104 item 3).** While an orbit, a pan or a zoom runs, the ring and the history arc are
   drawn with an alpha of 0.2, less than the dim of a waiting proposal (0.35, ADR-0092 item 7). The gesture runs from the
   first intent of the view until 0.25 seconds after its last one, so the frames of a drag and the steps of a wheel are one
   gesture. The ring and the arc then come back. The targets do not change during the fade.
3. **The ring of a sketch.** The ring of an active sketch keeps its whole band as the target of a tap (ADR-0086). A tap of a
   sketch puts a point, so a tap that goes past a segment makes a change in the Document. A later task can apply item 1 to it.
4. **No contract change, no engine change.** The change is in the client only. A tap that goes to the scene gives the select
   intent that exists (ADR-0026).

## Consequences

**Good:** a person selects a piece under the band with one click, and sees the model while the view turns.

**Bad:** the target of a narrow segment on a small ring is an axis-aligned box. At an angle of 45 degrees its corners can be
outside the segment, and a tap there goes to the scene. The test checks the size of the box and that its centre is on its
segment, not each corner. A finger on the phone is not tested; the tests simulate a mouse and a touch.

**Next:** "Dimensions" on the ring and the hidden bodies (R20b), and the labels of the dimensions (R20c).

## Alternatives considered

- **A tap on the band with no body under it gives the tool.** It keeps more of the band for the tools, but a click on the
  ground near a segment then opens a tool, and the person cannot predict which part of the band is a tool.
- **Hide the ring during a gesture.** The person then loses the place of the tools for a moment after each step of the wheel.
  The fade keeps them visible.

## History

- 2026-11-11 · TASK-0341 · The record, from improvement A of the UX test of 2026-11-10 and the judgement of the session.
