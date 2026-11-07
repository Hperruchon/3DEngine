---
id: 0280
title: The reader gives the result of each earlier reader that accepted a file, and the ring of a sketch keeps each tool in reach
status: Active
phase: P0.115
opened: 2026-11-07
depends-on: [0279]
governed-by: [0004, 0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088]
writes:
  create:
    - tasks/TASK-0280-the-reader-of-each-earlier-result-and-the-ring-of-a-sketch.md
  modify:
    - Engine.Core/Import/StlReader.cs
    - Engine.Tests/Import/StlImportTests.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/0088-the-window-of-a-sketch-beside-its-ring.md
    - tasks/TASK-0281-medium-forms-of-the-thirtieth-review.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0280 — The reader gives the result of each earlier reader that accepted a file, and the ring of a sketch keeps each tool in reach

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (c) found three high findings: E180 (the reader refuses a text file with a zero byte,
which v0.243 read), E181 (the reader refuses a file that only v0.250 read) and V134 (the ring of a sketch can be
under the window of the sketch or below the display, and the window then holds no Finish). A high finding comes before
the next product phase. The standing order answers Q1 of the review with A.

## Goal

The reader gives each file the result of the first earlier reader that accepted it, and while the ring of a sketch shows,
each of its tools has a place that a tap reaches.

## Scope (in)

1. E180 and E181: each file gets the result of the first of three rules that accepts it: the rule of v0.243, the rule
   of HEAD, then the rule of v0.250; a test that runs the earlier rules on each form of the review and on random files.
2. E182 (Q1 A): ADR-0083 item 3 says that a document of v0.250 to v0.253 with a whole facet on the line of `solid`
   replays to the results of HEAD, which are the results of v0.243 and of v0.254.
3. V134: the band of the ring of a sketch inside the display and left of the window of the sketch, and the window keeps
   each button that the ring cannot give; ADR-0086 item 1 and ADR-0088 item 1 say so.

## Scope (out)

- The medium and low findings (TASK-0281, TASK-0282).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the order v0.243, HEAD, v0.250 keeps each choice of HEAD for the two classes of conflicts (Q1 A) and
            gives back each lost result; the window keeps a button when the middle of its segment is under a window,
            under an anchor or off the display.
Weakest:    to be written at the close.

## Progress
- 2026-11-07: a probe with the readers of e903bef, 45f416d, 7959c98 and db0138c read 200,000 random text files: 114,399
  were accepted by an earlier reader; the new reader refuses none of them, gives each the result of the first of v0.243,
  HEAD and v0.250 that accepted it, and changes no result of v0.254. The new forms fail with the reader of HEAD. Injections
  of the old rule of the window and of the old layout each fail a test of the ring.
