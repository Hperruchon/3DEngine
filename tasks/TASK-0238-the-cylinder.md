---
id: 0238
title: The cylinder is a primitive of plane facets
status: Active
phase: K5a
opened: 2026-11-06
depends-on: [0184, 0185]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0051, 0052, 0054, 0055, 0057, 0058, 0059, 0060, 0076]
writes:
  create:
    - tasks/TASK-0238-the-cylinder.md
    - docs/adr/0076-the-cylinder.md
    - Engine.Core/Commands/CreateCylinderCommand.cs
    - Engine.Core/Commands/CreateCylinderCommandHandler.cs
    - Engine.Tests/Commands/CreateCylinderCommandTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Kernel/KernelSolid.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.App/**
---

# TASK-0238 — The cylinder is a primitive of plane facets

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row K5 gives the cylinder, the cone, the sphere and the torus. The fallback queue of the owner
(2026-11-05) gives its first slice, the cylinder, with the recommended answers. `CreateBox` is the only primitive. An
extrude of a circle already gives a prism of 64 plane facets (ADR-0040), and the owned kernel gives one name to the
facets of a circle (ADR-0058).

## Goal

A command `CreateCylinder` makes a cylinder of 64 plane facets, with three face names in the owned kernel and a datum on
its top or its bottom, and with no change of `Engine.Contracts`.

## Scope (in)

1. ADR-0076, Accepted under the standing order of 2026-11-05.
2. The command and its handler, through `IExtrudeOps`, with the circle of ADR-0040 item 2.
3. The solid of the kernel with the names `+z`, `-z` and `side`.
4. The creators of the feature list, the regeneration, the kernel projection and the datum resolver.
5. A datum on `+z` and `-z`; a refusal for `side`.

## Scope (out)

- The buttons of the desktop and of the phone: a later slice.
- The cone, the sphere and the torus.
- The true surface, and `CYLINDRICAL_SURFACE` in the STEP export.
- An edge or a vertex of a cylinder for `AddSketchReference`.

## Acceptance criteria

- [ ] A size that is not finite and greater than zero gives `E-GEOM-INVALID-PARAM`.
- [ ] `GetFaces` gives the three names, with the areas of the polygon.
- [ ] A datum on `+z` of a moved cylinder has the frame of the moved top, and `side` is refused.
- [ ] A cylinder through a plate cuts a hole whose walls have the name `side`.
- [ ] An injection of each rule fails a test.

## Outcome

Status: Active

## Method

Mechanical: one command and its handler, as `CreateBox`; one case in each of the four creator files.
Judgement:  the names `+z` and `-z` as for a box, and `side` with no item; the loop through `IExtrudeOps`, so the
            contract does not change.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: ADR-0076, the command, the kernel solid, the creators and thirteen tests; an injection of each of five rules fails a test.
