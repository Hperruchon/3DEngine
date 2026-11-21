---
id: 0181
title: The union and the intersection of two bodies, in the form of the subtract
status: Accepted
topic: Kernel, geometry, commands
date: 2026-11-21
supersedes: []
superseded-by: []
amends: ['0012', '0057', '0058']
amended-by: []
affects:
  - Engine.Contracts/Geometry/IBooleanOps.cs
  - Engine.Core/Commands/UnionCommand.cs
  - Engine.Core/Commands/UnionCommandHandler.cs
  - Engine.Core/Commands/IntersectCommand.cs
  - Engine.Core/Commands/IntersectCommandHandler.cs
  - Engine.Core/Commands/BooleanStep.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Core/Sketch/FaceGone.cs
  - Engine.Core/Determinism/CrossPlatformProbe.cs
  - Engine.Kernel/PlanarBoolean.cs
  - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
  - Engine.Geometry.Manifold/Native/ManifoldNative.cs
  - Engine.Tests/Commands/UnionIntersectCommandTests.cs
  - Engine.Tests/Kernel/PlanarUnionIntersectTests.cs
  - Engine.Tests/Http/HttpUnionIntersectScenarioTests.cs
enforced-by: Engine.Tests/Commands/UnionIntersectCommandTests.cs
notes: K16. Each decision that the row, ADR-0126 and ADR-0157 leave open copies Subtract.
---

# ADR-0181 — The union and the intersection of two bodies, in the form of the subtract

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K16 of the roadmap (ADR-0126 item 8, ADR-0157 items 11 and 15) asks for `Union` and `Intersect` "in the form of
Subtract: each consumes two bodies and makes one". The union gives the knob with a collar of K10; the intersection gives
the overlap volume of K11. The kernel boundary of the row is "boolean".

`Subtract` is the only boolean today. Its path is: the record `SubtractCommand` in `Engine.Core/Commands`, its handler
in `HandlerCatalog`, the capability `IBooleanOps` of `Engine.Contracts` with one method, the call of `manifold_difference`
in `ManifoldGeometryBackend`, the exact subtract of the owned kernel (`PlanarBoolean.Subtract`, ADR-0057) for the names of
the faces (ADR-0058), the check of a datum on a face that the cut removes (ADR-0059 item 3), the rebuild of a changed
operand (ADR-0055), and the generic events `body.consumed` and `body.created` (ADR-0021). The codebase review of 2026-11-21
(section 4) says that K16 changes `IBooleanOps` in `Engine.Contracts` and needs an ADR before its code.

The constraint that removes the obvious alternative, a union or an intersection of the meshes in the engine, is the kernel
boundary of the charter: Manifold computes each solid, and the owned kernel gives each face its name. A boolean that the
kernel does not do gives a body with no names, and `GetFaces`, a datum and the measures of K15a then refuse it.

The row, ADR-0126 and ADR-0157 do not decide the names, the parameters, which body keeps its name, the result when the
two bodies share no volume, or a button in the window. This record takes, for each, the option that copies `Subtract`
most closely, by the standing order of the owner of 2026-11-20.

## Decision

1. **Two commands.** `Union` and `Intersect`, each with the parameters `firstBodyId` and `secondBodyId` (guid, required)
   and the output `bodyId`, schema version 1. The records `UnionCommand` and `IntersectCommand` are in
   `Engine.Core/Commands`, as `SubtractCommand` is; no record of `Engine.Contracts` changes. Each is in `HandlerCatalog`,
   so `/commands` of HTTP, `engine apply` and `DocumentSession.Apply` reach it with no new route (ADR-0016).
2. **The steps of `Subtract`.** The handler checks in the same order and gives the same codes: a body that is not live
   gives `E-GEOM-BODY-NOT-FOUND`; a mesh body of an STL file gives `E-MESH-UNSUPPORTED` (ADR-0083 item 4); one body twice
   gives `E-GEOM-INVALID-PARAM`; a backend with no booleans gives `E-GEOM-CAP-MISSING`; a fault of the backend gives
   `E-GEOM-NATIVE-OP`. The new body has the handle of the command (ADR-0012 §4) and the kind `Solid`, it takes the name
   and the visibility of the first body, as a cut takes those of the minuend (ADR-0028 item 5), and the command consumes
   both operands (ADR-0021 item 3). The events are `body.consumed`, `body.consumed` and `body.created`; no event kind is
   new. No diagnostic code is new.
