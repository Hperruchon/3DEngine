---
id: 0183
title: The owned kernel subtracts planar solids exactly, and each face of the result keeps its provenance
status: Done
phase: K4a
opened: 2026-11-05
depends-on: [0182]
governed-by: [0057]
writes:
  create:
    - tasks/TASK-0183-planar-subtract-in-the-owned-kernel.md
    - docs/adr/0057-planar-subtract-in-the-owned-kernel.md
    - Engine.Kernel/**
    - Engine.Tests/Kernel/**
  modify:
    - 3DEngine.sln
    - CLAUDE.md
    - Engine.Tests/Engine.Tests.csproj
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - eng/test-floor.txt
    - docs/INDEX.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0183 — The owned kernel subtracts planar solids exactly, and each face of the result keeps its provenance

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase K4 gives planar faces with a boolean, and the provenance of each face inside the kernel. On 2026-11-05 the
owner answered the four questions of K4: the boolean first, a new project, one face for each curve of a profile, and
the references by identity in a later slice. This task is the first slice, K4a. ADR-0057 gives the design.

## Goal

The new project `Engine.Kernel` subtracts one planar solid from another with exact rational numbers. Each face of the
result names the feature, the element and the item that made it. The B-Rep of the result is valid, and its volume and
its bounds agree with Manifold.

## Scope (in)

1. The project `Engine.Kernel`, with no project reference, in the class of the kernel of the dependency gate.
2. Exact rationals, exact vectors and planes, and the predicate `Orient3D`.
3. A box, an extrude of a polygon profile, and a move, each with a tag on each face.
4. The subtract by a binary space partition, with an exact split point.
5. The B-Rep of a result: the split at each T-junction, one face for each region, the loops, the edges and the shells.
6. A comparison with Manifold on random models.

## Scope (out)

- K4b: the engine reads the kernel, and a query gives the faces of a body.
- K4c: the datums and the references by the identity of a face.
- The union, the intersection, and the curved faces (K5).

## Acceptance criteria

- [x] The B-Rep of a box, of an L profile and of a ring has the counts of the formula of Euler and Poincaré.
- [x] Each face of a ring names its feature and its element.
- [x] On 60 random models, the volume and the bounds agree with Manifold, and each B-Rep is valid.
- [x] An injection into each step of the B-Rep makes a test fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k4a-planar-subtract) · v0.185

## Method

Mechanical: exact arithmetic, the counts of Euler and Poincaré, and the comparison with Manifold.
Judgement:  the form of a face tag, and where one face of the input splits into two regions.
Weakest:    the random models are boxes and L extrudes with one or two cuts; a model with more cuts has no test yet.

## Progress

- 2026-11-05: opened after the four answers of the owner on K4.
- 2026-11-05: the kernel, 11 tests and 1 gate fact; ten injections fail, and the two gate controls pass before the
  change.
- 2026-11-05: closed at v0.185.
