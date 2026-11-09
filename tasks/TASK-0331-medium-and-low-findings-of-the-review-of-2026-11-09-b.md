---
id: 0331
title: The medium and low findings of the review of 2026-11-09 (b) are fixed
status: Ready
phase: P0.139
opened: 2026-11-09
depends-on: [0330]
governed-by: [0007, 0016, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0092, 0093, 0095, 0097, 0102, 0104, 0105, 0106, 0107]
writes:
  create:
    - tasks/TASK-0331-medium-and-low-findings-of-the-review-of-2026-11-09-b.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Tests/Http/ProposalsEndpointTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/HistoryArcTests.cs
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0331 — The medium and low findings of the review of 2026-11-09 (b) are fixed

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The write set is a first estimate; the implementer corrects it at the start of the work.

## Context

The codebase review of 2026-11-09 (b) (`docs/reviews/2026-11-09-b-codebase-review.md`) found four medium and three low
findings. A medium or low finding goes into a task after the next product phase, the pending chip of a proposal.

## Goal

V160, E198, V161, V162, E199, V163 and T180 are fixed, each with a test or a reading.

## Scope (in)

1. V160: a tap on a target of a proposal does not also use the ring under it.
2. E198: a command identifier that repeats in a proposal is refused at the post, and a cached result does not count as
   applied by the proposal.
3. V161: the history arc runs no query with no selection, and the frame does not wait on the session for it.
4. V162: the sheet of a portrait display leaves the tools of the ring free, or ADR-0092 item 7 says that it covers them.
5. E199: the refusal of a withdraw names the state of the proposal.
6. V163: the actions that change the document wait while an accept runs.
7. T180: the gate of the vision library names the library with no version.

## Scope (out)

- V158, until the owner answers Q5 of the review of 2026-11-09.

## Acceptance criteria

- [ ] Each finding fails on an injection before its fix and passes after it, or a reading gives the reason that it cannot.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  to be written.
Weakest:    to be written at the close.

## Progress
