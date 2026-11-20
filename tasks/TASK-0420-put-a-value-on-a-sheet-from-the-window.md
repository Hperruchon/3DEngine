---
id: 0420
title: "Put on sheet" in the pad of a value of a flat view places that value on the view of the sheet with the same frame
status: Active
phase: R30e
opened: 2026-11-20
depends-on: [0411, 0413]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0089, 0092, 0097, 0100, 0101, 0102, 0104, 0105, 0106, 0110, 0111, 0114, 0115, 0116, 0117, 0120, 0124, 0127, 0128, 0129, 0130, 0137, 0138, 0140, 0146, 0148, 0149, 0151, 0163, 0166, 0167, 0168, 0170, 0171, 0172, 0174, 0175]
writes:
  create:
    - tasks/TASK-0420-put-a-value-on-a-sheet-from-the-window.md
    - docs/adr/0175-put-a-value-on-a-sheet-from-the-window.md
    - 3DEngine.App/SheetValuePlace.cs
    - 3DEngine.ImageTests/SheetValueTests.cs
  modify:
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.App/SheetActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Android/**
---

# TASK-0420 — "Put on sheet" in the pad of a value of a flat view places that value on the view of the sheet with the same frame

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 7 records the answer D7 A of the owner of 2026-11-18: "Put on sheet" on a value chip of a flat view places that
value on the view of the sheet with the same frame, and R22d keeps the dimension line and the proposal of the agent. Row R30e
holds the slice. On 2026-11-20 the owner ordered "then start R30e", which gave this task TASK-0420 and ADR-0175.

The engine has the command already: PlaceSheetDimension (ADR-0158) places a value of a constraint or the distance of an
extrude on a view of a sheet, at a place on the paper. GetSheet gives the centre of the box of each view (ADR-0171 item 1), so
a client can turn a point of the design into a place on the paper. This slice is a change of the window only.

## Goal

A person selects an extrude in a flat view, taps a value, and puts that value on the sheet with one more tap. The log records
one PlaceSheetDimension, as an agent sends it.

## Scope (in)

1. A value of the selected body keeps the two ends of its measured edge in the design (ADR-0175 item 1).
2. The line "Put on sheet" in the pad of that value, in a flat view, when a live sheet has a view with the frame of that flat
   view (item 2).
3. The sheet, the view and the place on the paper, and one PlaceSheetDimension off the frame thread (items 3 to 5).

## Scope (out)

- The values of an active sketch, and a word of the command line. Their chips have no edge of a body.
- The dimension line, the move of a value on the sheet page, and the proposal of the agent (R22d, R30f).
- A change of `Engine.Core/**` or of `Engine.Contracts/**`.

## Acceptance criteria

- [x] A tap on "Put on sheet" sends one PlaceSheetDimension with the source and the kind of the value, on the first view with
      the frame of the flat view, 10 mm of paper outside its measured edge.
- [x] The line does not show outside a flat view, with no sheet view of that frame, or for a value with no edge.
- [x] A value that the sheet shows already, and a flat view with no sheet view, give a text and no command.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active · commit (the merge of the branch r30e-put-on-sheet) · v0.403

The pad of a value of the selected body in a flat view has "Put on sheet", and a tap places that value on the view of the
sheet with the same frame, 10 mm outside its edge, by one PlaceSheetDimension. The ledger entry v0.403 gives the proof.

## Method

- Read ADR-0162 item 7, ADR-0158 item 1 and the mapping of GetSheet (ADR-0155 item 5, ADR-0171 item 1) before the code.
- Ran `SheetValueTests` after each change, then 15 injections with `inject0420.js` in the scratchpad; each file came back
  byte for byte (sha256).
- A clean build, the phone project, `dotnet test`, the write-set check and `eng/repo-gates` before the commit.

## Progress

- 2026-11-20: opened under the order of the owner "then start R30e", branch r30e-put-on-sheet from `main` at `c8466b2`.
- 2026-11-20: the work: ADR-0175, `SheetValuePlace`, the edge of a value, the button of the pad, the host action and 17
  tests. The first form of the button put "Delete" of a compact pad outside the pad at 640 by 360; the compact pad now has
  the button beside its fields. Fourteen of fifteen injections failed a named test. Ledger entry v0.403.
