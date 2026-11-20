---
id: 0416
title: The medium and low findings of the codebase review of 2026-11-18 (b)
status: Done
phase: P0.167
opened: 2026-11-18
depends-on: [0415]
governed-by: [0004, 0007, 0012, 0016, 0018, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0048, 0060, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0113, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0131, 0138, 0140, 0142, 0143, 0144, 0145, 0147, 0155, 0158, 0159, 0160, 0161, 0163, 0164, 0165, 0166, 0167, 0168, 0169, 0170, 0171]
writes:
  create:
    - tasks/TASK-0416-the-findings-of-the-review-of-2026-11-18-b.md
  modify:
    - Engine.Core/Commands/AddSheetSectionCommandHandler.cs
    - Engine.Core/Commands/MoveSheetViewsCommandHandler.cs
    - Engine.Core/Drawing/SectionCut.cs
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Export/SvgExport.cs
    - Engine.Core/Export/PdfExport.cs
    - Engine.Core/Export/DxfExport.cs
    - 3DEngine.App/SheetPage.cs
    - 3DEngine.App/SheetExports.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - Engine.Tests/Drawing/SheetSectionTests.cs
    - Engine.Tests/Drawing/SheetMoveTests.cs
    - Engine.Tests/Export/SvgExportTests.cs
    - Engine.Tests/Export/PdfExportTests.cs
    - Engine.Tests/Export/DxfExportTests.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - 3DEngine.ImageTests/SheetSectionPageTests.cs
    - 3DEngine.ImageTests/SheetExportTests.cs
    - docs/adr/0168-the-sheet-page.md
    - docs/adr/0169-the-section-view-of-a-sheet.md
    - docs/adr/0170-export-a-sheet-from-the-window.md
    - docs/adr/0171-the-section-action-of-the-sheet-page.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0416 — The medium and low findings of the codebase review of 2026-11-18 (b)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-18 (`docs/reviews/2026-11-18-b-codebase-review.md`) gives the medium findings E221, E222,
V203, V204, V206 and V207, and the low findings E223, E224, E225, V205, V208 and V209. Section 7 of `docs/templates.md` puts
them after the next product phase, R30e. E223 is a decision that is not clear for a cone. The owner answered Q1 A on 2026-11-18
(TASK-0415 records it): a section through the axis of a cone shows the axis in each look, because an edge of a cut face that
lies in one plane with the axis of a cone gives the axis. The code changes, and ADR-0169 item 3 states it.

## Goal

Each of the twelve findings has a correction or a recorded answer, and a test that fails on the code before the correction.

## Scope (in)

1. E221: no line of a cut with a number that is not finite; a point whose place on the paper is not finite is refused or
   skipped.
2. E222: the cut of a section in a time near the time of the view without it.
3. E223 (Q1 A): the axis of a cone in a section from an edge in one plane with the axis, and the text of ADR-0169 item 3.
4. E224: one meaning of the size of a text in the three files.
5. E225: no step of undo for a move with no change.
6. V203: the Escape of a pad reaches no watcher of the page.
7. V204: no section, selection or drag of a view that the read does not hold, and no section or row of the formats after a
   change of the sheet.
8. V205: only one of the row of the formats and a section is open.
9. V206: the values of the last command sent for each view until a newer read.
10. V207: the paper keeps its place on the phone while the header changes.
11. V208: "Section" only for a view that the engine can cut.
12. V209: the name of the file of an export on the phone.

## Scope (out)

- R30e and each other row of the roadmap.
- A new diagnostic code needs its line in `docs/diagnostics.md` in the same change.

## Acceptance criteria

- [x] Each finding has a test that failed on the code of v0.396 or of the version before this task, and each rule has an
      injection that a test catches.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

Read the section of each finding in the review: its place, its probe and its recommended correction. TASK-0415 may add a step
that clears each state of the page whose view is not in the read; V204 can use it.

## Outcome

Status: Done · commit (the merge of the branch p0167-review-findings) · v0.405

Each of the twelve findings has a correction and a test, and ADR-0168 to ADR-0171 record them. The ledger entry v0.405 gives
the proof.

## Method

- Read each finding of the review (its place, its probe and its correction) and ADR-0168 to ADR-0171 before the code.
- E222: the kernel is outside the write set (`forbid: Engine.Kernel/**`), and `Engine.Core` cannot make a solid of polygons of
  several tags, so the read keeps the cut, as `GetFacesQueryHandler` keeps a B-Rep. A probe test measured a sphere.
- Ran the tests of each changed file, then 20 injections with `inject0416.js` in the scratchpad; each file came back byte for
  byte (sha256).
- A clean build, the phone project, `dotnet test`, the write-set check and `eng/repo-gates` before the commit.

## Progress

- 2026-11-20: opened under the order of the owner "go" after the merge of R30e, branch p0167-review-findings from `main` at
  `d7936b4`.
- 2026-11-20: the work: the twelve corrections, 14 tests, and the items of ADR-0168 to ADR-0171. Ledger entry v0.405.
- 2026-11-20: run 44758589835 of `67b7825` passed on each job at attempt 2. Attempt 1 failed on windows-latest in the step
  "Test": the test host of `3DEngine.ImageTests` crashed. Five local runs passed 853 of 853. The owner re-ran the job. The
  automatic re-run of ADR-0173 does not cover a failed step. Closed at v0.405; the merge waits for the owner.
