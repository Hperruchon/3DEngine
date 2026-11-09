---
id: 0330
title: Accept of the agent arc takes a tap in the default window
status: Active
phase: P0.138
opened: 2026-11-09
depends-on: [0329]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0102, 0104, 0105, 0107]
writes:
  create:
    - tasks/TASK-0330-the-high-finding-of-the-review-of-2026-11-09-b.md
    - docs/adr/0107-the-arc-of-a-proposal-is-in-the-free-room.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - eng/test-floor.txt
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - tasks/TASK-0331-medium-and-low-findings-of-the-review-of-2026-11-09-b.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0330 — Accept of the agent arc takes a tap in the default window

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-09 (b) (`docs/reviews/2026-11-09-b-codebase-review.md`) found V159: in the default window of
1280 by 720, with a live sketch and no selection, the window of the live sketches covers Accept, and a tap on it does
nothing. A high finding goes into a task before the next product phase.

## Goal

At 1280 by 720, 1440 by 900 and 1920 by 1080, with a live sketch and no selection, a tap on Accept and a tap on Reject each
give their action.

## Scope (in)

1. ADR-0107: with no ring of a body, the ring of the arc is in the room right of the panel, and the window of the live
   sketches does not show while a proposal waits.
2. A test at five display sizes, from 900 by 600 to 1920 by 1080, which fails on an injection of the old place.

## Scope (out)

- V160 (a tap on Accept that also uses a ring tool), which TASK-0331 holds; the fix must not move Accept onto a tool of a ring.

## Acceptance criteria

- [ ] The test fails on an injection of the old place and passes after the fix.
- [ ] A picture of the window at 1280 by 720 shows the arc with a live sketch and no selection.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the probe of the review gives the places at the three sizes.
Judgement:  the window of the live sketches waits while a proposal waits, because the room between it and the panel is
            too narrow for the arc at 1280 by 720; the centre of the ring stays at the middle of the room, because the
            arc draws only the top of the ring.
Weakest:    to be written at the close.

## Progress
- 2026-11-10: the fix, ADR-0107 and six tests. Three injections failed their tests, and each file came back byte for byte:
  the old centre of the ring (five tests), the window of the live sketches while a proposal waits (five tests), and the
  radius with no fit to the room (the test at 900 by 600).
- 2026-11-10: a picture of the real window at 1280 by 720, with a sketch and a proposal that a script posted on 127.0.0.1,
  shows the arc right of the panel, Reject and Accept free, and no window of the live sketches.
