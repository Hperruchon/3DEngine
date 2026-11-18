---
id: 0169
title: A section of a sheet is a view with a cut plane, a parent and a letter, and the read gives its hatch, its cut line and its label
status: Accepted
topic: Contracts, drawing
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0153', '0155', '0159', '0160', '0161', '0165']
amended-by: []
affects:
  - Engine.Contracts/Drawing/SheetRecord.cs
  - Engine.Core/Commands/AddSheetSectionCommand.cs
  - Engine.Core/Commands/AddSheetSectionCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Core/Drawing/SheetLayout.cs
  - Engine.Core/Drawing/SectionCut.cs
  - Engine.Core/Queries/GetSheetQuery.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - Engine.Core/Export/SvgExport.cs
  - Engine.Core/Export/DxfExport.cs
  - Engine.Core/Export/PdfExport.cs
  - Engine.Tests/Drawing/SheetSectionTests.cs
enforced-by: Engine.Tests/Drawing/SheetSectionTests.cs
---

# ADR-0169 — A section of a sheet is a view with a cut plane, a parent and a letter, and the read gives its hatch, its cut line and its label

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 13 records the answer D16 A of the owner of 2026-11-18: a section of a sheet comes now, as row R30h, before
R26. A section has a cut line on a view, a cut plane along the right or the up of its parent, a letter, the hatch of the cut
faces and the label "A–A". Item 11 says that a section shows no hidden lines by default. Item 17 says that this slice writes
the exact shape of its change of `Engine.Contracts`. On 2026-11-18 the owner ordered "start R30h".

"Section by a plane" is one of the six operations of the closed feature boundary of the charter. Until this record no code
gave it. The owned kernel has the subtraction of two closed solids (ADR-0057 item 5), so a cut is a subtraction of a closed
solid that holds the half space on the side of the eye.

A view of a sheet goes into the events `sheet.created` and `sheet.changed` as its whole record (ADR-0165, Context). A new
field with a default keeps each log valid.

## Decision

1. **The contract (D16 A; amends ADR-0153 item 4).** `Engine.Contracts/Drawing/SheetRecord.cs` gets the record
   `SheetSection(Guid Parent, IReadOnlyList<double> Point, string Letter)`, and `SheetView` gets the field
   `SheetSection? Section = null` at its end. A view with a section is a section view: its cut plane goes through `Point`
   (x, y and z of the design, in millimetres), and the normal of the plane is the direction toward the eye of the view. The
   cut removes the material on the side of the eye. `Parent` is the view that shows the cut line, and `Letter` is one
   capital letter from A to Z. Each view before this record has no section. The events carry the field as `section`. No
   field is required, no name of a field changes, and no event kind is new.
2. **The command AddSheetSection (D16 A).** It takes `sheetId`, `viewId` (the parent), `look`, `pointX`, `pointY`, `pointZ`,
   `x`, `y` and an optional `letter`. `look` is the direction of sight on the parent, the direction of the arrows: "left",
   "right", "up" or "down". With the frame (R, U, E) of the parent and its exact length L, the frame of the section is:

   | look | Right | Up | Toward the eye |
   |---|---|---|---|
   | left | (U × R) / L | U | R |
   | right | (R × U) / L | U | −R |
   | down | R | (U × R) / L | U |
   | up | R | (R × U) / L | −U |

   So a section of Front with look "left" has the frame of Right, and one with look "down" has the frame of Top. The section
   takes the scale of the parent, `HiddenLines` false and `CentreLines` true (ADR-0162 item 11). With no letter, the section
   takes the first letter from A to Z that no section of the sheet has, without I, O and Q, which a reader can take for a
   number. The command gives a new record of the sheet with the section after the views: one step of undo. It refuses, in
   this order:
   - a sheet that is not live (`E-SHEET-NOT-FOUND`);
   - an identifier that the log holds (`E-SHEET-ID-IN-USE`), as AddSheetView;
   - a parent that the sheet does not have (`E-SHEET-VIEW-NOT-FOUND`);
   - a look that is not one of the four, a point that is not three finite numbers, a parent whose length is not a rational
     number, a frame whose numbers a double does not hold exactly, a letter that is not one capital letter or that another
     section of the sheet has, a sheet with no free letter, and a place that is not on the paper (`E-SHEET-VIEW-INVALID`).
   The command lives in `Engine.Core/Commands` (ADR-0016). A section can be the parent of a section. RemoveSheetView of a
   parent keeps its sections; such a section keeps its cut and its label, and no view shows its cut line.
