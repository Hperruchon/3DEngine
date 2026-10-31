---
id: 0142
title: The open follows the replay, the adds follow the solver, and the comments of the cache are true
status: Done
phase: P0.60
opened: 2026-10-31
depends-on: [0141]
governed-by: [0004, 0007, 0012, 0014, 0015, 0016, 0018, 0019, 0020, 0021, 0024, 0025, 0028, 0029, 0030, 0031, 0033, 0035, 0036, 0037, 0039]
writes:
  create:
    - tasks/TASK-0142-engine-forms-of-the-sixteenth-review.md
  modify:
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/Persistence/EntryCodec.cs
    - Engine.Core/Replay.cs
    - Engine.Core/Commands/AddSketchCircleCommandHandler.cs
    - Engine.Core/Commands/SetSketchConstraintValueCommandHandler.cs
    - Engine.Core/Sketch/SketchConstraints.cs
    - Engine.Core/Geometry/InProcessMeshBackend.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http.Server/ServerEngine.cs
    - 3DEngine/DesktopSurface.cs
    - Engine.Tests/Commands/EngineDebtTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.ImageTests/CurveImageTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - 3DEngine.ImageTests/GizmoActionsTests.cs
    - 3DEngine.ImageTests/GizmoImageTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - 3DEngine.ImageTests/RebuildImageTests.cs
    - 3DEngine.ImageTests/SketchActionsTests.cs
    - 3DEngine.ImageTests/SketchGapUiTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - Engine.Tests/Http/DesktopSurfaceTests.cs
    - Engine.Tests/Http/DocumentEndpointTests.cs
    - Engine.Tests/Http/EngineHostStopTests.cs
    - Engine.Tests/Http/EngineHostSubscriptionTests.cs
    - Engine.Tests/Http/EventPayloadSchemaTests.cs
    - Engine.Tests/Http/HttpBodyNameScenarioTests.cs
    - Engine.Tests/Http/HttpSketchScenarioTests.cs
    - Engine.Tests/Http/StringFieldTests.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0142 — The open follows the replay, the adds follow the solver, and the comments of the cache are true

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (b) found E106 to E112 (low). It comes after R11b (TASK-0134), the next product phase, by `docs/templates.md`, section 7; it starts only when R11b is merged. The standing answer of the owner takes Q1 A
(the owner accepts E25) and Q2 A (the owner accepts E27) of the review.

## Goal

The open of a document file gives the same cancellation, message and reading as the replay, an add refuses a radius
that the solver refuses, and each comment of the cache says what the code does.

## Scope (in)

1. E106: `DocumentFile.Load` throws `OperationCanceledException` for a result Cancelled with a cancelled token.
2. E107: one helper for the clause of an error message, which the replay and the open share.
3. E108: a live add of a circle, a live add of a constraint `radius` and a live change of its value refuse a value
   that is not greater than `SketchSolver.Tolerance`; under `ApplyingEntry` the rule of E103 stays.
4. E109: `EntryCodec` parses with `AllowDuplicateProperties = false`.
5. E110: the comments of the two backends say that an open does not look in the cache, and a replay has none.
6. E111: `EngineHost` reads `backend.Identity`, and its two parameters go. The write set gains the test files that
   build a host, with the reason in the ledger.
7. E112: the comment of `Replay` says that the rule of E103 acts for a replay of plain commands.
8. Register entry R-0036: Q1 A and Q2 A of the review, so the owner accepts E25 and E27 with their reasons.

## Scope (out)

- The findings of the gates (P0.61) and of the desktop (P0.62).

## Acceptance criteria

- [x] A test of each finding fails before the change and passes after it; a comment has no test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p060-engine-forms) · v0.144

## Method

Mechanical: the probes of the review as tests, one injection for each fix, and the compiler for each host of E111.
Judgement:  an entry of a log keeps the rule of 0 for a radius, as E103 did for an arc; `IsJson` keeps the default
            options, so a repeated member in a last line is refused and not cut as a torn line.
Weakest:    E110 and E112 change comments only, and no test holds a comment.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (b).
- 2026-10-31: R11b merged; the task starts. E111 removes two parameters of `EngineHost`, so the 19 test files that
  build a host enter the write set. `IsJson` keeps the default options, so a last line with a repeated member is
  refused and not cut as a torn line. 6 new test cases; 6 injections caught.
- 2026-10-31: closed at v0.144.
