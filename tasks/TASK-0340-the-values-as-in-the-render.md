---
id: 0340
title: The values as in the render, with three decimals on a pill on a dimension line outside the body
status: Done
phase: R18c
opened: 2026-11-10
depends-on: [0332]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0082, 0084, 0091, 0092, 0099, 0105, 0107, 0108, 0110, 0111, 0113, 0114, 0115]
writes:
  create:
    - tasks/TASK-0340-the-values-as-in-the-render.md
    - docs/adr/0115-the-values-as-in-the-render.md
    - 3DEngine.ImageTests/DimensionLineTests.cs
  modify:
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.App/SketchValues.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.ImageTests/EdgeValueTests.cs
    - 3DEngine.ImageTests/SketchValueTests.cs
    - 3DEngine.ImageTests/PendingChipTests.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - docs/adr/0082-the-values-on-the-edges-of-the-redesign.md
    - docs/adr/0105-the-values-on-the-edges-of-an-active-sketch.md
    - docs/adr/0110-a-proposal-shows-each-value-on-its-chip.md
    - docs/adr/README.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0340 — The values as in the render, with three decimals on a pill on a dimension line outside the body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gap G3: in the render "Mix 1" each value has a
dimension line with extension lines and an outlined pill in the mono font, as "60.000 mm", outside the body. The window
draws a filled box with "60 mm", 24 pixels out from its edge, and two of the boxes are over the body. The owner accepted
each recommended answer on 2026-11-10, Q6 A among them: three decimals and the unit. The owner ordered "start R18c" on
2026-11-10.

## Goal

Each value on an edge looks as in the render, is exact, and never covers the body.

## Scope (in)

1. ADR-0115, from slice S3 of the proposal and Q6 A.
2. The form of a value: three decimals and the unit, and more decimals when the value has them.
3. The dimension line parallel to the edge, outside the outline of the body or the sketch, with extension lines; a leader
   for a radius.
4. The outlined round pill in the mono font on the dimension line, with a target of at least 44 pixels.
5. Tests: the form; the rule of the line; each value of a real extrude outside its outline; each pill on its line and
   outside the outline; the picture of the bracket at 1440 by 900.

## Scope (out)

- The rows of a body (S4, R18d). They show the text of the values, so they show three decimals too.
- The lines of a proposal on its arc or its sheet (S9, R18i).
- A check that no extension line crosses another one.

## Acceptance criteria

- [x] ADR-0115 exists and records Q6 A. ADR-0082, ADR-0105 and ADR-0110 show the amendment.
- [x] A test proves that each pill is outside the outline of the body; it fails when the pill is at the old place.
- [x] A test proves the form of three decimals; it fails with the old form.
- [x] The picture of the bracket at 1440 by 900 shows the dimension lines, the extension lines and the pills.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r18c-values) · v0.325

## Method

Mechanical: the form of a value; the lines from the edges that the window has.
Judgement:  the gap and the place of the pill; the leader of a radius; the form of the lines of a proposal.
Weakest:    the drawing of the extension lines and of the dimension line: a test checks their ends, and the picture
            shows them, but no test reads their pixels.

## Progress

- 2026-11-10: opened and started on the order of the owner, at `main` 6e8d1a7 (v0.324), branch r18c-values. ADR-0115 is
  free on `main` and on each branch; the other session took ADR-0114 for P0.141.
- 2026-11-10: run 42905740219 passed on the work commit. Ten injections each failed a named test: the old form, a
  negative zero, the side of the line, the reach past the outline, the old place, the half of the pill, the line through
  the pill, a pill as high as its target, the old form of a sketch value and a radius with no leader. The first run of
  the injection of the side was not caught: no case needed the turn of the normal. The case of the top edge now needs
  it. The image tests crashed the test host in some local runs, also on `main`; R-0062 records it. Closed at v0.325.
- 2026-11-10: merge of `main` at 6b614f1 (R20, TASK-0341, with no version): no conflict; TASK-0341 needs no ADR-0115.
