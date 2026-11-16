# Plan of P10.3 — the dimensions of a sheet

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-15. Prepared by a side session. TASK-0395 copied it to the repository on 2026-11-16.
- Answered on 2026-11-15, in the chat: option A for Q1 to Q7 (D1 to D7 of the answer sheet). The ADR of P10.3 records them.
- Roadmap row: P10.3 (`docs/roadmap.md` on `origin/p10-1-sheet-contract`, line 637): "PlaceSheetDimension and
  RemoveSheetDimension, the value from the design at each read, and a lost dimension (ADR-0153 item 5). After P10.2."
- Sources that I read: ADR-0153; on `origin/p10-1-sheet-contract` (commit 5136d05) the files `SheetRecord.cs`, the six
  sheet command files, `SheetResults.cs`, the diff of `CommandBus.cs`, `Document.cs`, `ICommandHandler.cs`,
  `DiagnosticCodes.cs`, `HandlerCatalog.cs`, `StepHistory.cs`, `SchemaEventsEndpoint.cs`, `SchemaDiagnosticsEndpoint.cs`,
  `DocumentFileTests.cs`, the first 80 lines of `SheetTests.cs`, TASK-0389; ADR-0146, ADR-0148, ADR-0149;
  `3DEngine.App/EdgeValues.cs` (lines 1 to 175); `BodyActions.DistanceOf`; `FeatureList.cs` (lines 1 to 130);
  `GetSketchQuery.cs`; `SketchRecord.cs`; the P10.2 plan `prep-p10-2.md` in the scratchpad of the main session.
- Each statement has its source. Section 10 lists each statement that I did not verify.

## Note of the copy (TASK-0395, 2026-11-16)

The text below is the plan as the side session wrote it at v0.375. These statements changed before the copy:

- P10.2 is on `main` at v0.377 (TASK-0393, ADR-0155), with the names that section 1 uses.
- The task of P10.3 is TASK-0395, and its ADR is ADR-0158. Section 6 uses older numbers.
- Test 3 of section 7 cannot use an undo of the Extrude or of AddSketchConstraint: undo is linear, so an undo of a source
  first removes each later dimension. The constraint becomes lost by DeleteSketchItem. No command removes an extrude, so a
  test gives the read a Document with a source that is not in it.
- Test 4 uses a Translate in place of a Subtract to consume the body of the extrude.
- The read has a test of the HTTP surface (`HttpSheetScenarioTests`): the JSON name of the identifier of a view and of a
  dimension in the event `sheet.changed` is `id`, and in the read it is `dimensionId`.
- Section 10: a constraint `distance` or `radius` holds exactly one value (`SketchConstraints.Refusal`).

## 1 The position

P10.3 comes after P10.2. P10.2 is a plan only: no code of P10.2 exists on a branch that I read. This plan uses the names of
the P10.2 plan with its recommended options: `GetSheetQuery`, the result `SheetInfo` with a list `dimensions` that is
"empty until P10.3", the code `E-SHEET-VIEW-NOT-FOUND`, the rule "a place is on the paper" (P10.2 Q4 A), and "RemoveSheetView
removes the dimensions of that view" (P10.2 Q6 A). If the owner takes another option in P10.2, sections 3, 4 and 7 change.

What exists for P10.3 (verified on `origin/p10-1-sheet-contract`):

- `SheetDimension(Guid Id, Guid ViewId, Guid Source, string SourceKind, double X, double Y)` —
  `Engine.Contracts/Drawing/SheetRecord.cs`, the last record. Its comment gives `SourceKind` "constraint" (a constraint of a
  sketch) or "distance" (an extrude, for its distance). X and Y are millimetres on the paper.
- `SheetRecord.Dimensions` holds them "in the order of their commands" (comment of `SheetRecord`).
- The bus needs no change. A dimension command returns `changedSheets: [sheet with { Dimensions = ... }]`, as
  `RenameSheetCommandHandler` returns `sheet with { Name = ... }`. `RefuseWrongSheetLists`, the step of undo and the event
  `sheet.changed` already carry a changed record (diff of `CommandBus.cs`).
- The event `sheet.changed` carries `["dimensions"] = sheet.Dimensions`, the list of records (`SheetFields` in
  `CommandBus.cs`). The record holds no number of the design, so the event holds none.
