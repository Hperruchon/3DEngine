---
id: 0070
title: The STEP export of the planar faces of the owned kernel
status: Accepted
topic: Kernel, persistence, clients
date: 2026-11-06
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Export/StepExport.cs
enforced-by: Engine.Tests/Export/StepExportTests.cs
---

# ADR-0070 — The STEP export of the planar faces of the owned kernel

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap row K6 is "STEP export for the analytic subset". STEP (ISO 10303) is the file that each program of mechanical
design reads, and it keeps the faces, the edges and the vertices of a solid. An STL file (ADR-0048) keeps triangles
only. Since K4b the owned kernel gives the B-Rep of each live body as a projection of the feature list (ADR-0058): exact
rational vertices, planar faces with loops, edges with two coedges, and closed shells. The kernel has planes only;
the cylinder, the cone, the sphere and the torus wait for K5. So the analytic subset of today is the plane and the line.

The owner ordered K6 on 2026-11-05, in the engine lane of the two lanes, with a standing order that answers each
question with the recommended option. This record is Accepted under that standing order; the owner checks it on
2026-11-08.

Three constraints remove the obvious alternatives:

- The charter refuses an exact STEP import (anti-objective 14). The export goes one way, and the engine never reads a
  STEP file.
- A STEP file is presentation, as a tessellation is: a replay never reproduces it, so it must not enter the log
  (anti-objective 5; `CLAUDE.md`, determinism rule 6 gives the same rule for a tessellation).
- A replay gives the same result on each platform (anti-objective 16). An export that gives other bytes on another
  platform makes a file that a person cannot compare, and a test that only one platform passes.

## Decision

1. **The form.** ISO 10303-21 text in ASCII, with a line feed after each line and one instance on each line. The schema
   is AP214, `AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }`. AP214 and AP242 use the same entities of ISO 10303-42 for a
   B-Rep, so a change to AP242 changes the header and the application context only. AP214 is the schema that the most
   readers accept, older readers too, and AP242 gives nothing that a solid of planes needs. The header has no time and
   no version of the program, so the same Document gives the same bytes. A number is a real of ISO 10303-21: the
   shortest text that reads back to the same double (`double.ToString("R")` in the invariant culture), with a point in
   the mantissa. The format of .NET Core 3.0 and later is managed code, so the text does not depend on the platform.
   The unit is the millimetre, the length unit of the Document (ADR-0015 item 7), with one global uncertainty of
   `1.E-07` mm.
2. **The bodies.** Each live body of the Document, visible or hidden, in the order of its handle, gives its B-Rep from
   `KernelProjection.OfRecord` and `Brep.Of`. One PRODUCT, `document`, holds one `ADVANCED_BREP_SHAPE_REPRESENTATION`
   with each solid. A shell with a positive volume is an outer shell, and each outer shell of a body is one solid with
   the identifier of the body as its name, so a cut that splits a body gives two solids of one name. A shell with a
   negative volume is a void. A void belongs to the smallest outer shell that holds its first vertex, by the parity of
   the faces that an exact ray crosses; a ray that meets an edge, a vertex or the plane of a face at the point goes to
   the next of eight fixed directions. A solid with no void is a `MANIFOLD_SOLID_BREP`. A solid with voids is a
   `BREP_WITH_VOIDS`, and each void is an `ORIENTED_CLOSED_SHELL` with `.F.` on a `CLOSED_SHELL` whose faces turn,
   so that the shell points out of the void (ISO 10303-42). Each face is an `ADVANCED_FACE` on a `PLANE`, with a
   `FACE_OUTER_BOUND` and a `FACE_BOUND` for each hole, each one an `EDGE_LOOP` of `ORIENTED_EDGE`. Each edge is one
   `EDGE_CURVE` on a `LINE` from its lower vertex to its higher vertex, and each vertex is one `VERTEX_POINT`. The normal
   of a plane points out of the solid, and its first axis is the first side of the outer loop. An exact vertex leaves
   the kernel with one rounding to the nearest double (ADR-0057 item 2); a direction is the exact vector over its
   largest component, rounded, then over its length. Only `+ - * /` and `sqrt` compute a number (`CLAUDE.md`,
   determinism rule 1). The facets of an arc or a circle of a profile are planar faces of the kernel (ADR-0057 item 4),
   so they are planes in the file until K5.
3. **A refusal.** `StepExport.Write` gives `E-EXPORT-BODY-FAILED`, with the body and the reason, and no file, for the
   first body that it cannot write: a body with a fault, a body that the kernel does not build, a B-Rep that is not
   closed, a shell with no volume, a void that no outer shell holds or that no ray tests, and a coordinate that a
   double does not hold. A silent skip of a body is refused (anti-objective 9). The export needs no capability
   of the backend.
4. **The surface.** `GET /export/step` gives the file as `model/step`, or the envelope with 422 for
   `E-EXPORT-BODY-FAILED` and 500 for each other error. As for `GET /export/stl` (ADR-0048 item 4), the route is outside
   the triad of ADR-0008, and it reads the Document in the serial section through `DocumentSession.ReadGeometry`.
5. **The command line.** `engine export step <document.3de> <file.step>` reads the document file as
   `engine export stl` does (ADR-0048 item 5), writes the STEP file, and prints `{ file, bodies, warnings }` or
   `{ error }`, with the exit code 0, 1 or 2. A write that fails gives `E-EXPORT-WRITE-FAILED`.
6. **Not in K6a and K6b.** The desktop button, the name and the colour of a body in the file, the surfaces of K5, and an
   import of any kind.

## Consequences

**Good:** a person or an agent gets each live body as a solid with faces, edges and vertices that another program of
design can edit. The file comes from the owned kernel, so it is the same bytes on each platform, and a test proves it
by a hash on the three runners of the pipeline.

**Bad:** a curved side of an extrude is a set of planar facets in the file, as in the kernel. A body with a fault gives no file
at all, so one such body stops the export of each other body. No reader of STEP runs in the pipeline: the tests read the file back with a small reader of the repository, which checks the topology and the
numbers but not each rule of AP214. The test of a void costs one exact ray against each face of each outer shell
of the body, for each void.

**Next:** the button of the desktop comes in a later slice of K6, after the Android work on the desktop. The surfaces
of K5 add a `CYLINDRICAL_SURFACE` and the other analytic surfaces to the same writer.

## History

- 2026-11-06 · TASK-0200 · The record, under the standing order of 2026-11-05.
- 2026-11-06 · TASK-0201 · Items 2, 3 and 6: a void is a `BREP_WITH_VOIDS` of the smallest outer shell that holds it,
  under the standing order of 2026-11-05. Until TASK-0201 a void gave `E-EXPORT-BODY-FAILED`.
