---
id: 0112
title: A command turns a body by a quaternion about the origin of the Document
status: Amended
topic: Contracts, geometry
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0012', '0039', '0051', '0058', '0059', '0060']
amended-by: ['0114']
affects:
  - Engine.Contracts/Geometry/Rotation.cs
  - Engine.Contracts/Geometry/ITransformOps.cs
  - Engine.Contracts/Schema/FieldSchema.cs
  - Engine.Core/Commands/RotateCommand.cs
  - Engine.Core/Commands/RotateCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Core/Determinism/CrossPlatformProbe.cs
  - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
  - Engine.Kernel/KernelSolid.cs
  - Engine.Kernel/Polygon.cs
  - Engine.Tests/Commands/RotateCommandTests.cs
  - Engine.Tests/Http/SchemaEndpointGateTests.cs
  - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
enforced-by: Engine.Tests/Commands/RotateCommandTests.cs
---

# ADR-0112 — A command turns a body by a quaternion about the origin of the Document

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner built the entry hall shelf with y up. The window has z up since R16m. On 2026-11-10 the owner answered Q5 C of
the codebase reviews of 2026-11-09: the Document records no up axis, and commands turn the shelf. The commands move a
body (`Translate`, ADR-0012 amendment 1) but do not turn it, so register entry R-0057 asked for a command that turns a
body. The owner chose this work on 2026-11-10, while another session changes the layout of the window. TASK-0335 (phase
K8) holds it.

Determinism rule 2 of `CLAUDE.md` says that a command records a matrix or a quaternion, and not an angle. A turn enters
five places that read a `Translate` today: the backend, the owned kernel (ADR-0058), the rebuild of a chain (ADR-0039),
the frame of a datum on a face (ADR-0051) and the check of a gone face (ADR-0059).

## Options

- **A quaternion (chosen).** Four numbers. Each quaternion that is not zero gives a turn, so the engine needs no
  tolerance to decide if the numbers are a turn. A quaternion of small integers gives exact entries: (1, 1, 0, 0) is a
  quarter turn about x, with no sine. The matrix needs `+ - * /` only.
- **A matrix of nine numbers.** A person can read it, but nine numbers of a client are a turn only within a tolerance.
  A matrix with a small scale or a small shear passes or fails at a threshold, and the threshold is a new decision.
- **An axis and the cosine and the sine of the angle.** Six numbers, as `UnitKinds.Cosine` and `UnitKinds.Sine` give an
  angle. The pair has the length 1 only within a tolerance, and the axis needs a length of 1 too.

## Decision

1. **The command.** `Rotate` has the parameters `bodyId`, `qw`, `qx`, `qy` and `qz`. It makes a new body, a copy of
   `bodyId` turned about the origin of the Document. As for `Translate`:
   - the handle of the new body is the command identifier (ADR-0012 item 4), and the output `bodyId` gives it;
   - the commit consumes the source body (ADR-0021 item 3), and the new body has the kind `Solid` and keeps the name and
     the visibility of the source (ADR-0028 item 5);
   - the events are `body.created` and `body.consumed` (ADR-0021 item 5), with no new event kind;
   - an unknown body gives `E-GEOM-BODY-NOT-FOUND`, a mesh body of an STL file gives `E-MESH-UNSUPPORTED` (ADR-0083
     item 4), a backend with no `ITransformOps` gives `E-GEOM-CAP-MISSING`, and a native failure gives
     `E-GEOM-NATIVE-OP`.
   The command adds no diagnostic code.
2. **The quaternion.** The log holds the four numbers, and no angle. The quaternion need not have the length 1. With
   s = w² + x² + y² + z², each entry of the turn is a polynomial of degree 2 in w, x, y and z divided by s
   (`Rotation.FromQuaternion`). q, −q and 2q give the same turn. A value that is not finite, a quaternion whose s is
   zero or not finite, or an entry that is not finite gives `E-GEOM-INVALID-PARAM`. This check comes before the check of
   the capability. The four parameters have the new unit kind `quaternion`, a component of the quaternion of a turn with
   no unit. A client shows the four as one turn. This amends ADR-0060 item 2.
