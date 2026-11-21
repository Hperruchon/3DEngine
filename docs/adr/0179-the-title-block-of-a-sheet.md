---
id: 0179
title: A sheet holds the fields of the title block of ISO 7200, SetSheetTitle gives them, and the frame shows the block, the symbol of the projection and the centring marks
status: Accepted
topic: Contracts, drawing
date: 2026-11-21
supersedes: []
superseded-by: []
amends: ['0153', '0155', '0159', '0160', '0161']
amended-by: []
affects:
  - Engine.Contracts/Drawing/SheetRecord.cs
  - Engine.Core/Commands/SetSheetTitleCommand.cs
  - Engine.Core/Commands/SetSheetTitleCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Drawing/TitleBlock.cs
  - Engine.Core/Queries/GetSheetQuery.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - Engine.Core/Export/SvgExport.cs
  - Engine.Core/Export/DxfExport.cs
  - Engine.Core/Export/PdfExport.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - 3DEngine.App/SheetPlacement.cs
  - Engine.Tests/Drawing/SheetTitleTests.cs
enforced-by: Engine.Tests/Drawing/SheetTitleTests.cs
notes: R30i. The answers Q4 A to Q7 A of the codebase review of 2026-11-20, by the standing order of the owner of 2026-11-20.
---

# ADR-0179 — A sheet holds the fields of the title block of ISO 7200, SetSheetTitle gives them, and the frame shows the block, the symbol of the projection and the centring marks

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 15 (D18 A of the owner of 2026-11-18) puts the fields of ISO 7200 in the sheet record, set by one command: the
title, the identification number, the revision, the date of issue, the sheet number, the legal owner, the material, the
creator and a note of the general tolerances. The block shows the symbol of the projection of ISO 5456-2, and the frame gets the
centring marks. Item 17 says that this slice, R30i, writes the exact shape of its change of `Engine.Contracts` in the same
commit as the code. Item 20 puts the note of the general tolerances in the scope of the drawing of a part.

The codebase review of 2026-11-20 (v0.408), sections 5 and 6, examined R30i and gave four questions. Under the standing order
of the owner of 2026-11-20 (the recommended option A of each question), the answers are:

- Q4 A: the block holds the nine fields of ADR-0162 item 15, plus the approval person and the document type, so that each
  field that ISO 7200 makes mandatory has a place.
- Q5 A: one command, SetSheetTitle, gives each field; an absent field is empty; one step of undo.
- Q6 A: the block is 180 mm wide at most and as high as its fields need; an earlier view stays where its log put it.
- Q7 A: no form on the sheet page in R30i: the command, GetSheet, the three files and the drawing of the block on the page.

Until this record the frame and a block of 120 by 20 mm with the name, the paper, the projection as text and the scale were
presentation only (ADR-0159 item 4), and four places drew them from three constants of `SvgExport`. The review found that
the fields, the symbol and the marks would be four more copies (section 5, observed in the code).

A sheet goes into the events `sheet.created` and `sheet.changed` as its whole record, and a Document rebuilds its sheets from
the commands of its log; the document file holds commands only (ADR-0015). So a new field with a default keeps each log and
each file valid (ADR-0165, Context).

The rules of the standards in this record come from search summaries and memory, not from the paid standards (ADR-0162,
"Bad"): the mandatory fields of ISO 7200, its width of 180 mm, the proportions of the symbol of ISO 5456-2 and the centring
marks of ISO 5457. The review marked the fields of ISO 7200 as inferred.

## Decision

1. **The contract (Q4 A; amends ADR-0153 item 1).** `Engine.Contracts/Drawing/SheetRecord.cs` gets the record
   `SheetTitleBlock` with eleven fields, each a `string`: `Title`, `IdentificationNumber`, `Revision`, `IssueDate`,
   `SheetNumber`, `LegalOwner`, `Material`, `Creator`, `GeneralTolerances`, `ApprovalPerson` and `DocumentType`. An empty text
   is an absent field. `SheetTitleBlock.Empty` has each field empty. `SheetRecord` gets the field `SheetTitleBlock? TitleBlock
   = null` at its end: a sheet with no SetSheetTitle has no block. No field is required, no name of a field changes, and no
   event kind is new.