3. **No shared volume.** An intersection of two bodies that only touch, or that are apart, is empty. The command is
   refused with `E-GEOM-NATIVE-OP` ("a degenerate result") and the two bodies stay live, as a cut that leaves nothing is
   refused. The exact kernel decides before the backend runs: Manifold gives a flat solid of no volume, which it does not
   call empty, for two bodies that touch on a face. When the kernel cannot build an operand, the check of the backend
   stays. A union of two bodies apart is one body of two parts, as a cut can leave two parts.
4. **The capability.** `IBooleanOps` gets `Union(result, first, second)` and `Intersect(result, first, second)`, with the
   contract of `Subtract`: both operands stay intact, and a duplicate result, an unknown operand or an empty result
   throws. `ManifoldGeometryBackend` calls `manifold_union` and `manifold_intersection`. The managed backend does not
   implement `IBooleanOps`, as before. This amends ADR-0012 §1.
5. **The owned kernel.** `PlanarBoolean.Union` and `PlanarBoolean.Intersect` clip the polygons of each solid by a binary
   space partition of the other, in the steps of the union and the intersection of csg.js, with the exact planes of
   ADR-0057 item 5. No part turns, and each part keeps the tag of its face. A result with no volume is null. This amends
   ADR-0057 item 9, which left the union and the intersection out.
6. **The names of the faces.** A face of the result keeps the name of the face of either body that made it. A datum on a
   face of an operand that the boolean removes gets `W-DATUM-REFERENCE-FAILED`, as after a `Subtract` (ADR-0059 item 3).
   A `Union` and an `Intersect` are on the chain of a datum with no move and no turn, and the curve of each curved side
   stays. The projection of ADR-0058 item 2 reads both commands; this amends it.
7. **The rebuild.** A `Union` or an `Intersect` with a changed operand runs again in the regeneration, as a `Subtract`
   does (ADR-0055). Each is a step of the feature list with its two operands.
8. **The clients.** No button of the window in this record. The command line (`engine apply Union --param ...`), HTTP and
   an agent use the generic path of item 1. A later row of the window adds the button.
9. **Determinism.** The probe of ADR-0074 holds a union and an intersection of two moved boxes, so the four platforms
   compare the bits of the meshes of `manifold_union` and `manifold_intersection`. The kernel uses exact rationals and
   no transcendental call. The log holds the two identifiers only.

## Alternatives considered

- **One command `Boolean` with a kind.** Refused: `Subtract` is one command for one operation, and a kind would give one
  name two parameter sets.
- **The names `bodyId` and `toolBodyId`, or a list of tool bodies.** Refused: a union and an intersection are symmetric in
  their volume, and `first` and `second` say only which body gives the name. A list needs a new form of parameter, and two
  commands give the same result.
- **The name of the larger body, or no name.** Refused: `Subtract` gives the name of its first operand.
- **An empty body for an empty intersection, or a new code.** Refused: the Document has no empty body, and `Subtract`
  refuses an empty result with `E-GEOM-NATIVE-OP`. The check of K11 reads `GetDistance` of K15a, not this command, to find
  that two parts do not overlap.
- **A refusal of a union of two bodies apart.** Refused: a cut can leave two parts, and the furniture of ADR-0157 item 12
  joins panels that are apart in a part of one module.
- **A button in the window now.** Refused for this record: the row asks for the kernel boundary, and `3DEngine.App` is
  outside the write set of TASK-0433.

## Consequences

**Good:** a client, the command line or an agent makes a knob with a collar, and the overlap volume of two parts, with
one command each. The result has the names of its faces, the measures of K15a and a rebuild, as a cut has.

**Bad:** the exact kernel runs on each boolean, before the backend for the check of item 3, so a union of many faceted curves is slow, as a cut is (ADR-0058,
"Consequences"). An intersection that is empty is a refusal, so a client that wants the overlap volume of two parts that
may not overlap must handle `E-GEOM-NATIVE-OP`. The change of the probe changes the determinism hash of each platform. A rebuild (item 7) runs the backend only, so an
edit that makes two bodies of an intersection touch gives a flat body that the kernel cannot build; `GetFaces` then
refuses it with `E-GEOM-FACES-FAILED`.

**Next:** a button of the window for the two commands, in a row of the window.

## History

- 2026-11-21 · TASK-0433 · The record, with each open decision copied from `Subtract`, by the standing order of the owner
  of 2026-11-20.
