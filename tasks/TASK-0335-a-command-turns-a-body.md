---
id: 0335
title: A command turns a body by a quaternion about the origin of the Document
status: Done
phase: K8
opened: 2026-11-10
depends-on: [0331]
governed-by: [0001, 0004, 0008, 0012, 0014, 0016, 0018, 0021, 0028, 0031, 0033, 0035, 0036, 0037, 0039, 0051, 0052, 0054, 0055, 0057, 0058, 0059, 0060, 0074, 0076, 0083, 0089, 0112]
writes:
  create:
    - tasks/TASK-0335-a-command-turns-a-body.md
    - docs/adr/0112-a-command-turns-a-body.md
    - Engine.Contracts/Geometry/Rotation.cs
    - Engine.Core/Commands/RotateCommand.cs
    - Engine.Core/Commands/RotateCommandHandler.cs
    - Engine.Tests/Commands/RotateCommandTests.cs
  modify:
    - Engine.Contracts/Geometry/ITransformOps.cs
    - Engine.Contracts/Schema/FieldSchema.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Kernel/KernelSolid.cs
    - Engine.Kernel/Polygon.cs
    - Engine.Tests/Commands/BodyNameAndVisibilityTests.cs
    - Engine.Tests/Commands/OperandConsumptionTests.cs
    - Engine.Tests/Commands/UndoRedoTests.cs
    - Engine.Tests/Features/RegenerationTests.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/0039-chain-of-a-consumed-extrude.md
    - docs/adr/0051-references-and-datum-on-a-face.md
    - docs/adr/0058-the-engine-reads-the-owned-kernel.md
    - docs/adr/0059-references-by-the-identity-of-a-face.md
    - docs/adr/0060-unit-kind-in-the-command-schema.md
    - docs/adr/README.md
    - docs/android-test-sheet.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - 3DEngine.App/**
---

# TASK-0335 — A command turns a body by a quaternion about the origin of the Document

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The write set is a first estimate; the implementer corrects it at the start of the work.

## Context

The owner answered Q5 C on 2026-11-10: the Document records no up axis, and commands turn the entry hall shelf, which the
owner built with y up. No command turns a body, so register entry R-0057 asked for one. On 2026-11-10 the owner chose this
work for this session, while another session changes the layout of the window. The window is out of this task, so the
two sessions change different files.

## Goal

A client, the command line or an agent turns a body with one command, and each part of the engine that reads a
`Translate` reads the turn too.

## Scope (in)

1. The command `Rotate` with a quaternion, its handler and its place in the catalog (ADR-0112 items 1 and 2).
2. `Rotation` and `ITransformOps.Rotate`, and the call of `manifold_transform` (items 3 and 4).
3. The owned kernel, the chain, the rebuild and the frame of a datum (items 5 to 7).
4. The determinism probe of ADR-0074 holds a turn, and the test sheet of the phone names the new hash (item 8).

## Scope (out)

- A tool of the window that turns a body (ADR-0112 item 10). The other session owns the window now.
- A pivot other than the origin (ADR-0112 item 9).
- The turn of the shelf of the owner. The file is the file of the owner, and the repository holds no copy of it.

## Acceptance criteria

- [x] A quarter turn by (1, 1, 0, 0) sets a box with y up upright, and the faces, a datum, a rebuild, an undo and an open
      agree with it, each with a test.
- [x] Each change of behaviour fails on an injection before its fix, or a reading gives the reason that it cannot.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the branch and on `main` after the merge, and the four determinism hashes agree.

## Outcome

Status: Done · commit (the merge of the branch r0057-rotate-body) · v0.317

## Method

Mechanical: the command has the form of `Translate`, and each place that reads a `Translate` reads a `Rotate` too.
Judgement:  ADR-0112 records the quaternion, the turn about the origin, and the normals of the kernel by cofactors.
Weakest:    the kernel refuses a map with a determinant of zero or less, and the canonical form of a turned plane,
            with no test: a quaternion always gives a determinant of 1 within the rounding, and a reading covers both.

## Progress
- 2026-11-10: the command Rotate, ADR-0112 (Proposed), the turn in the kernel, the rebuild and the datums, the probe, and
  10 tests. Nine injections each fail a test. The owner accepts ADR-0112 before the merge.
- 2026-11-10: run 42798565576 passed on the branch, and the four hashes agree (b116685c). The owner accepted ADR-0112.
  Main moved to v0.316 (R17a); its merge into the branch has no conflict.
- 2026-11-10: run 42798565576 passed. Closed at v0.317.
