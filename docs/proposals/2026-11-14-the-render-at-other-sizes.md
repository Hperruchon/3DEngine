# The window and the render "mix" at the other sizes of the brief, and the slices that close the gaps

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-14.
- Asked by: roadmap row R18j (slice S10 of `docs/proposals/2026-11-10-gaps-to-the-render.md`), which the owner accepted on
  2026-11-10. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.
- Status: the recommended answers of section 5 are taken under that order; the owner can change each of them. It changes no
  code and no ADR. The slices are the roadmap rows R18k to R18n.

## 1. What I compared

- **The render:** the pictures of the step "the agent proposes a width of 75 mm" in the designer brief
  (`https://claude.ai/artifact/4dPFQPdAPF39BZQSqbgYFD`, section "UX examples", "The same step at each screen size"), at
  1280 by 720, 1024 by 768, 768 by 1024 and 390 by 844. The brief says: "The layout changes at each size; it does not only
  scale." Its table gives: 1280 by 720, smaller rings and four rows; 1024 by 768, the tool ring under the thumb at the lower
  right and the rows as a small block; 768 by 1024, the rows under the part and no history arc; 390 by 844, a thumb ring,
  the proposal arc and the history arc around a small part.
- **The window:** `r18i-proposal` at `bb85833` (R18i, v0.343 when it merges). The pictures come from four new tests of
  `3DEngine.ImageTests/MixImageTests.cs` (`mix-pending-1280x720.png`, `mix-pending-1024x768.png`, `mix-pending-768x1024.png`
  and `mix-pending-390x844.png`): the bracket of the render with the proposal of a width of 75 mm and its value named,
  drawn on the GPU. The tests give no state of the session, so the status line has no count of the log.

## 2. What is already as in the render

- At 1280 by 720 each element of the 1440 by 900 step is in its place: the rows with the crossed value, the ring, the
  history arc with the pending node "C1 to 75 ?", the arc of the proposal, the glow and the chips (R18a to R18i).
- At 1024 by 768 the same elements show, and the command line takes its short text "Type a command" (ADR-0108 item 4).
- On a portrait display the ring is at the lower right corner, under the thumb, as in the render (ADR-0081).

## 3. The gaps

| Gap | The render | The window | Size |
| --- | --- | --- | --- |
| H1 The caption of a proposal | "AGENT · WIDEN TO 75 MM" on the arc, above the part; no box over the part | At 1280 by 720 the box of the caption and the command lines is inside the ring, over the chip of the width, so "60.000 → 75.000 mm" cannot be read | 1280 by 720 |
| H2 The arc and the rows | The rows are a small block at the upper left; the arc is right of them | At 1024 by 768 the arc of the proposal and Reject lie over the rows "Made from" and "Distance" | 1024 by 768 |
| H3 The rows on a portrait display | The rows of the selected body under the part: its name, "Made from", Distance, Width with the crossed value, Depth | The panel of the document (Open project, Save as, New, Sketch on, Dimensions, the list of the bodies), over the part; the chips are drawn over the panel | 768 by 1024, 390 by 844 |
| H4 The labels of the corner ring | One label for each segment, each in its segment | The labels of the two last segments, "Focus" and "Dimensions", lie on each other at the upper end of the ring | 768 by 1024, 390 by 844 |
| H5 The proposal on a portrait display | The arc with Reject and Accept above the part, as on a landscape display | A sheet with the command lines, Reject and Accept over the panel (ADR-0092 item 7) | 768 by 1024, 390 by 844 |
| H6 The command line on a portrait display | "Type a command or a value" at the bottom, beside Undo and Redo (768 by 1024) | No command line on a portrait display (ADR-0098 Q2 A); Frame and Home on the row of the anchors (ADR-0127 item 4) | 768 by 1024 |
| H7 The tool ring of a tablet on its side | A thumb ring at the lower right corner, with "Hold the thumb in the corner, then slide to a tool" | The ring around the selected body, as at 1440 by 900 | 1024 by 768 |

## 4. The slices

| Slice | Gaps | What it does | Size | Decision |
| --- | --- | --- | --- | --- |
| R18k The rows under the part | H3 | On a portrait display the rows of the selected body take the place of the panel, under the part, at the full width; no chip over them | 1–2 | Amends ADR-0099, ADR-0100 |
| R18l The labels of the corner ring | H4 | Each label of the corner ring in its segment, and a narrower segment or a smaller text where a label does not fit | 1 | Amends ADR-0081 |
| R18m The caption on the arc | H1, H5 | The caption of a proposal along the top of its arc, not in a box inside the ring; the command lines under the arc and off each chip; on a portrait display the arc above the part in place of the sheet | 1–2 | Amends ADR-0092, ADR-0107 |
| R18n The arc right of the rows | H2 | The ring of the arc of a proposal stays right of the rows of the selected body, as the free ring of ADR-0107 stays right of the panel | 1 | Amends ADR-0107 |

H6 and H7 are decisions, not slices (section 5).

## 5. Questions, and the answers under the order of 2026-11-14

| Question | Options | Recommended, and taken |
| --- | --- | --- |
| Q1 Do R18k to R18n go on the roadmap, after R18j, in that order? | A. Yes. B. Another order or a subset. | **A.** R18k gives the largest change of a portrait display; R18l to R18n are small. |
| Q2 The command line on a portrait display (H6) | A. Keep ADR-0098 Q2 A: no command line on a portrait display. B. A field at the bottom on a portrait display wider than 600 pixels. | **A** for now. A phone has no keyboard of its own; the owner can take B for a tablet after the run of the owner on the S23 (R-0064 holds the portrait phone). |
| Q3 The tool ring of a tablet on its side (H7) | A. A thumb ring at the corner on a landscape display narrower than 1100 pixels. B. Keep the ring around the body. | **B** for now. The window does not know a tablet from a small laptop window; a mouse needs the ring near the body. |

## 6. What I did not verify

- I compared the step of the proposal only. The brief also gives 1920 by 1080, with an Activity column, and the workflows
  "Create a part", "Recover from a fault" and "Activity" at 1440 by 900.
- The pictures of the window come from the image tests, not from a run of the desktop program or of the phone.
- The portrait sizes of the window follow R19 (ADR-0113): each control stays in the display and free for a tap. The gaps
  H3 to H5 are the look of the render, not a control outside the display.
