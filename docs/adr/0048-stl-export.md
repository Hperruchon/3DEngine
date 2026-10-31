---
id: 0048
title: The STL export of the live bodies
status: Accepted
topic: Persistence, clients
date: 2026-10-31
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Export/**
  - Engine.Api.Http/Endpoints/ExportEndpoint.cs
enforced-by: Engine.Tests/Export/StlExportTests.cs
---

# ADR-0048 — The STL export of the live bodies

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-31 the owner gave the order "an STL export of the live bodies", after V3 and the determinism hash, with the
recommended answer to each question. STL is the file of a printer and of most other programs of design: a list of
triangles. The native backend gives the triangles of each body (`ITessellationOps`, ADR-0018), and since P0.67 these
triangles are the same bits on the three platforms. A tessellation is presentation: it never enters a command, an
event, the Document or the log (`CLAUDE.md`, determinism rule 6). This record is Accepted under the standing answer.

## Decision

1. **The form.** Binary STL, little-endian: a header of 80 ASCII bytes, `3DEngine STL export, binary, millimetres`
   padded with spaces, the count of triangles as a 32-bit integer, then 50 bytes for each triangle: the normal and the
   three vertices as 32-bit floats, and a 16-bit attribute of 0. A vertex is the double of the tessellation as a float.
   The normal is the unit vector of the cross product of two sides, by the right-hand rule, because a tessellation
   winds counter-clockwise from outside (ADR-0018 item 9); a triangle with no area has the normal zero. STL has no
   unit; the coordinates are millimetres, the length unit of the Document (ADR-0015 item 7).
2. **The bodies.** Each live body of the Document, visible or hidden, in the order of its handle, in one file. A
   document with no body gives a file with no triangle. The STL file is not a document file: an open does not read it.
3. **The engine.** `StlExport.Write` in `Engine.Core/Export` gives the bytes, or `E-GEOM-CAP-MISSING` for a backend with
   no tessellation, or `E-GEOM-NATIVE-OP` for a tessellation that fails. `DocumentSession.ReadGeometry` gives the
   Document and its backend in the serial section, as a query has them, so a command cannot change a body during the
   export.
4. **The surface.** `GET /export/stl` gives the file as `model/stl`, or the envelope with 501 for a backend with no
   tessellation and 500 for each other error. The route is outside the triad of ADR-0008, as the routes of the assets
   are (ADR-0044 item 4): the bytes of a file are not the result of a query.
5. **The command line.** `engine export stl <document.3de> <file.stl>` opens the document file, writes the STL file,
   closes the session, and prints `{ file, triangles }` or `{ error }`, with the exit code 0, 1 or 2.
6. **The desktop.** The button "Export STL" of the line of the file writes the STL file in the document folder, with the
   name of the field and the extension `.stl`; a name has the rules of a document file (ADR-0030 item 2).
7. **Not in P8g.** ASCII STL, the colour of a body, an export of the visible bodies only, and another format (STEP is
   K6).

## Consequences

- No change of `Engine.Contracts`, of the log or of the document file.
- `DocumentSession` gains one public read; a later export (STEP, K6) uses it too.

## History

- 2026-10-31 · TASK-0157 · The record, under the standing answer of the owner of 2026-10-31.
