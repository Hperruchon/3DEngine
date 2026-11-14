---
id: 0367
title: The medium and low findings of the codebase review of 2026-11-14 (c)
status: Done
phase: P0.148
opened: 2026-11-14
depends-on: [0366]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0120, 0122, 0124, 0127, 0129, 0130, 0131, 0133, 0134, 0135, 0136]
writes:
  create:
    - tasks/TASK-0367-medium-and-low-findings-of-the-review-of-2026-11-14-c.md
    - docs/adr/0136-the-arc-ends-before-the-tools-and-the-caption-wraps.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/StepTexts.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/ProposalCaptionTests.cs
    - 3DEngine.ImageTests/ProposalPlaceTests.cs
    - 3DEngine.ImageTests/FixedStatusTests.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0133-the-caption-of-a-proposal-on-its-arc.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0367 — The medium and low findings of the codebase review of 2026-11-14 (c)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (c) (`docs/reviews/2026-11-14-c-codebase-review.md`) gives one medium finding (V181) and
three low findings (V182 to V184). Section 7 of `docs/templates.md` puts them after the next product phase, R18o.

## Goal

No word of the summary of a proposal is lost, "Undo" of the line holds the count of the last commit of the accept, a moved
block of lines stays inside the inner circle, and "Edit sketch" takes a tap while a proposal waits.

## Scope (in)

1. V181: the whole caption under the arc in lines that fit the chord, with no cut.
2. V182: the count of the log at the last commit of the accept.
3. V183: the left limit at the top and the bottom of the plate.
4. V184: no end of the arc on the target of a tool of the ring, and the comment of `DimRing`.
5. A test for each, which fails before the fix.

## Scope (out)

- The arc on a portrait display (R18o).

## Acceptance criteria

- [x] A test proves with a summary longer than the chord that each word of it shows.
- [x] A test proves that a command of another client after the accept takes "Undo" away.
- [x] A test proves that the plate of a moved block is inside the inner circle.
- [x] A test proves that no end of the arc lies on the target of a tool of the ring.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0148-review-findings) · v0.353

## Method

Mechanical: the lines of the caption; the count in the outcome; the corners of the plate.
Judgement:  the arc ends earlier, and the tools do not move; ADR-0136.
Weakest:    on most landscape layouts the arc is shorter, so less of the caption fits on the band; no person saw it.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (c) (TASK-0366).
- 2026-11-14: started after R18o, branch p0148-review-findings. V181 and V184 change rules of ADR-0092 and ADR-0133, so
  ADR-0136 records them. V181: the whole caption under the arc wraps at its spaces into lines that fit the chord. V182: the
  step texts keep the count of the log right after the applied event of each of the last commands, and the host gives the
  count after the last command of the proposal. V183: the left limit tests the circle at the edge of the plate farther from
  the centre. V184: the arc ends at the first angle from 305 down to 255 degrees where Accept and its word lie on no tool
  target, and the caption has its middle at the middle of the arc. The test of V160 can no longer find an end on a tool, so
  its control went; the guard of V160 stays for a ring with no free angle. The box of a turned letter at an end passes the
  box of the round circle by up to a pixel, so the caption test allows 2 pixels. Four injections each failed a named test.
  Each file came back byte for byte.
- 2026-11-14: run 43559073792 passed on the work commit. The picture of the mix test at 1280 by 720 shows Accept above
  "Edit sketch", which is free. Closed at v0.353.
