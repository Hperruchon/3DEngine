---
id: 0351
title: The hidden bodies from the status line
status: Done
phase: R20b2
opened: 2026-11-12
depends-on: [0348]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124]
writes:
  create:
    - tasks/TASK-0351-the-hidden-bodies-from-the-status-line.md
    - docs/adr/0124-the-selection-after-a-hide-and-the-hidden-bodies-in-the-status-line.md
    - 3DEngine.ImageTests/HiddenBodiesTests.cs
  modify:
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - docs/adr/0029-selection-and-hide-gestures.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
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

# TASK-0351 — The hidden bodies from the status line

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The UX test of 2026-11-10 (`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`, problem 5) found that a hidden
body is hard to bring back. Hide on the ring ends the selection, so the list of the bodies comes back over the model. The
status line still says "19 bodies". The only way back is "Show all" at the end of the list, and it shows each hidden body.
The owner took improvement B on 2026-11-10 (roadmap row R20b). R20b1 (TASK-0348) gave "Dimensions" on the ring. This task
is R20b2, which the owner started with "Go" on 2026-11-12, after the merge of P0.144.

## Goal

After a Hide, the person keeps a selection, sees how many bodies are hidden, and brings back one or each from the status
line, with no visit to the panel.

## Scope (in)

1. ADR-0124, which amends ADR-0029 item 3 and ADR-0093 item 3.
2. When the selected body hides, the selection goes to the next visible body in the order of the list, else to the one
   before it, else to none.
3. The status line shows "N hidden · Show" while a live body is hidden. A tap on that part opens a list of the hidden bodies
   above the status line. A tap on a row shows that body; "Show all" shows each one.
4. Tests for items 2 and 3.

## Scope (out)

- "Show hidden" on a ring of the document. No ring shows with nothing selected, and its place is a decision of the owner
  (register entry R-0064).
- The labels of the dimensions (R20c).

## Acceptance criteria

- [x] ADR-0124 exists. ADR-0029 and ADR-0093 show the amendment.
- [x] A test proves the selection after a Hide; it fails when the selection goes to none while a visible body stays.
- [x] A test proves the part "N hidden · Show"; it fails when the status line does not count the hidden bodies.
- [x] A test proves that a tap on a row of the list shows that body, and that "Show all" shows each one.
- [x] A test proves that the part stays on a phone, where the whole line has no room. The landscape phone keeps the line
      and the part, a window of 540 pixels keeps the part alone, and a portrait phone of 390 pixels has no room (R-0064).
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r20b2-hidden-bodies) · v0.336

## Method

Mechanical: the count in the status line, the list of the hidden bodies, and its actions.
Judgement:  the neighbour after a Hide (the order of the list, not the nearest body on the screen), and a list from the
            status line in place of a ring of the document.
Weakest:    no person used the change in the window or on the phone; the tests drive Dear ImGui with no device. A
            portrait phone of 390 pixels shows no part, so the panel stays its way back, and the ring of the document waits
            for the owner (R-0064).

## Progress

- 2026-11-12: opened by the order "Go" of the owner, after the merge of P0.144.
- 2026-11-12: the selection after a hide, the part of the status line, the list, six tests, and register entry R-0064.
- 2026-11-12: run 43217469649 passed on the branch. Closed at v0.336.
