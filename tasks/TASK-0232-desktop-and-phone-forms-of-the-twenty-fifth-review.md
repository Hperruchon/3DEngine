---
id: 0232
title: The start again sees the commands of the stop, and the panel and the phone hold their next forms
status: Active
phase: P0.92
opened: 2026-11-06
depends-on: [0231]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0061, 0072, 0073]
writes:
  create:
    - tasks/TASK-0232-desktop-and-phone-forms-of-the-twenty-fifth-review.md
  modify:
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/PhoneLifecycle.cs
    - 3DEngine.App/LocalSessionHost.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.ImageTests/DesktopSessionTests.cs
    - 3DEngine.ImageTests/CutUiTests.cs
    - 3DEngine.ImageTests/LocalSessionHostTests.cs
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - docs/adr/0073-the-phone-cuts-and-keeps-its-document.md
    - docs/android-test-sheet.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0232 — The start again sees the commands of the stop, and the panel and the phone hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (c) found V97 (medium) and V98 to V103 (low). By `docs/templates.md`, section 7, they
come after the next product phase, and after P0.91. The standing order of 2026-11-05 answers Q1 of the review with B.

## Goal

The start again chooses the previous document after the surface stops, the list of a cut stays in the panel, the
phone shows the line of the document, the sink of the phone writes outside the commit, and the texts are true.

## Scope (in)

1. V97: the choice of the previous document after the stop of the surface.
2. V98: the list of a cut inside the panel for an extrude, and the rectangle of its popup in the rectangles of the
   panels, so that a tap on the popup does not reach the scene (the review of Codex of v0.212, finding 1, confirmed by
   reading).
3. V99: the line of the document on the phone.
4. V100: the report of FanOutSink outside the commit.
5. V101 (Q1 B): ADR-0073 item 3 gives the order of a blocked thread.
6. V102 and V103: the affects of ADR-0073 and the two texts.

## Scope (out)

- E156, E157 and T147 to T151 (TASK-0231).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  V98 keeps the count of lines: the list goes on a new line only when the line has no room, and Show all
            shares the last line when it fits, so V50 holds. V101 sets the hint to 1, the value that Q1 B keeps.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (c).
- 2026-11-06: the write set takes 3DEngine.App/Ui/PanelAction.cs (the record DocumentLine, V99) and
  3DEngine.App/PhoneLifecycle.cs (the comments of V101).
- 2026-11-06: V97, V98 (the list of an extrude and the open list) and V99 have tests that failed before the change, or
  an injection of the old form that fails them. V100: an injection of the report inside the commit fails the new test.
  V101 to V103 are texts and one hint; the APK builds with zero warnings.
