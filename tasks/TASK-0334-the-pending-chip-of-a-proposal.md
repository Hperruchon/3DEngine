---
id: 0334
title: A proposal shows each value that it changes on the chip of that value
status: Active
phase: R16t
opened: 2026-11-10
depends-on: [0330]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0091, 0092, 0102, 0104, 0105, 0106, 0107, 0110]
writes:
  create:
    - tasks/TASK-0334-the-pending-chip-of-a-proposal.md
    - docs/adr/0110-a-proposal-shows-each-value-on-its-chip.md
    - 3DEngine.App/ProposedValues.cs
    - 3DEngine.ImageTests/PendingChipTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine/HostedSession.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - eng/test-floor.txt
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - tasks/TASK-0331-medium-and-low-findings-of-the-review-of-2026-11-09-b.md
    - tasks/TASK-0332-the-look-of-the-render.md
    - tasks/TASK-0333-the-edges-of-the-body.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
---

# TASK-0334 — A proposal shows each value that it changes on the chip of that value

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0092 item 5 kept the pending chip of a value for a later slice, and R16r shipped a command line that shows identifiers
cut to 8 characters. On 2026-11-09 the owner chose the pending chip as the next slice, after the codebase review and the
fix of its high finding (P0.138).

## Goal

A command of an agent that changes a value with a chip shows the change on that chip, "C3  60 → 75 mm", and its line in the
arc names the value.

## Scope (in)

1. ADR-0110: the values, the chip, the lines and no selection.
2. `ProposedValues`: the proposed values of a proposal, with no GPU, and its tests.
3. The chips and the lines in the window, their tests, and the picture of the mix at 1440 by 900 with a proposal that changes
   a value of the selected extrude.

## Scope (out)

- A value of another command, a dry run, and the findings of TASK-0331.

## Acceptance criteria

- [ ] The tests pass, and each new test fails on an injection of the old behaviour.
- [ ] The picture at 1440 by 900 shows a pending chip with "now → proposed" on the edge of the value, and a line that names
      the value.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the chips follow ADR-0082 item 4; the labels follow ADR-0034 item 8.
Judgement:  the two commands with a chip (Q1 A); one chip for one value (Q2 A); a line with no identifier (Q3 A); no change
            of the selection (Q4 A).
Weakest:    to be written at the close.

## Progress
- 2026-11-10: the proposed values, the pending chips, the named lines and 20 tests (19 with no GPU and one picture). An agent
  made them in a worktree; 15 injections failed their tests, and each file came back byte for byte. This session reviewed
  the diff, repeated two injections (the host with no values, a proposed chip with no pending style), and added to ADR-0110
  the step of the host that names the values and the arrow drawn as a line. The picture at 1440 by 900 shows the chip
  "60 → 75 mm" on its edge and the line "Plate C1  60 → 75 mm".
- 2026-11-10: renumbered from TASK-0332 and ADR-0108, because another session put R18a on main (cc51f5a) with those
  numbers. The first branch, r16t-pending-chip (105b111), stays unmerged; this branch takes its change on the new main.
