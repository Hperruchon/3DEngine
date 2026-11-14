---
id: 0358
title: A proposal shows its values in the rows, a pending node on the history arc, a glow on the edges and Undo after an accept
status: Active
phase: R18i
opened: 2026-11-14
depends-on: [0356, 0357]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0092, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0117, 0119, 0120, 0123, 0124, 0127, 0129, 0130]
writes:
  create:
    - tasks/TASK-0358-the-proposal-in-the-window.md
    - docs/adr/0130-the-proposal-in-the-window-as-in-the-render.md
    - 3DEngine.ImageTests/ProposalWindowTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0104-the-history-arc.md
    - docs/adr/0110-a-proposal-shows-each-value-on-its-chip.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0358 — A proposal shows its values in the rows, a pending node on the history arc, a glow on the edges and Undo after an accept

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Slice S9 of the study of the gaps to the render (`docs/proposals/2026-11-10-gaps-to-the-render.md`, gap G11) is roadmap row
R18i. The window shows a proposal of an agent as an arc with Reject and Accept, its command lines and each chip as "60.000 →
75.000 mm". The render also gives the old value crossed out in the rows, a pending node on the history arc, a glow on the
edge of the screen and "Applied · Bracket rebuilt · Undo" after Accept. On 2026-11-14 the owner ordered work alone until
Sunday, with the recommended option for each question.

## Goal

A person sees which value a proposal changes, where its step goes in the history, and that the window waits, and after an
accept one tap undoes the change.

## Scope (in)

1. ADR-0130, which amends ADR-0092 items 6 and 7, ADR-0104 and ADR-0110 item 2.
2. The rows of a proposed value; the pending node; the glow; the line after an accept with Undo.
3. Tests for each.

## Scope (out)

- The preview of the new shape (S11) and the author of a step (S12).
- An undo of the whole proposal in one tap.

## Acceptance criteria

- [ ] ADR-0130 exists. ADR-0092, ADR-0104 and ADR-0110 show the amendment.
- [ ] A test proves the value now and the proposed value of a row, and no change of another row.
- [ ] A test proves the pending node above the newest node, with its text.
- [ ] A test proves the glow on a landscape and a portrait display while a proposal waits, and no glow with none.
- [ ] A test proves the line after an accept, Undo as a target of 44 pixels, its action, and no Undo after a reject.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the rows, the node, the glow, the line and its button.
Judgement:  the place of the pending node, the frames of the glow, and the name of the body in the line.
Weakest:    "Undo" undoes one step, so a proposal of several commands needs one tap for each; "rebuilt" names the selected
            body. No person used the change in the window.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R18h (v0.341) and P0.146, at the commit of P0.146
  (e602df8), branch r18i-proposal. TASK-0358 and ADR-0130 are free. Six injections each failed a named test; each file came
  back byte for byte. The picture of the mix with a pending value shows the four parts.
