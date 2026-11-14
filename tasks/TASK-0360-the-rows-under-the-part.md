---
id: 0360
title: On a portrait display the rows of a selected body are under the part, and the panel has no controls of a selected body
status: Done
phase: R18k
opened: 2026-11-14
depends-on: [0359]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0117, 0119, 0120, 0122, 0123, 0124, 0125, 0127, 0129, 0130, 0131]
writes:
  create:
    - tasks/TASK-0360-the-rows-under-the-part.md
    - docs/adr/0131-the-rows-under-the-part-on-a-portrait-display.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/CutUiTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/DesktopUiTests.cs
    - 3DEngine.ImageTests/DimensionOverlayTests.cs
    - 3DEngine.ImageTests/DocumentPathTests.cs
    - 3DEngine.ImageTests/EdgeValueTests.cs
    - 3DEngine.ImageTests/FaceSelectionTests.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - 3DEngine.ImageTests/RingToolsTests.cs
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - docs/adr/0099-the-rows-of-a-selected-body-take-the-place-of-the-panel.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0360 — On a portrait display the rows of a selected body are under the part, and the panel has no controls of a selected body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The comparison at the other sizes of the brief (R18j, `docs/proposals/2026-11-14-the-render-at-other-sizes.md`, gap H3) found
the panel of the document over the part on a portrait display while a body is selected; the render gives the rows under
the part. The study took Q4 A under the order of the owner of 2026-11-14: the controls of a selected body leave the panel,
because with the rows on a portrait display that part of the panel shows in no orientation.

## Goal

A portrait display shows the part, its rows under it and its values, as the render at 768 by 1024, and each control of a
selected body has one place in each orientation.

## Scope (in)

1. ADR-0131, which amends ADR-0084 item 4 and ADR-0099 items 1 and 7.
2. The rows under the part on a portrait display, above the corner ring and the sheet of a proposal.
3. A chip on rows under the part goes above them.
4. The row "Hidden too" while the dimensions show.
5. The panel without the controls of a selected body, and its tests through the rows, the ring and the panel with no
   selection.

## Scope (out)

- The labels of the corner ring (R18l), the caption on the arc (R18m) and the arc right of the rows (R18n).

## Acceptance criteria

- [x] ADR-0131 exists. ADR-0084 and ADR-0099 show the amendment.
- [x] A test proves the rows under the part on a portrait display at the full width, with no panel.
- [x] A test proves the row "Hidden too" and its toggle.
- [x] The layout test proves at each size that no tool of the ring is under the rows and no window of the rows under the sheet.
- [x] Each test that reached the panel of a selected body passes through the rows, the ring or the panel with no selection;
      no test is deleted.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r18k-rows-under-part) · v0.345

## Method

Mechanical: the place of the rows, the shift, the chips above the rows, the row "Hidden too", the move of the tests.
Judgement:  the room of the rows on a phone; the removal of the controls of a selected body from the panel.
Weakest:    on a portrait phone the room under the part is small, so the rows show few values. No person used the change on
            a tablet or on the S23.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R18j (v0.344), branch r18k-rows-under-part. TASK-0360
  and ADR-0131 are free. An agent moved the 29 tests that reached the panel of a selected body to the rows, the ring and the
  panel with no selection, and deleted none; the session reviewed each change. The layout test found two faults that the
  scrolled panel hid before: the rows under the sheet of a proposal, and the first row on a tool of the ring at 360 by 640;
  the rows now end above the sheet and move up when the room is lower than the first row.
- 2026-11-14: five injections each failed a named test; the first form of the chip injection was caught by no test, so
  `BodyRowsTests.A_Chip_On_Rows_Under_The_Part_Goes_Above_Them` came in. Each file came back byte for byte.
- 2026-11-14: run 43545274637 passed on the work commit. The picture of the layout test at 390 by 844 shows the part, its rows
  under it and the corner ring. Closed at v0.345.
- 2026-11-14: reopened before the merge. The review agent of the next codebase review read the branch: on a portrait phone
  with the corner ring and a value, the rows had no room for the cut row, and the ring and the command line hold no cut.
  The row of a cut and the row "Hidden too" now come right after the first row and always show; a theory of five sizes
  in `BodyRowsTests` holds it, and two injections each failed it.
- 2026-11-14: closed again at v0.345; the entry v0.345 of the ledger came before this fix, and the next entry records it.
