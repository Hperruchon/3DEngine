---
id: 0326
title: An agent proposes commands on an arc, and the person accepts or rejects them
status: Active
phase: R16r
opened: 2026-11-09
depends-on: [0325]
governed-by: [0004, 0007, 0013, 0015, 0016, 0019, 0024, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0102, 0104]
writes:
  create:
    - tasks/TASK-0326-the-agent-arc.md
    - Engine.Api.Http/Proposals.cs
    - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
    - Engine.Tests/Http/ProposalsEndpointTests.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
  modify:
    - docs/adr/0092-an-agent-proposes-and-the-person-accepts.md
    - docs/adr/README.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - docs/diagnostics.md
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine/HostedSession.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
---

# TASK-0326 — An agent proposes commands on an arc, and the person accepts or rejects them

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The third of the four slices that the owner chose on 2026-11-08 after v0.297: the agent arc of the render "mix" (Mix 2).
On the same day the owner answered ADR-0092: Q1 A, Q2 A, Q3 B (a queue) and Q4 A.

## Goal

An agent posts a proposal of commands over HTTP. The desktop shows it on an amber arc above the selected body, and the
person accepts or rejects it. Nothing enters the log before an accept.

## Scope (in)

1. ADR-0092 Accepted, with the decision.
2. The queue `EngineHost.Proposals` and the routes `POST /proposals`, `GET /proposals`, `GET /proposals/{id}` and
   `DELETE /proposals/{id}`, with three new codes and their tests.
3. The arc in the window, the sheet on a portrait display, the accept and the reject, their tests, and the picture of the
   mix at 1440 by 900 with a proposal.

## Scope (out)

- The pending chip of a proposed value (ADR-0082 item 4), a dry run, a group of undo, the author of a command, the glow of
  the edge of the screen, the keys Enter and Escape for a proposal, and the phone.

## Acceptance criteria

- [ ] The tests pass, and the picture at 1440 by 900 shows the arc above the ring with Reject, Accept and the command lines.
- [ ] Each new code is in `docs/diagnostics.md`.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the routes follow `POST /commands`; the place of the arc follows the render "Mix 2".
Judgement:  no route accepts, so an agent cannot accept its own proposal; the limits 8 open, 32 kept, 16 commands and 120
            characters; an accept stops at the first command that the engine does not apply.
Weakest:    to be written at the close.

## Progress
- 2026-11-09: ADR-0092 Accepted with the answers of the owner; the queue, the four routes, three codes, the arc, the
  sheet and 35 tests. An agent made the code in this worktree. It added `SchemaDiagnosticsEndpoint.cs` to the write set,
  because the schema gate lists each code. The review found that the real window showed no proposal: the application
  project does not reference the surface library, and `HostedSession` did not give the queue to the window. This session
  added `IProposalDesk` to `HostedSession` and a test that posts over HTTP to the desktop surface and accepts and rejects
  through the window side; the test failed on an injection of an empty queue. The agent proved the band of the arc, the
  stop at the first rejected command and the absence of a route that accepts by injection. Undo is the shared history
  of the engine, so the window undoes each command of an accept. Local: the gates, a clean build with zero warnings, the
  tests of the solution and the phone project.
