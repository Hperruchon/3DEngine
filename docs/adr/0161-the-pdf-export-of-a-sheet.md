---
id: 0161
title: A sheet exports as one PDF file of version 1.4, from the read GetSheet, with arcs as Bézier curves within 0.001 mm
status: Amended
topic: Drawing, export
date: 2026-11-17
supersedes: []
superseded-by: []
amends: []
amended-by: ['0165']
affects:
  - Engine.Core/Export/PdfExport.cs
  - Engine.Api.Http/Endpoints/ExportEndpoint.cs
  - Engine.Api.Http/EngineSurface.cs
  - Engine.Cli/Cli.cs
  - Engine.Cli/Usage.cs
  - Engine.Tests/Export/PdfExportTests.cs
enforced-by: Engine.Tests/Export/PdfExportTests.cs
---

# ADR-0161 — A sheet exports as one PDF file of version 1.4, from the read GetSheet, with arcs as Bézier curves within 0.001 mm

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0118 item 2 gives the order of the export of a sheet: SVG, then DXF, then PDF. Row P10c of the roadmap: "The export of a
sheet as PDF, for the documentation". ADR-0159 gives the SVG file and ADR-0160 the DXF file, from the same read GetSheet, each
with a writer in `Engine.Core/Export`, a route and a command of the command line.

Three facts limit the choice:

- PDF has no arc. A path has straight lines and cubic Bézier curves only.
- `DeterminismCallGateTests` refuses each trigonometric call in `Engine.Core`, so a curve must come from + − × ÷ and sqrt.
- A PDF file usually holds a date of creation, which makes two exports of one sheet differ.

The codebase review of 2026-11-17 (section 5) gives the research. On 2026-11-17 the owner ordered "start the review, then
P10c" and "merge P10c when green". No plan asked questions; the session took the options below, each the smallest one that
keeps the numbers of the read. The owner can change each one.

## Decision

1. **The source.** `PdfExport.Write` reads a live sheet with the same code as GetSheet (`GetSheetQueryHandler.Read`), as the
   SVG and the DXF file do. A sheet that is not live gives `E-SHEET-NOT-FOUND`. The file is presentation: it never enters a
   command, an event, the Document or the log (determinism rule 6).
2. **The file.** PDF of version 1.4 with six objects in this order: the catalog, the page tree, one page, the standard font
   Helvetica in WinAnsiEncoding, the content of the page, and an Info dictionary with the name of the sheet as title and
   "3DEngine" as producer. The file has no date and no identifier, so one sheet gives the same bytes on each platform and at
   each time. The media box is the paper in points, 72 / 25.4 points for each millimetre. The table of the places of the
   objects and the length of the stream are exact, and each byte is ASCII.
3. **The lines.** The content starts with the matrix of 72 / 25.4, so each number of the content is in millimetres of the
   paper with y up. The widths and the dashes are those of the SVG file (ADR-0159 item 3): a visible line of 0.5 mm, a
   hidden line of 0.25 mm with dashes of 3 mm and gaps of 0.75 mm, flat ends and mitred corners. The hidden lines of a view
   come before its visible lines, and a comment gives the identifier of each view and the reason of each body that gives
   no lines.
   ADR-0165 item 3: the centre lines are thin, with the dashes of the SVG file, between the hidden and the visible lines.
4. **The curves.** A straight line is a path from its start to its end. An arc of a circle or of an ellipse, counter-clockwise
   on the paper, is a chain of cubic Bézier curves in the frame of its two half axes, with the minor axis turned to the
   counter-clockwise side. The arc has a stop at each quarter turn from its start, so each part is a quarter turn or less;
   each part is halved at the unit vector (u + v) / |u + v| until 0.1 · a · t⁶ is at most half of 0.001 mm, with a the
   larger half axis and t = tan(θ/4) = h / (1 + sqrt(1 − h²)) for the half chord h of the part, at most 12 halvings. A curve
   from u to v has the control points u + k u⊥ and v − k v⊥ with k = 4/3 · t, through the frame. A start equal to the end is
   the whole curve; an end in the direction of the start, but not equal to it, is a whole turn less the rounding, as the
   line of the read says. The first and the last point of a chain are the ends of the line. A test measures 17 points of
   each curve: each is within 0.001 mm of the curve of the design.
5. **The frame and the title block.** The frame, the title block and its two texts are those of the SVG file (ADR-0159 item
   4), at the same places, as rectangles and texts of Helvetica.
6. **The dimensions.** Each dimension is a text of 3.5 mm, centred at its place on its base line, with the text of the SVG
   file (ADR-0159 item 5). The width of the text comes from the widths of Helvetica in its metrics for the characters of a
   value (the digits, the point, the signs, R and the letters of "lost"); each other character takes the width of a digit.
   ADR-0165 item 4: a diameter has the prefix Ø of WinAnsiEncoding, which holds no ⌀, with its width of 778 thousandths.
7. **The text and the numbers.** A string is in WinAnsiEncoding: a parenthesis and a backslash have a backslash before them,
   a character from U+00A0 to U+00FF is its byte in octal (the middle dot is \267), and each other character outside
   printable ASCII is "?". A number is the shortest text that reads back as the same double, with the culture of no country,
   with its digits in fixed notation when that text has an exponent; a negative zero and a number that is not finite are 0.
8. **The surface.** `GET /export/pdf?sheetId=<id>` gives the file as `application/pdf`, 404 with `E-SHEET-NOT-FOUND` for a
   sheet that is not live, and 400 with `E-API-BAD-REQUEST` with no valid `sheetId`. The command line
   `engine export pdf <document.3de> <file.pdf> --sheet <id> [--folder <path>]` writes the file and prints
   `{ file, skipped, warnings }`; `pdf` with no `--sheet` is a usage error, as `svg` and `dxf` are.
9. **Not in P10c.** An embedded font, a character outside WinAnsiEncoding, a page of more than one sheet, a layer of PDF, a
   dimension line and an arrow (R22d), a button of the desktop, and a sheet of an assembly.

## Consequences

**Good:** each reader of PDF shows and prints the sheet with no font file, and the file is the same on each platform and at
each time. A real reader of PDF opened a sample of a plate with a hole and a turned cylinder in this task, with its
ellipses, its hidden lines and its title block.

**Bad:** a curve is a chain of Bézier curves within 0.001 mm, not an exact arc. A name with a character outside
WinAnsiEncoding shows "?". The centre of a text with a character outside the list of item 6 is near its place, not exact.

## History

- 2026-11-17 · TASK-0401 · The record.
- 2026-11-17 · TASK-0401 · Shipped at v0.385.
- 2026-11-18 · TASK-0405 · Amended by ADR-0165 items 3 and 4: the centre lines and the sign of a diameter (R30g).