- A constraint takes the identifier of its command: `new SketchConstraint(add.CommandId, ...)` in
  `AddSketchConstraintCommandHandler.cs:114`. `SetSketchConstraintValue` gives a new value with the same identifier
  (`SetSketchConstraintValueCommandHandler.cs:81-82`). So a constraint identifier is unique in the Document and stays the
  same when its value changes.
- `SketchConstraint(Guid Id, string Kind, IReadOnlyList<Guid> Items, IReadOnlyList<double> Values)`; "No value is an
  angle: the kind "angle" holds a cosine and a sine" (`Engine.Contracts/Sketch/SketchRecord.cs`).
- The constraint kinds with a length are `distance` and `radius`; the kind `angle` has the units Cosine and Sine
  (`AddSketchConstraintCommandHandler.cs:29-38`).
- `FeatureList.DistanceInEffect(extrudeId)` gives "the distance in effect of the Extrude in effect with the identifier, or
  null". `FeatureList.Extrude` gives the Extrude in effect, "live or consumed" (`Engine.Core/Features/FeatureList.cs:85-93`).
  It reads the last `SetExtrudeDistance` in effect.

How R22 gets the values (the model for the read of P10.3):

- `EdgeValues.Of` (in `3DEngine.App`) reads `constraint.Values[0]` of each constraint of kind `distance` or `radius` from
  the result of `GetSketch`, and the distance of the extrude from `BodyActions.DistanceOf` (`EdgeValues.cs:120-147`,
  `BodyActions.cs:256`). The value is the value of the design, not a length measured on the geometry.
- R22a to R22c are presentation in the window and change no contract (ADR-0146 item 3, ADR-0148 item 4, ADR-0149 item 5).
  ADR-0149 item 4: "The lost value waits for P10."
- ADR-0146, "Bad": "a value whose edge is at a slant to each flat view shows in no flat view ... A drawing of the sheet (P10)
  needs its own rule for such a value." Q4 answers this for P10.3.

## 2 The goal

A sheet holds dimensions. A command places a dimension of a design value in a view of the sheet, and a command removes it.
Each is one step of undo. A read of the sheet gives the number of each dimension from the design at the time of the read. A
dimension whose source left the design is "lost": it keeps its record, and the read says "lost".

## 3 The commands

The identifier of a new dimension is the identifier of its command, as for a sheet (ADR-0153 item 1) and a view (P10.2
plan, section 3).

### 3.1 PlaceSheetDimension

| Parameter | Type | Unit | Required | Content |
|---|---|---|---|---|
| `sheetId` | guid | — | yes | The live sheet. |
| `viewId` | guid | — | yes | A view of the sheet. |
| `source` | guid | — | yes | The identifier of a constraint, or of an extrude. |
| `sourceKind` | string | — | yes | `"constraint"` or `"distance"`. |
| `x`, `y` | number | Length | yes | The place of the line on the paper, in millimetres from its lower left corner. |
| `dimensionId` | guid | — | no | Q2 A: a dimension of the sheet to place again (a new view, source or place). |

Output: `dimensionId` (guid). The handler checks in this order. The first failure gives its code, and the sheet does not
change.

1. The sheet is live. Else `E-SHEET-NOT-FOUND` (existing code, new raise site).
2. With no `dimensionId`: the log does not hold the identifier of the command. Else `E-SHEET-ID-IN-USE` (existing code, new
   raise site; `SheetResults.InUse`). With a `dimensionId`: the sheet holds that dimension. Else
   `E-SHEET-DIMENSION-NOT-FOUND` (new).
3. The sheet holds the view. Else `E-SHEET-VIEW-NOT-FOUND` (code of P10.2, new raise site).
4. `sourceKind` is `"constraint"` or `"distance"`. Else `E-SHEET-DIMENSION-INVALID` (new).
5. The source is in the design now:
   - `"constraint"`: one live sketch of `document.Sketches` holds a constraint with the identifier. Else
     `E-SHEET-SOURCE-NOT-FOUND` (new). Its kind is `distance` or `radius` (Q1 A). Else `E-SHEET-DIMENSION-INVALID`.
   - `"distance"`: `FeatureList.Of(document).DistanceInEffect(source)` is not null. Else `E-SHEET-SOURCE-NOT-FOUND`.
6. `x` and `y` are finite and on the paper: 0 ≤ x ≤ width and 0 ≤ y ≤ height, with the orientation of the sheet (the rule
   of a view, P10.2 Q4 A). Else `E-SHEET-DIMENSION-INVALID`.
