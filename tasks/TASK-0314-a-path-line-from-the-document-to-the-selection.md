---
id: 0314
title: A path line from the document to the selection, and the rows of the document
status: Active
phase: R17a
opened: 2026-11-08
depends-on: [0313]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0102, 0104, 0105, 0106, 0107, 0110, 0111]
writes:
  create:
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - docs/adr/0100-a-path-line-from-the-document-to-the-selection.md
    - docs/proposals/2026-11-08-document-and-body-selection.md
    - 3DEngine.ImageTests/DocumentPathTests.cs
  modify:
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/FaceSelectionTests.cs
    - 3DEngine.ImageTests/EdgeSelectionTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - docs/adr/0075-a-second-tap-selects-the-face.md
    - docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - docs/adr/0099-the-rows-of-a-selected-body-take-the-place-of-the-panel.md
    - docs/adr/0102-a-project-in-any-folder.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - tasks/TASK-0315-a-focus-on-one-body.md
    - tasks/TASK-0332-the-look-of-the-render.md
    - tasks/TASK-0333-the-edges-of-the-body.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0314 — A path line from the document to the selection, and the rows of the document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner said that the window has no way to tell the selection of the whole document from the
selection of one body. The proposal `docs/proposals/2026-11-08-document-and-body-selection.md` gives the state of the
code, three tools (Onshape, Shapr3D, Plasticity) and three options. The owner accepted each recommended answer, Q1 to
Q7 A, on 2026-11-08. This task is option A. TASK-0315 (option B, the focus) comes after it.

The proposal file is not on `main` yet. This task creates it: it is in the commit that opens this task.

## Goal

The person sees where the selection is in the document, goes up one level with a tap or with Escape, and sees the
data of the document when nothing is selected.

## Scope (in)

1. ADR-0100, from option A and the answers Q1 to Q3 and Q6 of the proposal.
2. **The path line.** The first line of the top bar of ADR-0093 is a path: the file name (or "unsaved"), then the
   name of the selected body, then the selected face, then the selected edge. A separator is between two segments.
   With nothing selected, the path is the file name only. A long path cuts the middle segments first, so the file name
   and the last segment stay whole.
3. **A tap on a segment** selects that level: the file name clears the body, the face and the edge; the body name
   clears the face and the edge; the face clears the edge. A segment is at least 44 pixels high (ADR-0027 item 6).
4. **Escape goes up one level each time:** edge, face, body, document. This amends ADR-0075 item 2 and ADR-0077
   item 3, where Escape stops at the body.
5. **The rows of the document.** On a landscape display, with nothing selected, no sketch active and no home screen,
   the rows of ADR-0084 and ADR-0099 show the document: the kind ("Part"), the unit, the count of the live bodies,
   the count of the failed bodies with the code of each fault in red with the word "failed", then one row for each
   body. A tap on a body row selects the body. The rows read the data that the client has today.
6. **The phone.** On a portrait display the path line is the first line under the top bar, and a tap on a segment acts
   as on the desktop. The panel stays under it.
7. Tests: the path for each level, a tap on each segment, Escape from the edge to the document, the rows of the
   document with a failed body, the cut of a long path at 844 by 390 and 1440 by 900, and the path on a portrait
   display.

## Scope (out)

- The focus on one body. TASK-0315 does it.
- An assembly and its references (option C). It waits for the decision of the owner on the non-goals "Multi-Document
  in one runtime" and "Assemblies" (Q7 A).
- A double tap (Q5 A).
- Any change of the engine, of the contract or of the log. The selection stays a state of the client (ADR-0007).

## Acceptance criteria

- [ ] ADR-0100 exists and records the answers of the owner of 2026-11-08. ADR-0075, ADR-0077, ADR-0093 and ADR-0099
      show the amendment.
- [ ] Escape on a selected body clears the selection; a test fails when Escape stops at the body again.
- [ ] A tap on the file name clears the body; a tap on the body name clears the face.
- [ ] With nothing selected on a landscape display, the rows give the kind, the unit and the counts, and a failed body
      shows "failed" and its code.
- [ ] The pictures at 1440 by 900 and 844 by 390 show the path line and the rows.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- TASK-0313 is merged (v0.298), as Q6 A asks. The rows at the upper left of ADR-0099 are the base of item 5.
- The selection is in `3DEngine.App/BodyActions.cs`: `Selection`, `Face`, `Edge`. Escape is `CancelIntent { ByKey:
  true }`; today a body with no face falls to `default` and stays selected.
- Check the numbers at commit: TASK-0314 and ADR-0100 were free on each branch on 2026-11-08 at `main` 18297b8;
  TASK-0313 plans ADR-0099. A slice of the redesign that starts before this task can take them first.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the
  list.

## Method

Mechanical: the path line from the three fields of the selection; the rows from the live bodies.
Judgement:  the cut of a long path; the place of the path on a portrait display.
Weakest:    no person used a path line in this window yet.

## Progress

- 2026-11-08: opened from the proposal, with the answers of the owner.
- 2026-11-09: depends on TASK-0313 and is governed by ADR-0099, after the merge of R16p at v0.298.
- 2026-11-09: started at `main` 6f75607 (v0.310), branch r17a-path-line. The write set adds `BodyActionsTests.cs`, whose
  test held that Escape stops at the body, and ADR-0102, whose item 7 (the folder in the top bar) this task amends, and
  the file of TASK-0315, whose `governed-by` must name ADR-0100. ADR-0100 Q1 A: the rows of the document are the first
  part of the panel, so ADR-0099 Q1 A holds and the image tests of the panel keep their places. The codebase review is
  due before the close: the close is the fifth product milestone after the review of v0.304.