3. **The turn.** `Engine.Contracts.Geometry.Rotation` holds the nine numbers of the turn: the images of the x, y and z
   axes. Each reader takes the turn of the command from `RotateCommandHandler.Turn`, so the backend, the owned kernel and
   the frame of a datum use the same nine doubles.
4. **The backend.** `ITransformOps` gets `Rotate(result, source, rotation)`, with the contract of `Translate`. This
   amends ADR-0012 item 1. `ManifoldGeometryBackend` calls `manifold_transform` with the nine numbers and a move of zero,
   as the extrude does on the frame of its datum (ADR-0036 item 6).
5. **The owned kernel.** The projection turns the solid of the body (`KernelSolid.Rotate`). The kernel reads the nine
   doubles exactly. A vertex p goes to X·p.x + Y·p.y + Z·p.z. A normal goes by the cofactors (Y × Z, Z × X, X × Y), so
   each plane is exact also when the nine doubles are not exactly a turn. The plane takes its canonical form again. A
   map with a determinant of zero or less is refused, because it flattens the solid or turns it inside out. This amends
   ADR-0058 item 2.
6. **The chain.** A `Rotate` is a step on the chain of consumers of a body, as a `Translate` is:
   - the rebuild of ADR-0039 runs a `Rotate` again when its operand changes;
   - the end of the chain of ADR-0059 item 3 passes a `Rotate`, so the check of a gone face reads the turned solid.
   This amends ADR-0039 and ADR-0059 item 3.
7. **The frame of a datum.** Each `Rotate` in effect on the chain, before the place of the reference, turns the origin
   and the three axes of the carrier about the origin of the Document, with the nine numbers of item 3, in the order of
   the chain. A `Rotate` after the place does not turn it. The turn of a face of a subtrahend (ADR-0059 item 4) changes
   the signs of v and n only, so the order of the two does not change the frame. The points of a reference of an edge
   or a vertex (ADR-0052) turn in the same way. This amends ADR-0051 item 2.
8. **Determinism.** The four numbers enter the log; the nine numbers come from them with `+ - * /` only, in one fixed
   order, so each platform gives the same bits. The kernel reads them exactly, and the frame of a datum uses `+` and
   `*` only. The probe of ADR-0074 holds a turn of a box by (1, 2, 3, 4), so `manifold_transform` multiplies general
   numbers in native code, and a datum on a face after the turn. The pipeline compares the hash of the four platforms,
   and the test sheet of the phone names the new hash.
9. **The pivot.** The turn is about the origin of the Document only. A turn about another point is a `Translate`, a
   `Rotate` and a `Translate`. A later change can add an optional pivot at a low cost: an optional parameter rewrites no
   entry of a log and no saved document (`CLAUDE.md`, "Method").
10. **Out of this record.** No tool of the window turns a body. A client of the surface, the command line or an agent
    sends `Rotate`. A tool of the ring or of the gizmo that turns a body needs its own decision.

## Consequences

- The shelf of the owner can stand with z up: a `Rotate` by (1, 1, 0, 0) turns y to z. R-0057 closes.
- `ITransformOps` has a new member, so each implementation of it gets `Rotate`; four test doubles get it in TASK-0335.
- The chain of a body can hold turns, so a frame of a datum can have general axes after a turn of a box. The extrude on
  a datum with general axes exists since TASK-0166, and the probe holds one (ADR-0051 item 10).
- The determinism hash of the probe changes once, with the new steps.

## History

- 2026-11-10 · TASK-0335 · The record, for register entry R-0057 and the answer Q5 C of the owner.
- 2026-11-10 · TASK-0335 · Accepted by the owner: the quaternion, the turn about the origin, and no pivot.
- 2026-11-10 · TASK-0335 · Shipped at v0.318.
- 2026-11-10 · TASK-0339 · ADR-0114 item 1 amends item 2: a quaternion with s below 2^-969 gives no turn, and Rotate
  refuses it with E-GEOM-QUATERNION-TOO-SMALL (finding E201, the answer A of the owner).
