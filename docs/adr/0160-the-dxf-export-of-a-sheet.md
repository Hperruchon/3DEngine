---
id: 0160
title: A sheet exports as one DXF file of release 12, from the read GetSheet, with arcs as bulges and ellipses as arcs within 0.001 mm
status: Accepted
topic: Drawing, export
date: 2026-11-17
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Export/DxfExport.cs
  - Engine.Core/Export/SvgExport.cs
  - Engine.Api.Http/Endpoints/ExportEndpoint.cs
  - Engine.Api.Http/EngineSurface.cs
  - Engine.Cli/Cli.cs
  - Engine.Cli/Usage.cs
  - Engine.Tests/Export/DxfExportTests.cs
enforced-by: Engine.Tests/Export/DxfExportTests.cs
---

# ADR-0160 — A sheet exports as one DXF file of release 12, from the read GetSheet, with arcs as bulges and ellipses as arcs within 0.001 mm

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0118 item 2 gives the order of the export of a sheet: SVG, then DXF, then PDF. Row P10b of the roadmap: "The export of a
sheet as DXF, for the exchange with a CAD program and a laser cutter". ADR-0159 gives the SVG file from the read GetSheet,
and its form: a writer in `Engine.Core/Export`, a route and a command of the command line.

Two facts limit the choice:

- A DXF ARC and a DXF ELLIPSE need an angle. `DeterminismCallGateTests` refuses `Atan2` and each other trigonometric call in
  each project of the log path, `Engine.Core` included. The file must be the same on each platform, as the SVG file is.
- DXF of release 2000 or later needs handles, owners and a dictionary of objects before AutoCAD reads it. No AutoCAD and no
  reader of DXF in Python are on the computer of the owner, so a test cannot show that such a file opens.

On 2026-11-17 the owner ordered "start P10b". No plan asked questions; the session took the options below, each the
smallest one that keeps the numbers of the read. The owner can change each one.

## Decision

1. **The source.** `DxfExport.Write` reads a live sheet with the same code as GetSheet (`GetSheetQueryHandler.Read`), as the
   SVG file does (ADR-0159 item 1). A sheet that is not live gives `E-SHEET-NOT-FOUND`. The file is presentation: it never
   enters a command, an event, the Document or the log (determinism rule 6).
2. **The release and the unit.** The file is DXF of release 12 (`$ACADVER` AC1009), with the sections HEADER, TABLES,
   BLOCKS (empty) and ENTITIES, and no handles. The coordinates are millimetres of the paper with y up, as the read gives
   them. The header gives the extent of the paper, `$INSUNITS` 4 (millimetre) and `$MEASUREMENT` 1 (metric), which a
   reader of release 12 can ignore.
3. **The layers.** FRAME, VISIBLE and TEXT have the colour 7 and the line type CONTINUOUS. HIDDEN has the colour 8 and the
   line type HIDDEN, of dashes of 3 mm and gaps of 0.75 mm (ISO 128-20, as in ADR-0159 item 3). Release 12 has no line
   width, so the layer gives the difference between a visible line and a hidden line. The hidden lines of a view come
   before its visible lines, and a comment of group 999 gives the identifier of each view.
4. **The geometry.** A straight line is a LINE. A whole circle is a CIRCLE. An arc of a circle is a POLYLINE from its start to
   its end, whose vertex carries the bulge, the tangent of a quarter of the angle of the arc, positive counter-clockwise.
   With the half chord h and d = sqrt(r² − h²), the bulge is h / (r + d), so it needs no angle; an arc of a half turn or more
   is two arcs at its middle, so each bulge is at most 1. Release 12 has no ellipse, so an arc of an ellipse or a whole
   ellipse is a POLYLINE of arcs of circles. In the frame of the two half axes, the arc is cut into parts of less than a half
   turn, and each part is halved at the unit vector (a + b) / |a + b| until the arc of a circle through its two ends and its
   middle is within 0.0005 mm of the ellipse at a quarter and three quarters of the part, at most 12 halvings. A test
   measures each arc at 17 points: each point is within 0.001 mm of the ellipse. The first and the last vertex are the
   ends of the line of the read, so the chain meets the lines next to it.
5. **The frame and the title block.** The frame, the title block and its two texts are those of the SVG file (ADR-0159 item
   4), at the same places, as LINE and TEXT entities. The scale text is the same function as in the SVG file.
6. **The dimensions.** Each dimension is a TEXT of 3.5 mm on the layer TEXT, centred at its place on its base line, with the
   text of the SVG file: the value, "R" for a radius, or "lost" (ADR-0159 item 5). There is no dimension line and no arrow
   (R22d).
7. **The text and the numbers.** The file is ASCII, with one line feed after each line. A character outside printable ASCII
   is `\U+XXXX`, as AutoCAD reads it; a character of control is a space, and a pair of surrogates is "?". A number is the
   shortest text that reads back as the same double, with the culture of no country; a negative zero is 0, and a number
   with an exponent has its digits in fixed notation. The code uses + − × ÷ and sqrt only.
8. **The surface.** `GET /export/dxf?sheetId=<id>` gives the file as `image/vnd.dxf`, 404 with `E-SHEET-NOT-FOUND` for a
   sheet that is not live, and 400 with `E-API-BAD-REQUEST` with no valid `sheetId`. The command line
   `engine export dxf <document.3de> <file.dxf> --sheet <id> [--folder <path>]` writes the file and prints
   `{ file, skipped, warnings }`; `dxf` with no `--sheet` is a usage error, as `svg` is.
9. **Not in P10b.** DXF of release 2000 or later, an ELLIPSE or SPLINE entity, a line width, a block of each view, a
   dimension entity, a hatch, a button of the desktop, and a sheet of an assembly.

## Consequences

**Good:** a CAD program or a laser program reads the sheet with its exact lines and circles, and each arc of a circle is
exact. The file needs no angle, so it is the same on each platform, and no gate changes.

**Bad:** an ellipse arrives as a chain of arcs, not as one ellipse, so a CAD program cannot edit it as an ellipse; the chain
is within 0.001 mm of the paper. Release 12 has no line width, so a reader sees the difference between lines only by the
layer and the line type. No CAD program on the computer of the owner opened the file in this task; the test reads it with
its own reader of the pairs.

## History

- 2026-11-17 · TASK-0398 · The record.
