---
id: 0465
title: Where the path line has no room for it, "N hidden" leaves the path line, and the ring of the part keeps "Show hidden"
status: Active
phase: R32a
opened: 2026-11-25
depends-on: [0464]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0080, 0083, 0093, 0099, 0100, 0101, 0102, 0108, 0113, 0124, 0127, 0129, 0138, 0168, 0172, 0174, 0178]
writes:
  create:
    - tasks/TASK-0465-n-hidden-leaves-the-path-line.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/ShowHiddenTests.cs
    - docs/adr/0178-show-hidden-on-the-ring-of-the-part-and-the-path-line.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
---

# TASK-0465 — Where the path line has no room for it, "N hidden" leaves the path line, and the ring of the part keeps "Show hidden"

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0072 (2026-11-21): on a portrait phone of 360 by 640 pixels with a sheet, "Sheet" is right of "Views", and the
path line has about 130 pixels. The file name at its floor of 48 pixels (ADR-0100 item 1), its chevron and "2 hidden" need
more, so the part ran under "Views". Three rules cannot all hold there: the floor of the file name, the gap of 12 pixels left of
"Views" (ADR-0138) and the part on the path line (ADR-0178 item 2). On 2026-11-25 the owner answered Q3 A: "N hidden" leaves the
path line, and the ring of the part keeps it as "Show hidden" (TASK-0464).

## Goal

At 360 pixels with a sheet, no item of the path line goes under "Views", and the way back to the hidden bodies is on the ring of
the part.

## Scope (in)

1. `DesktopUi.PathRow`: where the room of the path line cannot hold the file name at its floor, its chevron, the gap and the
   part, the part leaves the path line in that frame. The list of the hidden bodies closes with it.
2. `ShowHiddenTests.With_A_Sheet_The_Path_Line_And_The_Top_Bar_End_Left_Of_Views` gets the case of 360 by 640 pixels: no part on
   the path line, each item 12 pixels left of "Views", "Show hidden" on the ring, and its tap gives the action of "Show all".
3. ADR-0178 item 2 says where the part leaves the path line.
4. The roadmap row, R-0072 closed, and the ledger entry v0.449.

## Scope (out)

- The landscape phone of 640 by 360 (R-0076). Row R32b does it.
- A new place for the count. The answer of the owner keeps the ring as the way back.

## Acceptance criteria

- [x] `With_A_Sheet_The_Path_Line_And_The_Top_Bar_End_Left_Of_Views` passes at 360, 390 and 412 pixels; at 390 and 412 pixels
  the part stays on the path line.
- [x] With the change of `PathRow` removed, the case of 360 pixels fails.
- [x] Each other test of `ShowHiddenTests`, and `ResponsiveLayoutTests`, passes with no change.
- [x] R-0072 is closed, and `docs/register.md` holds 12 open entries of 15.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- The part shows on the path line only while no body is selected (ADR-0178 item 2, finding V226), so the path line then has
  one segment, the file name, and `FocusRoom` is 0.
- The floor of the file name is the width of the text that `Fit` gives at 48 pixels, not 48: the cut text with "..." is
  narrower than 48 pixels, and at 390 pixels the part fits only with that width.

## Outcome

Status: Active · commit (the merge of the branch r32a-hidden-on-ring) · v0.449

## Method

Mechanical: the test of the owner's case, from the register entry, the room as `PathLine` gives it to `PathRow`.
Judgement:  a rule of fit, not a rule of width: the part leaves wherever the room cannot hold it, so the same rule serves each
            display (ADR-0178 item 2, "the phone and the desktop use the same rule"). The answer named 360 pixels with a sheet.
Weakest:    on a display where the part leaves the path line and the status line, the count of the hidden bodies does not show
            until the person opens the ring of the part. The answer of the owner accepts this.

## Progress

- 2026-11-25: opened on the order of the owner "Start R32a", branch `r32a-hidden-on-ring` from `origin/main` at `f8373f48`
  (v0.448).