2. **The command SetSheetTitle (Q5 A).** It takes `sheetId` (required) and eleven optional strings with the names of the fields
   in camel case: `title`, `identificationNumber`, `revision`, `issueDate`, `sheetNumber`, `legalOwner`, `material`,
   `creator`, `generalTolerances`, `approvalPerson` and `documentType`. A field that the command does not give is empty. The
   command gives a new record of the sheet with the whole block, also when each field is empty: one step of undo. The output
   is `sheetId`. It refuses, in this order:
   - a sheet that is not live (`E-SHEET-NOT-FOUND`);
   - a field that breaks the rules of the name of a body (ADR-0028 item 3): more than 128 UTF-16 units, a control character,
     or text that is not well-formed UTF-16 (`E-SHEET-TITLE-INVALID`, a new code).
   The command does not check the form of a date: `IssueDate` is the text of the client, and the engine reads no clock, so a
   replay gives the same record on each platform (anti-objective 16). The command lives in `Engine.Core/Commands` and
   `HandlerCatalog` (ADR-0016).
3. **The events and the read (amends ADR-0155 item 5).** `sheet.created` and `sheet.changed` carry the field `titleBlock`: an
   object with the eleven fields in camel case, or null for a sheet with no block. `/schema/events` gives it as an object that
   is not required. GetSheet gives `titleBlock`, an object that is never null: the block of the sheet, or the empty block.
   `/schema/queries` lists it.
4. **The block (Q6 A; amends ADR-0159 item 4).** One function, `TitleBlock.Layout` in `Engine.Core/Drawing`, gives the lines
   and the texts of the frame, the centring marks, the block and the symbol in millimetres of the paper. The block is 180 mm
   wide and 44 mm high, in the lower right corner of the frame. Its left side and its top are thick lines; the lines between its
   rows and its cells are thin. It has four rows; the lowest row is the identification zone of ISO 7200:

   | Row (from the bottom) | Height | Cells, with their width in mm |
   |---|---|---|
   | 1 | 10 | Legal owner 55, Identification number 60, Revision 15, Date of issue 30, Sheet 20 |
   | 2 | 12 | Title 125, Document type 55 |
   | 3 | 10 | Created by 45, Approved by 45, Material 90 |
   | 4 | 12 | General tolerances 90, Scale 22, Unit 18, Paper 15, the symbol of the projection 35 |

   Each cell has its label at its upper left corner, a capital of 1.8 mm, and its value at its lower left corner, 2 mm over
   its lower line, a capital of 3.5 mm; the title has 5 mm. Each text is 1.5 mm from the left line of its cell. A text that is
   wider than its cell less 3 mm gets a smaller size, at which it has the width of that room; its width comes from the widths
   of Helvetica, and a character with no width in the table takes the width of a wide character. The title shows the name of
   the sheet when its field is empty. The scale is the text of ADR-0159 item 4, the unit is "mm", and the paper is its name.
   The symbol cell has no label. The text of the projection and of the orientation in the old block go. The band of the block
   is the band of ADR-0167 item 3, so the area of a new view is above 54 mm from the lower edge of the paper; a view that a log
   put in the band stays where it is, and the block can cover it.
5. **The centring marks (ISO 5457).** A thick line at the middle of each side of the paper, from the edge of the paper to 5 mm
   past the frame. The mark of the lower side stops at the frame when the block holds the middle of the paper, as on A4 and on
   A3 in portrait.
6. **The symbol of the projection (ISO 5456-2).** With h = 3.5 mm, at the middle of its cell: a cone seen from the side, a
   trapezium 2h long whose left side is h high and whose right side is 2h high, and the cone seen from its left, two circles
   with one centre of the diameters 2h and h, h apart from the trapezium. In first angle the circles are on the right of the
   trapezium; in third angle on its left (`ThirdAngle` of the record). The outlines are thick. The axis goes through the two
   views and 1.5 mm past them, and a vertical line goes through the centre of the circles and 1.5 mm past them; both are centre
   lines. The code uses + − × ÷ only.
