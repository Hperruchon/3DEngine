---
id: 0365
title: The arc of a proposal moves right of the rows of the selected body, and its lines keep off the tools of the ring
status: Done
phase: R18n
opened: 2026-11-14
depends-on: [0364]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0092, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0129, 0130, 0131, 0133, 0134]
writes:
  create:
    - tasks/TASK-0365-the-arc-of-a-proposal-right-of-the-rows.md
    - docs/adr/0134-the-arc-of-a-proposal-right-of-the-rows.md
    - 3DEngine.ImageTests/ProposalPlaceTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0133-the-caption-of-a-proposal-on-its-arc.md
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

# TASK-0365 — The arc of a proposal moves right of the rows of the selected body, and its lines keep off the tools of the ring

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Gap H2 of the comparison at the other sizes of the brief (`docs/proposals/2026-11-14-the-render-at-other-sizes.md`) is roadmap
row R18n: at 1024 by 768 the arc of a proposal and Reject lie on the rows "Made from" and "Distance". On 2026-11-14 the owner
ordered work alone until Sunday, with the recommended option for each question.

## Goal

The arc of a proposal lies on no row of the selected body, and its caption and lines lie on no tool of the ring.

## Scope (in)

1. ADR-0134, which amends ADR-0092 item 7 and ADR-0133 item 2.
2. The ring of the arc right of the rows when the arc lies on them and the moved arc stays in the display.
3. The tools of the ring as obstacles of the block of the lines, with a move to the left.
4. A test at six landscape sizes with the body at three places.
5. R-0066 for the sizes under 1024 pixels wide.

## Scope (out)

- The arc on a portrait display (gap H5, R18o).
- A small landscape display under 1024 pixels wide (R-0066).

## Acceptance criteria

- [x] ADR-0134 exists. ADR-0092 and ADR-0133 show the amendment.
- [x] A test proves from 1024 pixels wide that no item of the arc lies on the rows, and that neither the caption nor a line
      lies on a tool of the ring; at each size each item is inside the display.
- [x] R-0066 records the sizes under 1024 pixels wide.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r18n-arc-right-of-rows) · v0.350

## Method

Mechanical: the box of the arc, the move to the right, the move of the block to the left.
Judgement:  no smaller ring; the tools as obstacles; the limit of 1024 pixels.
Weakest:    the band of a moved arc is not on the band of the tools; under 1024 pixels wide the arc lies on the rows (R-0066).

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R18m, branch r18n-arc-right-of-rows. TASK-0365,
  ADR-0134 and R-0066 are free. A first form also made the ring smaller when the moved arc passed the right edge; at 800 by
  600 and 900 by 600 that ring put the caption and the lines on the tools, so it went. The test found that the moved arc put
  the lines on "Move", so the tools became obstacles; at 1024 by 768 and 1920 by 1080 the block found no place lower down,
  so it moves left too. The test also showed that Accept lies on the target of "Edit sketch" at each size before this task;
  the arc takes the tap first (finding V160), so the test checks the caption and the lines only. Four injections each failed
  the test: no move, no limit at the right edge, no tools as obstacles and no move to the left. Each file came back byte for
  byte.
- 2026-11-14: run 43553796087 passed on the work commit. The picture of the mix test at 1024 by 768 shows the rows free,
  the arc right of them and the lines off the tools. Closed at v0.350.