7. No other dimension of the sheet has the same source (Q3 A). Else `E-SHEET-DIMENSION-INVALID`. With a `dimensionId`, the
   dimension itself does not count.

The new record: with no `dimensionId`, the list of dimensions with the new dimension at its end; with a `dimensionId`, the
same list with that dimension replaced at its place (same identifier, same index).

The command does not test that the view shows the length of the source true (Q4 A).

### 3.2 RemoveSheetDimension

| Parameter | Type | Required |
|---|---|---|
| `sheetId` | guid | yes |
| `dimensionId` | guid | yes |

Output: `dimensionId`. Checks: the sheet is live (`E-SHEET-NOT-FOUND`); the sheet holds the dimension
(`E-SHEET-DIMENSION-NOT-FOUND`). A lost dimension can be removed: the command does not read the source. The new record has
the other dimensions in the same order.

### 3.3 The other commands

- `RemoveSheet` removes the dimensions with the record (P10.1, no change).
- `RemoveSheetView` removes the dimensions of that view in the same new record (P10.2 Q6 A). P10.2 has no dimension to
  test it with, so P10.3 adds the test (section 7, test 8). If P10.2 took Q6 B (refuse), P10.3 tests the refusal instead.
- A change of the source (`SetSketchConstraintValue`, `SetExtrudeDistance`, `DeleteSketchItem`, an undo) does not change the
  sheet record and raises no sheet event. The read sees the change.

## 4 The read

`GetSheet` (from P10.2) fills its list `dimensions`, one `SheetDimensionInfo` for each record, in the order of the record.
The record is in `Engine.Core/Queries/GetSheetQuery.cs`, as each result of a query is (P10.2 plan, section 2).

| Field | Type | Content |
|---|---|---|
| `dimensionId`, `viewId`, `source`, `sourceKind`, `x`, `y` | as the record | |
| `lost` | boolean | True when the source is not in the design now. |
| `label` | string or null | `"length"` (constraint `distance`), `"radius"` (constraint `radius`), `"distance"` (extrude); null when lost. |
| `value` | number or null | The number of the design now; null when lost. |

The value and the state at each read:

- `"constraint"`: the constraint with the identifier in a live sketch of `document.Sketches`. The value is `Values[0]`, as
  `EdgeValues.Of` reads it. No live sketch holds it: lost. This covers `DeleteSketchItem` of the constraint, an undo of
  `AddSketchConstraint`, and an undo of `CreateSketch`.
- `"distance"`: `FeatureList.Of(document).DistanceInEffect(source)`. Null: lost. This covers an undo of the `Extrude`. An
  extrude whose body a `Subtract` consumed is still in effect (`FeatureList.Extrude`: "live or consumed"), so its dimension
  is not lost: the result body still has its geometry.
- An undo of the removal gives the source back, and the next read gives the dimension as not lost. "Lost" is never stored.

Determinism: the read copies doubles from the records and uses no arithmetic. Nothing of the read enters the log. The
determinism gate scans each file of `Engine.Core` (`DeterminismCallGateTests.cs:42`), so the read must not call `Atan2` to
give an angle in degrees (see Q1 B).

The read gives no geometry of the dimension line in P10.3 (Q7 A): no ends of the measured edge on the paper and no flag
"true length in this view".

## 5 The diagnostic codes

The rule: add each new code to `docs/diagnostics.md`, to `DiagnosticCodes.cs` and to the list of
`SchemaDiagnosticsEndpoint.cs` (P10.1 added its three codes to the three places). Do not change the meaning of a code.

New codes (Q5 A), each `Error`, namespace `SHEET`:

| Code | Meaning | Raised by |
|---|---|---|
| `E-SHEET-DIMENSION-NOT-FOUND` | A command names a dimension that the sheet does not hold. | `PlaceSheetDimension` (with `dimensionId`), `RemoveSheetDimension` |
| `E-SHEET-DIMENSION-INVALID` | A source kind that is not "constraint" or "distance", a constraint whose kind has no length, a place that is not finite or not on the paper, or a source that another dimension of the sheet has. | `PlaceSheetDimension` |
| `E-SHEET-SOURCE-NOT-FOUND` | The source of a new dimension is not in the design: no live sketch holds the constraint, or no Extrude in effect has the identifier. | `PlaceSheetDimension` |

