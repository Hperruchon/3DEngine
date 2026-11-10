---
id: 0331
title: The medium and low findings of the review of 2026-11-09 (b) are fixed
status: Done
phase: P0.139
opened: 2026-11-09
depends-on: [0330]
governed-by: [0004, 0007, 0013, 0016, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0092, 0093, 0095, 0097, 0102, 0104, 0105, 0106, 0107, 0110, 0111]
writes:
  create:
    - tasks/TASK-0331-medium-and-low-findings-of-the-review-of-2026-11-09-b.md
    - docs/adr/0111-a-proposal-takes-its-own-taps-and-commands.md
    - 3DEngine.App/AcceptHold.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/HistorySteps.cs
    - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http/Proposals.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Tests/Http/ProposalsEndpointTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/HistoryArcTests.cs
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - tasks/TASK-0332-the-look-of-the-render.md
    - tasks/TASK-0333-the-edges-of-the-body.md
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

- V158. The owner answered Q5 C on 2026-11-10: the shelf is turned by commands. No command turns a body, so R-0057 holds it.
- The panel of the bodies over Move at 360 by 780 (R-0058), which a probe of this task found with no proposal.

## The answers of the owner of 2026-11-10

Questions Q1 to Q5 of the codebase reviews of 2026-11-09 (a and b): Q1 A, Q2 B, Q3 A, Q4 A, Q5 C. This task records them in
`docs/register.md`: R-0031 (Q1), R-0027 (Q2), R-0022 accepted (Q3), R-0030 accepted (Q4) and the new R-0057 (Q5).

## Acceptance criteria

- [x] Each finding fails on an injection before its fix and passes after it, or a reading gives the reason that it cannot.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0139-review-findings) · v0.315

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  ADR-0111 records the rules that change ADR-0092: the known identifier fails the accept with a new code, and the
            window holds the actions of the person while an accept runs, with a list of free actions so a new action is held.
            The sheet of a portrait display moves above the ring only when it fits; else it covers the ring as before.
Weakest:    the hold of V163 and the read of V161 are wired in NativeThreeDEngine, which no test runs; the tests read
            AcceptHold and HistoryCache, and a reading covers the wiring. T180 is a text, with no test.

## Progress
- 2026-11-10: the fixes of the seven findings, ADR-0111, E-API-PROPOSAL-COMMAND-KNOWN, and the answers of the owner
  to Q1 to Q5 in the register (R-0022 and R-0030 accepted, R-0057 and R-0058 new).
- 2026-11-10: run 42772516188 passed. Closed at v0.315.
