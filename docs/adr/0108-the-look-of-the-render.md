---
id: 0108
title: The window has the look of the render, an almost black ground and outlined controls with no fill
status: Accepted
topic: Clients, rendering
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0078', '0081', '0092', '0095', '0098', '0113']
amended-by: []
affects:
  - 3DEngine.App/Ui/Theme.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.ImageTests/ThemeTests.cs
  - 3DEngine.ImageTests/CommandLineTests.cs
  - 3DEngine.ImageTests/RingImageTests.cs
  - 3DEngine.ImageTests/EdgeValueImageTests.cs
  - 3DEngine.ImageTests/ProposalArcTests.cs
enforced-by: 3DEngine.ImageTests/ThemeTests.cs
---

# ADR-0108 — The window has the look of the render, an almost black ground and outlined controls with no fill

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner asked if the window is close to the render "mix" of the designer brief. The proposal
`docs/proposals/2026-11-10-gaps-to-the-render.md` found that the layout is as in the render and the look is not (gap G1,
part of G9, and the look of G10). The owner accepted each recommended answer. Q2 A: the ground of the render, almost
black, in place of the cool grey `#3f4955` of ADR-0078. This record is slice S1 (R18a, TASK-0332).

The colours below come from the render Mix 1 at 1440 by 900: a sample of its pixels gave a ground from `#03060b` at the
corners to `#0b1b2a` at the middle, outlines near `#3c606c`, an accent near `#30eaff` for the icons and the search mark,
and a green near `#8cf4bf` for "correct". The render is a picture; its colours are a look, not tokens.

## Decision

1. **The ground (Q2 A, amends ADR-0078 item 1).** The clear of the scene is `#070d14`. Each colour of a text keeps a
   contrast of at least 4.5:1 on it, and each colour of a mark or an outline at least 3:1, as WCAG 2.2 gives the ratio:
   text 19.5, the quiet text 6.3, "correct" 14.7, "failed" 5.7, the amber 9.6, the accent 13.3, the outline 3.8, the
   line of a sketch 12.1, another sketch 8.5, a reference 6.7 and the snap 7.2. The windows take a ground one step above
   it (`#0b1219` at alpha 0.94). The soft light at the middle of the render waits: the window draws over the scene, so a
   light there covers the body; the pass of the edges (R18b, ADR-0109) or a later slice can give it.
2. **The controls (amends ADR-0081 item 2 and ADR-0095 item 2).** A segment of the ring, a chip of a value, the command
   line, an anchor and a button have a thin outline (`#4a7480`) and no fill. A hover gives the accent at alpha 0.16, a
   press at alpha 0.32. The tool in use keeps its press fill and has an outline of 2.5 pixels in the accent. An icon of
   the ring and the arrow of an anchor are in the accent, with the alpha of the style, so they dim with the ring while a
   proposal waits. A chip lies over the body, so it has the ground of the scene at alpha 0.85, as the render, and not a
   blue fill. An outline on the edge of a window that the clip of the window cuts (the command line, "Frame" and
   "Home") is drawn 1 pixel inside its control.
3. **The fields.** Each field and each button of a window has an outline of 1 pixel (`FrameBorderSize`).
4. **The command line (amends ADR-0098 item 2 and ADR-0113 item 4).** A round outlined field with a search mark at its
   left, the text "Type a command or a value", and "Ctrl K" in the quiet text at its right while the field is empty and
   has room for it. The field is 400 pixels wide, or 30 percent of the width of the display when that is less, and at
   least 200 pixels, because the text of the render, the search mark and the keys need 394 pixels in the font of the
   window. A field too narrow for the text shows "Type a command". The input has no key of the Command key of macOS, so
   each platform shows "Ctrl K"; register entry R-0060 holds "Cmd K". A typed value is slice S8 (R18h).
5. **The states.** "correct" is green (`#8cf4bf`) in the first row of a body and in the status line; "failed" with its
   code, and the count of the failed bodies in the status line, are red (`#e85c5c`), as before.
6. **The agent (amends ADR-0092 item 7).** On a landscape display "AGENT IS WAITING", after an amber dot, ends 12 pixels
   left of the command line, in the middle of its height. When it meets the title there, it goes under the title, as
   before. A portrait display has no command line: the dot and the word follow the title, or go under the path line
   (ADR-0113 item 3).
7. **No change of the places of the ring, the rows or the panel, of the taps, or of the contract.**

## Consequences

**Good:** the window has the ground and the controls of the render, and a test holds the contrast of each colour on the
ground. The body and its marks stand out more on the dark ground.

**Bad:** a control with no fill has less weight; a person sees the tool in use by its outline and its light fill only.
The field of the command line takes 100 pixels more of the top bar on a large display. The soft light at the middle of
the scene waits.

## History

- 2026-11-10 · TASK-0332 · The record, from slice S1 of the proposal and the answer Q2 A of the owner of 2026-11-10.
