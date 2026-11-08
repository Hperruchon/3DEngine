---
id: 0303
title: The filter of a subnormal value, the gate of the host, the stale check, ADR-0079 item 5, the end of an engine and the test of V141 hold their next forms
status: Done
phase: P0.128
opened: 2026-11-08
depends-on: [0302]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0028, 0029, 0033, 0037, 0043, 0057, 0058, 0060, 0061, 0072, 0073, 0075, 0077, 0078, 0079, 0083, 0089]
writes:
  create:
    - tasks/TASK-0303-low-forms-of-the-thirty-second-review.md
  modify:
    - Engine.Kernel/Bsp.cs
    - Engine.Tests/Kernel/SideFilterTests.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.Android/MainActivity.cs
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.ImageTests/PhoneLifecycleTests.cs
    - docs/adr/0079-the-activity-may-start-again.md
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0303 — The filter of a subnormal value, the gate of the host, the stale check, ADR-0079 item 5, the end of an engine and the test of V141 hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-08 (b) found seven low findings: E195, E196, T178, V143, V144, V145 and T179. They come
after TASK-0302.

## Goal

Each of the seven findings is fixed, or its record says why it stays, each with a test, an injection or a reading.

## Scope (in)

1. E195: the filter goes to the exact sign when a value is subnormal, and ADR-0089 item 6 says so.
2. E196: the cache of `GetFaces` belongs to one document, or its comment says why a shared registry cannot happen.
3. T178: the gate of the host reads the last `WarningsAsErrors`.
4. V143 and V144: the stale check and ADR-0079 item 5 name the race and the inferred cause.
5. V145: the end of an engine on a lost device frees each object.
6. T179: the test of V141 says that it reads the text.

## Scope (out)

- The other findings.

## Acceptance criteria

- [x] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0128-low-forms-b) · v0.288

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  V143 and V144 stay open in part: the race and the second cause need the source of SDL or a run of a relaunch;
            ADR-0079 item 5 and the comment of the activity name them. E196 is a comment, because no host shares a
            registry.
Weakest:    V143 and V144 are named, not fixed; V145 is proved by reading only, because a lost device does not come on
            demand.

## Progress
- 2026-11-08: the seven findings. Injections: the plain double in the filter fails the new test of E195; a later
  WarningsAsErrors with no CA1416 in the host fails the gate with both rules of T178. V145 is proved by reading.
- 2026-11-08: run 42450946419 passed. Closed at v0.288.