Existing codes with a new raise site. Each row of `docs/diagnostics.md` gets "Since TASK-nnnn also ...", as the row of
`E-EXPORT-WRITE-FAILED` does. The meaning does not change:

- `E-SHEET-NOT-FOUND`: also `PlaceSheetDimension` and `RemoveSheetDimension`. Its row now says "`RenameSheet` or
  `RemoveSheet` names a sheet that is not live"; the new site names a sheet that is not live, so the meaning stays.
- `E-SHEET-ID-IN-USE`: also `PlaceSheetDimension`. Its row now says "`CreateSheet` has the identifier of a command that the
  log holds, and a sheet takes the identifier of its command". The new site has the same reason for a dimension. P10.2 Q5 A
  does the same for `AddSheetView`, so P10.3 follows the answer of P10.2 Q5.
- `E-SHEET-VIEW-NOT-FOUND` (P10.2): also `PlaceSheetDimension`.

A lost dimension gives no diagnostic code in the read (Q6 A).

## 6 The write set (task number to confirm)

Numbers: on `origin/main` at v0.375 (f7786c1) the last task files are TASK-0390 (the review of 2026-11-15), TASK-0391 and
TASK-0392 (its findings), and the last ADR is 0153. So the number "TASK-0392" in this plan is taken: read it as "the task
of P10.3". If P10.2 takes TASK-0393 and ADR-0154, P10.3 takes TASK-0394 and ADR-0155. Confirm with `ls tasks` and
`ls docs/adr` at the start.

`depends-on`: the task of P10.2. `governed-by`: the list of TASK-0389, plus ADR-0154 (P10.2), ADR-0146, ADR-0149 and the
new ADR.

Create:

- `tasks/TASK-0392-the-dimensions-of-a-sheet.md`
- `docs/adr/0155-the-dimensions-of-a-sheet.md` — records the answers to Q1 to Q7, amends ADR-0153 item 5 (the accepted
  sources, the place again, one source one time on a sheet).
- `Engine.Core/Commands/PlaceSheetDimensionCommand.cs`, `Engine.Core/Commands/PlaceSheetDimensionCommandHandler.cs`
- `Engine.Core/Commands/RemoveSheetDimensionCommand.cs`, `Engine.Core/Commands/RemoveSheetDimensionCommandHandler.cs`
- `Engine.Tests/Drawing/SheetDimensionTests.cs`

Modify:

- `Engine.Core/Commands/SheetResults.cs` — find a view, find a dimension, the check of a source, the check of a place.
- `Engine.Core/Queries/GetSheetQuery.cs`, `Engine.Core/Queries/GetSheetQueryHandler.cs` (files of P10.2) — the dimensions
  with their value and their state.
- `Engine.Core/Hosting/HandlerCatalog.cs` — two commands.
- `Engine.Core/DiagnosticCodes.cs`, `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs`, `docs/diagnostics.md`.
- `Engine.Tests/Persistence/DocumentFileTests.cs` — two sample commands. In P10.1 the test of the file failed with no sample
  of `RemoveSheet` (TASK-0389, Progress), so the samples are necessary.
- `eng/test-floor.txt`
- `docs/adr/0153-the-sheet-in-the-document.md` (amended-by, history), `docs/adr/README.md`
- `docs/roadmap.md`, `docs/CURRENT-STATE.md`, `docs/register.md` (only for a question that stays open)
- Only if P10.2 did not remove the dimensions with a view: `Engine.Core/Commands/RemoveSheetViewCommandHandler.cs`.

Forbid: `Engine.Contracts/**`, `Engine.Kernel/**`, `3DEngine.App/**`, `3DEngine.Core/**`.

## 7 The tests (`Engine.Tests/Drawing/SheetDimensionTests.cs`)

Each test has an injection that must make it fail. Restore each file from saved text after the injection, not with
`git checkout` (memory "Injection restore").