3. **The cut (D16 A; amends ADR-0155 item 5).** For a section view the read subtracts from each solid one closed box of the
   owned kernel: the points P + a R' + b U' + c E' with |a| ≤ A, |b| ≤ A and 0 ≤ c ≤ A, where P is the point, (R', U', E')
   the frame of the section, and A is 1 plus the largest of |a|, |b| and |c| over the vertices of the solid. Each face of
   the box has the tag (the identifier of the view, "cut"). Each number is exact. The faces of the result with that tag are
   the cut faces; they look toward the eye. A body that the cut removes fully gives no lines and goes into the skipped bodies
   with its reason. A body that the plane does not cross keeps its lines. The edges of a cut face are lines of the kind
   "edge", and the circles of ADR-0143 apply to them. An edge between the side of a cylinder or a cone and a face of another
   tag along its axis, as the edge of a cut face, gives the axis of the side as an outline does (amends ADR-0165 item 3).
   So a section through the axis of a hole shows the axis, also when the cut goes through the corners of the facets and
   the side has no outline.
4. **The hatch (D16 A; ADR-0162 item 20 puts it in the scope of ADR-0118 item 7).** GetSheet gives the hatch of the cut faces of each body as lines of the kind
   "hatch": thin, continuous lines on the paper, 2.5 mm apart, at 45° (ISO 128-50). The first body with a cut face has lines
   that go up to the right, the next one lines that go down to the right, and so on, so two parts that touch have different
   hatches. A line is x − y = n × 2.5 × √2 or x + y = n × 2.5 × √2 for a whole number n, from the lower left corner of the
   paper. The loops of the cut faces of one body clip each line by the rule of even and odd. A loop is the polygon of the
   kernel, so a hatch line ends on the chord of a facet and not on the arc; the difference is less than the radius times
   (1 − cos(π/n)) for a circle of n facets. The box and the centre of a view leave the hatch out.
5. **The cut line and the label (D16 A).** Each view whose identifier is the parent of a section gives, for that section, its
   cut line on the paper: the line of the plane, across the box of the lines of the parent and 5 mm past each side of it.
   The whole line is of the kind "cut", thin with a long dash and a dot, as a centre line. The outer 5 mm at each end are of
   the kind "cut-end", thick and continuous. From the middle of each thick end, a line of the kind "arrow" of 7 mm goes in the
   direction of sight; its end has the head. Each view gets a list of texts, `SheetText(X, Y, Text, Size)`, with X at the
   middle of the text and Y on its base line, in millimetres of the paper. The letter of a section is 4 mm past the head of
   each arrow, and the label of a section is "A-A" for the letter A, 10 mm under the box of its lines. Each text has the size
   5 mm (ISO 3098). The label uses the hyphen of ASCII, so the three files print the same text. A view with no line has no
   cut line.
6. **The files (amends ADR-0159 item 3, ADR-0160 item 3 and ADR-0161 item 3).** The order is: the hidden lines, the centre
   lines and the cut lines, the hatch, the visible lines and the thick ends, then the arrows and the texts. A hatch line is
   thin, 0.25 mm, and continuous. A cut line is thin with the dashes of a centre line. A thick end is thick, 0.5 mm. An arrow
   is a thin line with a filled head of 3 mm at its end, 1.6 mm wide at its base. A text is centred on its place. The DXF file
   puts the hatch on a layer HATCH of the colour 8 and the line type CONTINUOUS, the cut lines on a layer CUT of the colour 7
   and the line type CENTER, the thick ends on the layer VISIBLE, and the arrows (a LINE and a SOLID) and the texts on the
   layer TEXT. The PDF file holds the widths of Helvetica for the capital letters.
7. **The record of a read.** GetSheet gives each view its section as the field `section` of `SheetViewInfo`, null for a view
   that is not a section, and its texts as the field `texts`.
8. **Not in this record.** A cut line with steps, a half section, a detail view, a rib that is not hatched, and the action
   "Section" of the sheet page. A later record of R30h gives the action of the page.

## Consequences

**Good:** a part with a hole is clear on a drawing, and the hatch, the cut line and the label follow ISO 128-40, 128-44 and
128-50. The cut uses the subtraction of the owned kernel, so the section of R26 in the window can use the same cut.

**Bad:** each read of a section subtracts one box from each solid, so a section costs more than a view. A hatch line ends on
the chord of a facet, not on the arc.

**Neutral:** a log before this record gives the same sheets. A section is a view, so MoveSheetView, ScaleSheetView,
SetSheetViewLines and RemoveSheetView apply to it.

## Rejected alternatives

- **A section that cuts in the read of the parent only, with no view of its own.** The owner asked for a view with a label.
- **The cut plane as a normal and an offset.** A point of the design is what a person picks on the parent, and the parent
  gives the normal, so the record holds no second direction that can disagree.
- **A hatch that ends on the arc.** It needs the curves in the clip; the difference is smaller than the width of a line for
  each circle of the kernel at a usual scale.

## History

- 2026-11-18 · TASK-0411 · The record, for row R30h and ADR-0162 items 11, 13 and 17.
