---
id: 0138
title: A button "Views" opens a net of the six flat views and the 3D view, and the keys 1, 3, 7 and 0 give them
status: Accepted
topic: Clients
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0100', '0127', '0137']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.Core/Input/InputSamples.cs
  - 3DEngine.Vulkan/SdlInput.cs
  - 3DEngine.ImageTests/FlatViewControlsTests.cs
  - 3DEngine.ImageTests/MixImageTests.cs
  - 3DEngine.Tests/SdlInputTests.cs
enforced-by: 3DEngine.ImageTests/FlatViewControlsTests.cs
---

# ADR-0138 — A button "Views" opens a net of the six flat views and the 3D view, and the keys 1, 3, 7 and 0 give them

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0137 gave the camera six flat views and the command line their commands. Row R21 asks for the views with one tap each,
and ADR-0137 item 5 put the controls and the keys in R21b. The command line is only for a person who knows the names.

A first form drew the net of the views all the time: under the command line on a landscape display, and with a button on the
row of the anchors on a portrait display. The layout test found that the net lay on the arc of a proposal and on the tools of
the ring at 1024 by 768 and at 640 by 360, and that the row of the anchors of a phone had no room for one more button. No
free place on the screen holds a net of seven cells at each size.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The net.** A net of a cube holds the views: Top over Left, Front, Right and Back, Bottom under Front, and "3D" at the
   right of Top. Each cell is at least 44 pixels high and as wide as the widest word, at most a quarter of the safe width.
   The cell of the view of the camera has the colour of a tool in use.
2. **The button "Views" (amends ADR-0127 item 4 and ADR-0100 item 1).** "Views" opens and closes the net. On a landscape
   display it is on the row of the anchors after Redo, before the step of Undo, and the net opens above the anchors. On a
   portrait display, which has no command line, it is at the upper right; the path line ends 12 pixels left of it, and the
   net opens under the button and the path line. A tap on a cell gives its view and closes the net. So a view takes two
   taps, not one (amends the phrase "one tap each" of row R21 and ADR-0137 item 5).
3. **The keys.** With no field, no pad and no home screen that takes the keys, as F of ADR-0127 item 4: 1 gives Front, 3
   Right and 7 Top; with Control, 1 gives Back, 3 Left and 7 Bottom; 0 gives the 3D view. The keys follow the place of the
   key on the top row and on the pad, so an AZERTY keyboard needs no Shift.
4. **No contract change.** The controls are presentation (ADR-0007).

## Consequences

**Good:** each view has a control at each size and on each device, and a key on a keyboard; the net takes no room while it
is closed.

**Bad:** a view takes two taps. No person used the net; the look of the cells is a choice of this session.

## Alternatives considered

- **The net drawn all the time.** Refused: the layout test found it on the arc of a proposal and on the tools of the ring.
- **Three buttons for the views that the person uses most, and a second tap for the opposite views.** Refused: the meaning of
  a button would depend on the view, and the net shows each view in its place.
- **The keys by the letter of the layout, as F.** Refused: on an AZERTY keyboard the digits of the top row need Shift.

## History

- 2026-11-14 · TASK-0370 · The record, from row R21b and ADR-0137 item 5, under the order of the owner of 2026-11-14.