| # | Test | It proves | Injection that it must catch |
|---|---|---|---|
| 1 | `A_Placed_Dimension_Has_Its_Record_And_The_Event_Sheet_Changed` | The dimension has the identifier of its command, its view, source, kind and place; `sheet.changed` carries it; the output gives `dimensionId`. | The handler returns the sheet with no new dimension. |
| 2 | `The_Read_Gives_The_Value_Of_The_Design_At_Each_Read` | A length constraint of 60 reads 60; after `SetSketchConstraintValue` to 75 it reads 75 with no sheet command. An extrude of 10 reads 10; after `SetExtrudeDistance` to 12 it reads 12. A radius reads with the label "radius". | (a) The read keeps the number of the first read in a cache by dimension. (b) The read takes `extrude.Distance` in place of `DistanceInEffect`. |
| 3 | `A_Dimension_Is_Lost_When_Its_Source_Leaves_And_Not_Lost_After_Undo` | `DeleteSketchItem` of the constraint: the record stays, the read gives lost, no value, no label. Undo: not lost, the value again. An undo of the `Extrude`: lost; redo: not lost. | (a) A removal of the source removes the dimension. (b) The read finds the constraint in the log, not in the live sketches. |
| 4 | `A_Consumed_Extrude_Keeps_Its_Dimension` | A `Subtract` consumes the body of the extrude; the dimension is not lost. | The read judges an extrude by the live bodies (`document.Bodies`). |
| 5 | `Each_Wrong_Dimension_Command_Gives_Its_Code` | A sheet that is not live; a view that is not on the sheet; the kind "angle" as `sourceKind`; a constraint of kind `horizontal` and of kind `angle`; a source that is not in the design; a place with NaN and a place off the paper; the identifier of a command in the log (the 1024 refused Redo of `SheetTests`); a second dimension of the same source; `dimensionId` and `RemoveSheetDimension` of a dimension that is not on the sheet. The sheet does not change. | Each check of section 3.1 removed in turn; for example the check of the constraint kind accepts each kind. |
| 6 | `Place_And_Remove_Have_Undo_And_Redo` | Two dimensions; remove the first: the second stays; undo and redo give the record before and after each command. | `RemoveSheetDimension` removes each dimension. |
| 7 | `A_Place_Again_Keeps_The_Identifier_And_The_Order` | With `dimensionId`: a new source, view and place, the same identifier, the same index; one undo gives the record before. | The handler adds the dimension at the end of the list. |
| 8 | `A_Removed_View_Removes_Its_Dimensions_And_Undo_Gives_Them_Back` | Two views with one dimension each; remove one view: its dimension goes, the other stays; undo gives both back. | `RemoveSheetView` keeps the dimensions of the view. |
| 9 | `A_Lost_Dimension_Can_Be_Removed_And_Placed_Again` | A lost dimension: `RemoveSheetDimension` applies; a place again with a new source gives a dimension that is not lost. | `RemoveSheetDimension` reads the source and refuses a lost one. |
| 10 | `An_Open_Of_The_File_Gives_The_Same_Dimensions` | A save and an open give the same list, in the same order. | The encoder of `PlaceSheetDimension` drops `sourceKind`. |

Also: the two samples in `DocumentFileTests` (encode and decode of each command). Injection: no sample of
`RemoveSheetDimension`; the gate "The test has no sample command of ..." must fail.

The P10.2 plan notes that the JSON names of `SheetView` in an event are not verified. If P10.2 adds no HTTP or WebSocket test
of `sheet.changed`, P10.3 adds one that reads the names of a dimension (`dimensionId` or `id`, `sourceKind`, `x`, `y`).

## 8 Stop and ask: the public shape of `Engine.Contracts`

With the recommended options, P10.3 does not change `Engine.Contracts`. Each item below is a change of the public shape. It
needs an ADR in the same commit and a question to the owner first:

- A public list of the source kinds in `Engine.Contracts` (as `SheetRecord.Papers`). It is a new name. Keep the list in
  `SheetResults` in `Engine.Core`.
- A new field of `SheetDimension`: for example the sketch of a constraint, a label, a second point, a text offset, or a
  stored "lost" flag.
- A new event kind, for example `sheet.dimension.placed`. The event `sheet.changed` is enough.
- A new value of `SourceKind` (for example "angle"), or a new meaning of "constraint" or "distance". The comment of the
  contract lists the two kinds. Note: the source kind "distance" (an extrude) and the constraint kind "distance" (a length
  in a sketch) have the same word. The read gives a separate `label`, so the clash stays out of the contract. A rename is a
  change of meaning: stop and ask.
- A field `UnitKinds` for a new unit. The parameters of P10.3 use Length only.

## 9 Questions for the owner (the recommended option first)

