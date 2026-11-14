---
id: 0368
title: On a portrait display the arc of a proposal is at the top, and a sheet of its lines is under it
status: Active
phase: R18o
opened: 2026-11-14
depends-on: [0365, 0366]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0082, 0084, 0091, 0092, 0098, 0099, 0105, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0117, 0119, 0122, 0128, 0129, 0130, 0131, 0133, 0134, 0135]
writes:
  create:
    - tasks/TASK-0368-the-arc-of-a-proposal-on-a-portrait-display.md
    - docs/adr/0135-the-arc-of-a-proposal-on-a-portrait-display.md
    - 3DEngine.ImageTests/ProposalPortraitTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/0111-a-proposal-takes-its-own-taps-and-commands.md
    - docs/adr/0131-the-rows-under-the-part-on-a-portrait-display.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0368 — On a portrait display the arc of a proposal is at the top, and a sheet of its lines is under it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Gap H5 of the comparison at the other sizes of the brief (`docs/proposals/2026-11-14-the-render-at-other-sizes.md`) is roadmap
row R18o: on a portrait display the proposal is a sheet with Reject and Accept over the part, and the render gives the arc
above the part. The codebase review of 2026-11-14 (c) came first, as the gate asks. On 2026-11-14 the owner ordered work
alone until Sunday, with the recommended option for each question.

## Goal

On a portrait display Reject and Accept are on an arc above the part, the part and the chip of the proposed value show, and
each tool of the corner ring is free.

## Scope (in)

1. ADR-0135, which amends ADR-0092 item 7, ADR-0111 item 4 and ADR-0131 item 1.
2. The arc at the top, the sheet of the lines under it, the rows under the sheet, and the chips off the proposal.
3. New tests of the portrait proposal; the tests of the old sheet change to the new place.

## Scope (out)

- The findings V181 to V184 of the review of 2026-11-14 (c) (P0.148).

## Acceptance criteria

- [ ] ADR-0135 exists. ADR-0092, ADR-0111 and ADR-0131 show the amendment.
- [ ] A test proves at 390 by 844 the arc at the top with Reject and Accept that take their taps, and the sheet under it.
- [ ] A test proves at four phone sizes that each tool of the corner ring is free and takes its tap.
- [ ] A test proves that no chip lies on the arc, the sheet or the rows, and that the rows stay under the sheet.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the ring of the arc from the width; the sheet between the arc and the middle; the covers of the chips.
Judgement:  the sheet under the arc, not above the ring; the chips off the proposal on a portrait display only.
Weakest:    Reject and Accept are at the top of a phone, far from the thumb; no person used them on the S23.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after the review of 2026-11-14 (c), branch r18o-portrait-arc.
  TASK-0368 and ADR-0135 are free. A first form kept the sheet above the corner ring; on a phone it had no room while the
  rows showed, so it fell back above the anchors and covered Move. A probe showed about 130 pixels between the middle of a
  phone and the ring, of which the first rows take about 90, so the sheet went under the arc. The picture at 390 by 844 then
  showed the sheet, and then the rows, on the chip of the width, so while a proposal waits the chips keep off the arc, the
  sheet and the rows. The rows no longer read the top of the sheet, so that code went. Seven injections each failed a named
  test; the cover of the rows was caught only by the mix test at 390 by 844, with the dimension lines of the bracket. Each
  file came back byte for byte.
