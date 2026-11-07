---
id: 0269
title: The wait of the phone checks its turn, the STL reader keeps its accepted results, and the test sheet says what the phone keeps
status: Active
phase: P0.109
opened: 2026-11-07
depends-on: [0268]
governed-by: [0004, 0007, 0019, 0025, 0029, 0061, 0072, 0073, 0079, 0083]
writes:
  create:
    - tasks/TASK-0269-the-phone-wait-the-stl-name-and-the-test-sheet.md
  modify:
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.App/PhoneLifecycle.cs
    - 3DEngine.ImageTests/PhoneLifecycleTests.cs
    - Engine.Core/Import/StlReader.cs
    - Engine.Tests/Import/StlImportTests.cs
    - docs/adr/0079-the-activity-may-start-again.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/demo-stl-test-sheet.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0269 — The wait of the phone checks its turn, the STL reader keeps its accepted results, and the test sheet says what the phone keeps

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (b) found three high findings in the phone, the engine and the test sheet: E172 (a
stale session takes the foreground event of a new activity), E173 (the fix of E170 changed the result of the STL reader
for two forms that v0.243 read) and E174 (criterion 9 of the test sheet fails as written). A high finding comes before
the next product phase. The standing order answers the two questions of the review: E173 A and E174 A.

## Goal

The wait of the phone ends a stale session at each return; the STL reader gives the result of v0.243 for each file that
v0.243 read, and ADR-0083 says how a change of the reader keeps a recorded import; the test sheet says what the phone
keeps after the background.

## Scope (in)

1. E172: a check of the turn at each return of the wait, and at each pass of the loop.
2. E173: the name of the solid is the rest of its line when the next word is `facet` or `endsolid`, else the rule of
   v0.243; tests that compare each form with the result of v0.243; an item of ADR-0083 (E173 A).
3. E174: the text of `docs/demo-stl-test-sheet.md` (E174 A).

## Scope (out)

- The medium and low findings (TASK-0271, TASK-0272). The desktop findings V125 and V126 (TASK-0270).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  E172: the decision of one return of the wait is `PhoneLifecycle.AfterWait`, which a test holds, because the
            loop of the phone runs on no computer here. E173: the name ends at "facet" that "normal" and a number
            follow, on any line, or at "facet" or "endsolid" after its line; each file that v0.243 accepted has its
            first "facet" there. The write set named `3DEngine.Tests/SessionTurnsTests.cs`, which does not exist;
            it names the files of the change now.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07 (b).
- 2026-11-07: E172, E173 and E174. The test of E173 failed with the reader of HEAD and passed with the reader of
  v0.243, so its expected results are the results of v0.243; it passes with the new rule, with the three forms of E170.
  The test of E172 did not compile before `AfterWait`. The phone project builds with zero warnings.
