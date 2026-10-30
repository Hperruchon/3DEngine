---
id: 0115
title: The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says
status: Done
phase: P0.46
opened: 2026-10-30
depends-on: [0111]
governed-by: [0004, 0014, 0016, 0018, 0019, 0036]
writes:
  create:
    - tasks/TASK-0115-engine-debt-of-the-plan.md
    - Engine.Tests/Geometry/ManifoldOwnershipTests.cs
  modify:
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Geometry.Manifold/Native/ManifoldNative.cs
    - Engine.Geometry.Manifold/Native/ManifoldSolidHandle.cs
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
    - Engine.Tests/Hosting/ParameterBinderTests.cs
    - Engine.Tests/Http/CommandsEndpointTests.cs
    - tasks/TASK-0114-forms-of-the-thirteenth-review.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0115 — The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The plan of 2026-10-30 (answer Q4 A) gives this task the open findings E11, E14 and E16 of the reviews, and the
test of the first click of R-0043. E14 runs at each extrude and at each rebuild since K0a and K0b. The plan put this
task after P0.45. R9 waits for the visual check of the owner on its branch, and P0.45 changes the desktop, so under
the answer Q3 A this task comes first: it changes no file of the desktop.

## Goal

The Manifold wrapper frees each buffer with the function of its allocator, the binder refuses a number that is not
finite (E11), and each request envelope refuses a member that it does not name (E16).

## Scope (in)

1. E14: each object of Manifold in a buffer of `Marshal.AllocHGlobal` goes with its destructor in place, and the
   buffer with `Marshal.FreeHGlobal`. The binding holds no `manifold_delete_*` function.
2. E11: the binder refuses a number that is not finite. E16: the envelopes of `POST /commands` and
   `POST /queries` refuse an unknown member with HTTP 400.

## Scope (out)

- A change of the native package.
- R-0043 moves to TASK-0114. Its probe needs a run of the desktop with a log of the samples of a focus click, and its
  fix can change `3DEngine/NativeThreeDEngine.cs`, which the parked branch of R9 changes.

## Acceptance criteria

- [x] A test or an injection proves each fix.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p046-engine-debt) · v0.115

## Method

Mechanical: the destruct functions and Marshal.FreeHGlobal, the finite check of the binder, and the attribute of the
            two envelopes.
Judgement:  the binding drops manifold_delete_* so that no later code can call the C++ delete on a buffer of
            Marshal.AllocHGlobal. R-0043 moves to TASK-0114, because its probe needs the desktop.
Weakest:    no test sees a mismatch of allocators: the test of the binding holds the rule, and a loop of 2,000 solids
            shows no fault of the process on this computer only.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111), from the plan of 2026-10-30.
- 2026-10-30: E14, E11 and E16, with their tests; R-0043 moves to TASK-0114, with the reason in Scope (out).
- 2026-10-30: closed at v0.115.