**Q1 — Which constraints can be a source.**
- A (recommended): the constraints of kind `distance` and `radius`, and the distance of an extrude. These are the values
  that the window shows on an edge (`EdgeValues.Of`). Each has one length as `Values[0]`.
- B: also `angle`. The record holds a cosine and a sine. The read cannot give degrees in `Engine.Core`, because the gate
  forbids `Atan2` there; the client computes the degrees. It adds a label "angle" and two numbers to the read.

**Q2 — A new source or a new place for a dimension.** ADR-0153 item 5 says that a person "removes it or gives it a new
source", and item 6 lists no command for a new source or for a move.
- A (recommended): `PlaceSheetDimension` takes an optional `dimensionId`. With it, the command places that dimension again:
  a new view, source or place, the same identifier, the same index, one step of undo. It covers a move of the line (R22d)
  and a new source of a lost dimension.
- B: a remove and a new place. Two steps of undo, a new identifier, and the dimension goes to the end of the list.
- C: two more commands, `MoveSheetDimension` and `SetSheetDimensionSource`. ADR-0153 item 6 needs an amendment.

**Q3 — The same source two times.** ADR-0118 item 5: each value of the design shows one time.
- A (recommended): a sheet refuses a second dimension of the same source. Another sheet can show it.
- B: permit it; the read or the export marks the second.

**Q4 — The view and the true length.** ADR-0146 item 1 shows a value in a flat view only where its length is true.
- A (recommended): the command does not test the view. The design changes after the command, so a test at the command
  becomes false later. A later slice gives the read a flag (Q7), and R22d proposes a view where the length is true.
- B: the command refuses a source whose edge the view does not show true. It needs the lines of the view (ADR-0142, about
  2 s for a sphere, ADR-0142 "Bad") in each command, and the result changes with the design.

**Q5 — The codes.**
- A (recommended): three new codes (`E-SHEET-DIMENSION-NOT-FOUND`, `E-SHEET-DIMENSION-INVALID`,
  `E-SHEET-SOURCE-NOT-FOUND`), and new raise sites for `E-SHEET-NOT-FOUND`, `E-SHEET-ID-IN-USE` and `E-SHEET-VIEW-NOT-FOUND`.
- B: two new codes; a source that is not in the design gives `E-SHEET-DIMENSION-INVALID`. A client cannot tell "wrong
  input" from "the design changed".

**Q6 — A lost dimension in the read.**
- A (recommended): the field `lost` with no value and no label. A read gives data, not a diagnostic.
- B: also a warning `W-SHEET-DIMENSION-LOST` in the diagnostics of the read. One more code to register.

**Q7 — The geometry of the dimension line.**
- A (recommended): not in P10.3. The SVG export (P10a) or R22d adds the ends of the measured edge on the paper and the
  flag "true length", from the face tags of each `ViewLine` (`ViewEdges.cs:64-66`, P10.2 plan), as `EdgeValues.Of` finds an
  edge in the window. P10.3 stays at 1 to 2 sessions.
- B: in P10.3. The read finds the edge of each source in its view. It is the first link of a sketch item to a line of a view
  in `Engine.Core`, so P10.3 can grow past its estimate.

## 10 Not verified

- No code of P10.2 exists that I read. `GetSheetQuery`, `SheetInfo`, `E-SHEET-VIEW-NOT-FOUND`, the rule "on the paper" and
  the removal of the dimensions with a view are from the P10.2 plan, not from code.
- The numbers TASK-0392 and ADR-0155. Another session can take them.
- That a `radius` constraint has exactly one value. `EdgeValues.Of` reads `Values[0]`. `SketchConstraints.Recorded` gives
  the values unchanged for each kind except `angle` (`SketchConstraints.cs:102-108`); I did not read the check of the count
  of values in `AddSketchConstraintCommandHandler`.

Verified after the first version of this plan: `DeleteSketchItem` of a constraint, or of an item that a constraint uses, gives
`SketchModel.Without`, which removes each constraint on a removed item (`SketchModel.cs:70-71`). An undo removes each sketch
that its step created (`CommandBus.cs:335`). So these cases give a lost dimension.
- The JSON names of `SheetDimension` in the event `sheet.changed`.
- The 1024 refused Redo for the test of an identifier in use: I read it in `SheetTests.cs`; I did not check that the same
  count works for a dimension.
- I did not run a build or a test. This plan changes no code.
