---
id: 0315
title: A focus on one body, as a state of the client that writes nothing to the log
status: Ready
phase: R17b
opened: 2026-11-08
depends-on: [0314]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0043, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0102, 0104, 0105, 0107]
writes:
  create:
    - tasks/TASK-0315-a-focus-on-one-body.md
    - docs/adr/0101-a-focus-on-one-body.md
    - 3DEngine.ImageTests/BodyFocusTests.cs
  modify:
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/BodyVisibility.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/CommandLineTests.cs
    - 3DEngine.ImageTests/DocumentPathTests.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/0100-a-path-line-from-the-document-to-the-selection.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0315 — A focus on one body, as a state of the client that writes nothing to the log

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

To review one body today, the person hides each other body. Each hide is the command `SetBodyVisibility` (ADR-0029):
it goes into the log and into Undo, and "Show all" afterwards also shows the bodies that were hidden on purpose.
Onshape and Shapr3D give an Isolate that is a view state only. The proposal
`docs/proposals/2026-11-08-document-and-body-selection.md` gives the analysis (option B). The owner accepted each
recommended answer on 2026-11-08: the other bodies are not drawn (Q4 A), and the person enters the focus from the
ring or the command line, with no double tap (Q5 A).

## Goal

The person shows one body only for a review, and ends the review with one action, with no command in the log.

## Scope (in)

1. ADR-0101, from option B and the answers Q4 and Q5 of the proposal.
2. **The focus.** A state of the client: one body identifier, or null. While a focus lasts, the scene does not draw
   the other bodies. The visibility of each body, the log and Undo do not change.
3. **The entry.** "Focus" is a segment of the tool ring of a selected body (ADR-0081) and a command of the command line
   (ADR-0098). On a portrait display "Focus" is a segment of the thumb ring.
4. **The path line** of ADR-0100 shows "Focus" before the name of the body, with a close mark of at least 44 pixels.
5. **The exit.** The close mark, Escape at the level of the body, and "Focus" again end the focus. A body that leaves
   the session (for example a body that an agent consumes) ends the focus. The selection of a face or an edge inside
   the focus does not end it.
6. Tests: the focus draws one body; the log has the same count of entries before and after a focus; each exit; a
   body that leaves the session; the ring segment and the command; a picture at 1440 by 900.

## Scope (out)

- The other bodies dimmed, as in Onshape. A later task can add it with a pass with transparency (Q4).
- A double tap (Q5 A). A focus on more than one body. A focus that a saved document keeps.
- Any change of the engine, of the contract or of the log.

## Acceptance criteria

- [ ] ADR-0101 exists and records the answers Q4 A and Q5 A of 2026-11-08. ADR-0081, ADR-0098 and ADR-0100 show the
      amendment.
- [ ] A test proves that a focus and its exit add no entry to the log; it fails when the focus uses `SetBodyVisibility`.
- [ ] Each exit of item 5 has a test.
- [ ] The picture at 1440 by 900 shows one body, the path line with "Focus" and the close mark.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

- Start after TASK-0314: the path line and Escape up to the document are its base.
- `3DEngine.App/BodyVisibility.cs` keys the visibility by the mesh of each body; the focus can filter there or in
  `SceneRenderer.cs`. Keep one place for the rule.
- Escape: with a focus, Escape at the level of the body ends the focus first, then the next Escape clears the body.
  Record this order in ADR-0101.
- Check the numbers at commit: TASK-0315 and ADR-0101 were free on each branch on 2026-11-08 at `main` 18297b8.
- `governed-by` must hold each ADR in force whose `affects` meets the write set. `TaskGovernanceGateTests` gives the
  list.

## Method

Mechanical: the filter of the bodies to draw; the segment and the command from those that exist.
Judgement:  the order of Escape with a focus; the place of the close mark.
Weakest:    no person used a focus in this window yet.

## Progress

- 2026-11-08: opened from the proposal, with the answers of the owner.
- 2026-11-09: governed by ADR-0099, after the merge of R16p at v0.298.
