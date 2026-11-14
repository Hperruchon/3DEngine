---
id: 0364
title: The caption of a proposal is on the band of its arc, and the command lines under the arc lie on no chip
status: Done
phase: R18m
opened: 2026-11-14
depends-on: [0362, 0363]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0092, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0122, 0129, 0130, 0131, 0133]
writes:
  create:
    - tasks/TASK-0364-the-caption-of-a-proposal-on-its-arc.md
    - docs/adr/0133-the-caption-of-a-proposal-on-its-arc.md
    - 3DEngine.ImageTests/ProposalCaptionTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0364 — The caption of a proposal is on the band of its arc, and the command lines under the arc lie on no chip

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Gap H1 of the comparison at the other sizes of the brief (`docs/proposals/2026-11-14-the-render-at-other-sizes.md`) is roadmap
row R18m: at 1280 by 720 the box of the caption and the command lines of a proposal lies on the chip of the width. On
2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

The caption of a proposal is on its arc, as the render, and no command line lies on a chip.

## Scope (in)

1. ADR-0133, which amends ADR-0092 item 7.
2. The caption on the band of the arc, along its top; a cut caption whole as the first line under the arc.
3. The block of the lines off each chip.
4. The split of gap H5 to roadmap row R18o.
5. Tests of the caption and of the lines with a chip at each place inside the ring.

## Scope (out)

- The arc on a portrait display (gap H5, R18o).
- The arc right of the rows at 1024 by 768 (gap H2, R18n).

## Acceptance criteria

- [x] ADR-0133 exists. ADR-0092 shows the amendment.
- [x] A test proves at three sizes that the caption is on the band, between Reject and Accept, along the arc.
- [x] A test proves that a long caption is cut on the arc and whole under it.
- [x] A test proves at two sizes, with the chip of the width at each place inside the ring, that no line lies on the chip.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r18m-caption-on-arc) · v0.349

## Method

Mechanical: the place of each letter on the middle circle of the band; the move of the block.
Judgement:  the size of the caption, the cut with the whole line, and the split of H5.
Weakest:    a summary longer than about 35 characters shows two times; no person read the caption on the arc.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after P0.147, branch r18m-caption-on-arc. TASK-0364, ADR-0133
  and R18o are free. The caption at the size of the panels was cut at "brack..." at 1280 by 720; at the size of the lines it
  is cut at "75..." there and is whole at 1440 by 900. A rule that kept the lines off the history arc too was caught by no
  injection, because the history arc is out of the reach of the block, so the rule went. The arc on a portrait display (H5)
  changes the sheet and its tests, so it is row R18o.
- 2026-11-14: four injections each failed a named test: no chips as obstacles, letters on a straight line, no whole
  caption under the arc, and no cut. Each file came back byte for byte.
- 2026-11-14: run 43552026024 passed on the work commit. The pictures of the mix test at 1280 by 720 and 1440 by 900 show the
  caption on the arc and the chip of the width free. Closed at v0.349.
