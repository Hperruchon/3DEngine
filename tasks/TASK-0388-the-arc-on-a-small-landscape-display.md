---
id: 0388
title: On a small landscape display the arc of a proposal is right of the rows on a smaller ring, and each part of a proposal hides the tools under it
status: Done
phase: P0.157
opened: 2026-11-14
depends-on: [0387]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088, 0092, 0093, 0095, 0097, 0101, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0119, 0120, 0122, 0129, 0130, 0131, 0132, 0133, 0134, 0135, 0136, 0139, 0141, 0152]
writes:
  create:
    - tasks/TASK-0388-the-arc-on-a-small-landscape-display.md
    - docs/adr/0152-the-arc-on-a-small-landscape-display.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.ImageTests/ProposalPlaceTests.cs
    - docs/adr/0134-the-arc-of-a-proposal-right-of-the-rows.md
    - docs/adr/0139-a-sketch-leaves-a-flat-view-and-no-tool-hides-under-the-arc.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0388 — On a small landscape display the arc of a proposal is right of the rows on a smaller ring, and each part of a proposal hides the tools under it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0066 (due 2027-05-13): under 1024 pixels wide the arc of a proposal lies on the rows, and its lines on the
tools of the ring. The product rows wait for the owner (R-0067). On 2026-11-14 the owner ordered work alone until Sunday, with
the recommended option for each question.

## Goal

At 800 by 600 and 900 by 600 no row and no tool that shows is under the arc of a proposal or its lines.

## Scope (in)

1. ADR-0152, which amends ADR-0134 item 1 and ADR-0139 item 2.
2. A smaller ring of the arc right of the rows, and the hide of the tools under each part of a proposal.
3. The theory of the place of the arc at each landscape size with no exception, and R-0066 closed by its exit.

## Scope (out)

- A portrait display (ADR-0135).

## Acceptance criteria

- [x] ADR-0152 exists, and ADR-0134 and ADR-0139 show the amendment.
- [x] The theory proves at six landscape sizes that no item of the arc lies on the rows, that no tool that shows lies under a
      rectangle of the proposal or under the band, and that the ring of the arc moves right of the rows under 1025 pixels wide.
- [x] R-0066 is closed with its exit.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0157-small-landscape-arc) · v0.373

## Method

Mechanical: the radius of the smaller ring; the rectangles that hide the tools.
Judgement:  a smaller ring in place of a hidden row; the smallest radius of 90 pixels.
Weakest:    the pictures at two sizes only were seen.

## Progress

- 2026-11-14: opened after P0.156, branch p0157-small-landscape-arc, from the pictures at 800 by 600 and 900 by 600. TASK-0388
  and ADR-0152 are free. A first rule left the icon of "Edit sketch" under the band at 900 by 600, so the box of the band is a
  rectangle of the proposal. An obstacle of the rows for the plate had no case where it mattered, so it went. The first test of
  the band read the covers of the code; the test now builds the band from the ring and the end circles. Three injections each
  failed a named test; each file came back byte for byte. A run after the injections used the last injected build; a rebuild
  gave 720 of 720.
- 2026-11-14: run 43583302921 passed on the work commit. Closed at v0.373.