7. **The files and the page (amends ADR-0159 item 4, ADR-0160 item 5 and ADR-0161 item 5).** Each draws the list of item 4,
   before the views:
   - SVG: one path for each line in the group `frame` (thick 0.5 mm, thin 0.25 mm, a centre line with the dashes of ADR-0165
     item 3), and one text for each text in the group `title`, at its left end, with the size of the font of its capital
     letters (finding E224).
   - DXF: each line on the layer FRAME, a circle as a CIRCLE, and a centre line of the symbol as a LINE with its own line type
     CENTER (group 6); each text as a TEXT on the layer TEXT with the height of its capital letters and no justification.
     Release 12 has no width of a line, so the thick and the thin lines differ by their layer only in the views, and not in
     the block.
   - PDF: each line with its width and its dashes, and each text in Helvetica with the size of the font of its capital
     letters.
   - The sheet page (ADR-0168 item 1, whose `affects` field holds the page): each thick line of 2 pixels, each thin line of 1 pixel, each centre line of 1 pixel in long dashes, and
     each text inside the box of the block.
8. **Replay and the saved documents.** The record is a function of the log: the commands of a log before this record give the
   same sheets with `TitleBlock` null, and a document file before this record opens with no change (ADR-0015). The block, its
   lines and its texts are presentation: no command, event or Document holds them (determinism rule 6).
9. **Not in this record.** A form of the fields on the sheet page (Q7 A; a later slice), a check of the form of a date, a field
   of the count of sheets or of the language, a second title, a revision table, a text that wraps on two lines, and a block of
   another width.

## Consequences

**Good:** a drawing has the fields that ISO 7200 makes mandatory, a person or an agent sets them in one step of undo, and the
three files and the page draw one list, so they cannot disagree. Each log and each document file stays valid.

**Bad:** the band of the block is 44 mm and not 20 mm, so a new view goes 12 mm higher than before, and the block can cover a
view that an earlier log put in the band. A long text gets a small size and not a second line. The widths of Helvetica are an
estimate for the SVG file and the page, which use other fonts. The proportions of the symbol and the centring marks come from
memory of the standards, not from the paid texts. Each event of a sheet carries one more field.

**Next:** a form of the fields on the sheet page (a slice after R30i, Q7 A).

## Alternatives considered

The answers Q4 to Q7 do not decide these points. The session took the smallest option, and the owner can change each one:

- **The record.** One nested record `SheetTitleBlock`, null by default, and not eleven fields of `SheetRecord`: one default
  keeps each log valid, and the command replaces one value.
- **The rules of a field.** The rules of the name of a body (128 UTF-16 units) and not a free length or a length of each field.
- **An empty block.** SetSheetTitle with each field empty keeps an empty block and does not give back null, so the event
  says that a person set the block.
- **The title.** The title shows the name of the sheet when its field is empty, so a sheet before this record keeps its name
  in the block.
- **The date.** Free text, with no check of ISO 8601.
- **The code.** A new code `E-SHEET-TITLE-INVALID` and not a new case of `E-SHEET-INVALID`, so the meaning of each code stays.
- **The size and the cells.** 44 mm in four rows, and the cells of item 4. A long text gets a smaller size, and is not cut and
  does not wrap.
- **The centring marks.** 5 mm past the frame, and the lower mark stops at the frame where the block holds the middle.
- **DXF.** All the lines of the block on the layer FRAME, and no new layer.
- **The symbol.** h = 3.5 mm and a trapezium 2h long, from memory of ISO 5456-2.
- **The area of the views.** A band across the paper with the height of the block, and not an area that leaves out the box of
  the block only.

## History

- 2026-11-21 · TASK-0428 · The record, for row R30i and ADR-0162 items 15 and 17, with the answers Q4 A to Q7 A.
