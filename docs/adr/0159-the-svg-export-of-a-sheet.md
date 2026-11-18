---
id: 0159
title: A sheet exports as one SVG file in millimetres of the paper, from the read GetSheet, with exact arcs and the value of each dimension
status: Amended
topic: Drawing, export
date: 2026-11-16
supersedes: []
superseded-by: []
amends: []
amended-by: ['0164', '0165', '0169']
affects:
  - Engine.Core/Export/SvgExport.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - Engine.Api.Http/Endpoints/ExportEndpoint.cs
  - Engine.Api.Http/EngineSurface.cs
  - Engine.Cli/Cli.cs
  - Engine.Cli/Usage.cs
  - Engine.Tests/Export/SvgExportTests.cs
enforced-by: Engine.Tests/Export/SvgExportTests.cs
---

# ADR-0159 — A sheet exports as one SVG file in millimetres of the paper, from the read GetSheet, with exact arcs and the value of each dimension

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0118 item 2 gives the order of the export of a sheet: SVG first, then DXF, then PDF (Q4 of 2026-11-10). Row P10a of the
roadmap: "The export of a sheet as SVG, from the exact numbers of the views and the values of the design". P10.2 (ADR-0155)
gives the lines of each view on the paper in millimetres, and P10.3 (ADR-0158) gives the value of each dimension or "lost".
The STL export (ADR-0048) and the STEP export (ADR-0070) give the form of an export: a writer in `Engine.Core/Export`, a route
`GET /export/...`, and a command of the command line.

On 2026-11-16 the owner ordered "then start P10a, merge when green". No plan of P10a asked questions; the session took the
options below, each the smallest one that keeps the exact numbers. The owner can change each one.

## Decision

1. **The source.** `SvgExport.Write` reads a live sheet with the same code as GetSheet (`GetSheetQueryHandler.Read`), so the
   file and the read give the same lines and the same values. A sheet that is not live gives `E-SHEET-NOT-FOUND`. The file
   is presentation: it never enters a command, an event, the Document or the log (determinism rule 6).
2. **The geometry.** The view box is the paper, in millimetres, with the width and the height of the attributes in `mm`. The
   y axis of the paper goes up and the y axis of SVG goes down, so each y becomes the height minus y. A straight line is a
   path from its start to its end. An arc of a circle is an SVG arc with its radius; it goes counter-clockwise on the paper,
   so its sweep flag is 0, and its large-arc flag is 1 when the end is on the right of the start, seen from the centre. An
   arc of an ellipse is an SVG arc in the frame of its two unit half axes, with a transform `matrix` that rotates or
   reflects and moves: the transform changes no width and no dash, and the angle of the axes needs no arc tangent. A start
   equal to the end is the whole curve, as two arcs.
3. **The lines (ISO 128-20).** A visible line is a thick line of 0.5 mm. A hidden line is a thin line of 0.25 mm with dashes
   of 3 mm and gaps of 0.75 mm. The hidden lines of a view come before its visible lines, so a visible line covers a hidden
   line. Each view is one group with the identifier of the view. A body that gives no lines has a `desc` with its reason,
   and the writer returns the list of such bodies.
   ADR-0165 item 3: a centre line is a thin line of 0.25 mm with a long dash of 6 mm, a gap of 0.75 mm, a dot of 0.125 mm
   and a gap of 0.75 mm (ISO 128, type 04), between the hidden and the visible lines.
   ADR-0169 item 6: a cut line has the dashes of a centre line, a hatch line is thin and continuous, a thick end is thick,
   an arrow is thin with a filled head, and the texts of a view are centred at their places.
4. **The frame and the title block.** A frame 10 mm inside the edge of the paper, and a title block of 120 by 20 mm in the
   lower right corner of the frame, with the name of the sheet, the paper and its orientation, the projection, the scale
   and the unit. The scale is the scale of the views in lowest terms when they have one scale, or "as shown".
5. **The dimensions.** Each dimension is a text of 3.5 mm, centred at its place: the value of the design, with the prefix
   "R" for a radius (ISO 129-1), or "lost" when its source is not in the design. The file gives no dimension line and no
   arrow: the ends of the measured edge on the paper and the flag "true length" wait for R22d (answer D7 A of 2026-11-15:
   "P10a or R22d").
   ADR-0165 item 4: a diameter has the prefix ⌀.
6. **The numbers.** Each number is the shortest text that reads back as the same double, with the culture of no country,
   and a negative zero is 0. A number that is not finite is 0, and a character that XML 1.0 does not permit is U+FFFD
   (ADR-0164 items 2 and 4). The code uses + − × ÷ and sqrt only, so the file is the same on each platform.
7. **The surface.** `GET /export/svg?sheetId=<id>` gives the file as `image/svg+xml`, 404 with `E-SHEET-NOT-FOUND` for a
   sheet that is not live, and 400 with `E-API-BAD-REQUEST` with no valid `sheetId`. The command line
   `engine export svg <document.3de> <file.svg> --sheet <id> [--folder <path>]` writes the file and prints
   `{ file, skipped, warnings }`; `--sheet` with `stl` or `step`, and `svg` with no `--sheet`, are usage errors.
8. **Not in P10a.** A dimension line, an arrow, a font file, a colour, a layer, a hatch, a button of the desktop, and a sheet
   of an assembly.

## Consequences

**Good:** an agent or a person gets a drawing that a browser shows and a CAD program reads, with the exact numbers of the
read, and P17 gets its picture of a view.

**Bad:** a dimension is a number at a place with no line, so a reader must know which edge it measures. A text has the font
of the viewer. The read of a curved body takes seconds (R-0069), and the export takes the same time.

## History

- 2026-11-16 · TASK-0396 · The record.
- 2026-11-16 · TASK-0396 · Shipped at v0.381.
- 2026-11-18 · TASK-0400 · Amended by ADR-0164 items 2 and 4: a number that is not finite is 0, and a character that XML does not permit is U+FFFD (findings E215 and E217).
- 2026-11-18 · TASK-0405 · Amended by ADR-0165 items 3 and 4: the centre lines and the sign of a diameter (R30g).
- 2026-11-18 · TASK-0411 · Amended by ADR-0169 item 6: the hatch, the cut line, the arrows and the texts of a section (R30h).
