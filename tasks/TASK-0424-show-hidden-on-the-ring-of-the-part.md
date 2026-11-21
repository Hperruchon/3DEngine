---
id: 0424
title: "Show hidden" on the ring of the part, and the count of the hidden bodies on the path line where the status line has no room
status: Active
phase: R20e
opened: 2026-11-20
depends-on: [0427]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0080, 0083, 0093, 0095, 0099, 0100, 0101, 0102, 0108, 0113, 0114, 0119, 0120, 0122, 0124, 0127, 0129, 0131, 0138, 0163, 0168, 0172, 0174, 0178]
writes:
  create:
    - 3DEngine.ImageTests/ShowHiddenTests.cs
  modify:
    - tasks/TASK-0424-show-hidden-on-the-ring-of-the-part.md
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.App/Ui/DesktopUi.Part.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - docs/adr/0178-show-hidden-on-the-ring-of-the-part-and-the-path-line.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0424 — "Show hidden" on the ring of the part, and the count of the hidden bodies on the path line where the status line has no room

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-20 the owner chose the path line for register entry R-0064. ADR-0178 reads the answer with the ring of the part of
ADR-0174 item 1. TASK-0427 opened this task, because ADR-0178 names its test.

## Goal

A person finds "Show hidden" on the ring of the part, and a portrait phone shows the count of the hidden bodies.

## Scope (in)

1. ADR-0178 item 1: the segment "Show hidden" on the ring of the part while at least one live body is hidden.
2. ADR-0178 item 2: the part "N hidden · Show" on the path line where the status line has no room for it.
3. `ShowHiddenTests`, and a state of the layout test of ADR-0172 with hidden bodies on a portrait phone.

## Scope (out)

- The write set holds only the test file now. The task adds the files of the window, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

- A change of the engine or of the contract (ADR-0178 item 3).

## Acceptance criteria

- [x] `ShowHiddenTests` shows the segment with a hidden body and not without, and its tap sends one "Show all".
- [x] At 390 by 844 with a hidden body the path line shows the part, and its tap opens the list of ADR-0124 item 3.
- [x] An injection of each item makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Outcome

Status: Active · commit (the merge of the branch r20e-show-hidden) · v0.412

## Method

Mechanical: the label "Show hidden" after the other tools of the first level of the ring of the part, with the open eye of
"Show"; the item `Path:hidden` and its text "N hidden"; the state Hidden of the layout test of ADR-0172; the counts of
`eng/test-floor.txt`.

Judgement (ADR-0178 does not settle these points):

- The status line decides first in each frame, so the top bar now comes after the anchors and the status line. Before
  TASK-0424 the top bar came first. One rule for each display: the part goes on the path line only when `StatusFit` gives
  no part. While a low pad holds the row of the anchors, neither place shows the part.
- With a selected body, the part goes at the end of the path line, after the last segment and the close mark of a focus,
  because the segments of the path stay together. With nothing selected, that place is right of the file name.
- The part takes its room before the segments, so a long file name is cut first; the folder of the file follows the part.
- The list under the path line has its left edge on the left edge of the part, inside the display. It takes the focus in
  its first frame, as the file menu does (finding V211), because it covers the folded structure view.

Weakest: no picture of the real window or of the phone was made. The width of the part comes from the font of the window,
and the test reads the rectangles of the items, not the pixels.

Injections (`inject0424.js` in the scratchpad), each restored byte for byte (sha256), each made a named test fail:

- no segment: `The_Ring_Of_The_Part_Has_Show_Hidden_Only_While_A_Body_Is_Hidden` and
  `A_Tap_On_Show_Hidden_Gives_The_Action_Of_Show_All`;
- the segment with no hidden body: `The_Ring_Of_The_Part_Has_Show_Hidden_Only_While_A_Body_Is_Hidden`;
- a wrong action: `A_Tap_On_Show_Hidden_Gives_The_Action_Of_Show_All`;
- no part on the path line of a phone: `On_A_Portrait_Phone_The_Path_Line_Counts_The_Hidden_Bodies` (three sizes) and two
  more;
- the path line repeats the part on the desktop: `Where_The_Status_Line_Has_Room_The_Path_Line_Does_Not_Repeat_The_Part`
  (three sizes);
- no list from a tap on the part, a release on the part that closes the list, and a list that closes when the status line
  has no part: `On_A_Portrait_Phone_The_Path_Line_Counts_The_Hidden_Bodies`;
- the list above the status line, not under the path line: `On_A_Portrait_Phone_The_Path_Line_Counts_The_Hidden_Bodies`
  and `The_List_Under_The_Path_Line_Opens_Over_The_Structure_View`;
- no focus of the list in its first frame: `The_List_Under_The_Path_Line_Opens_Over_The_Structure_View`;
- no room for the part when the path fits its segments: `On_A_Portrait_Phone_The_Path_Line_Counts_The_Hidden_Bodies`.

The first run of the injection of the place of the list stayed green on the test of the phone, because the list above the
status line is also under the path line; the test now requires the top of the list at most 8 pixels under the part.

## Progress

- 2026-11-21: opened after R30i, branch `r20e-show-hidden` from `d403db9d` (R30i, TASK-0428, v0.411), which is not merged
  yet. The segment, the part on the path line, `ShowHiddenTests` (11 cases) and the state Hidden of the layout test; 11
  injections. Ledger entry v0.412.
