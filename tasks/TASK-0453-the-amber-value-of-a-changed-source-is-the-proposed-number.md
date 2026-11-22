---
id: 0453
title: On the sheet page the amber value of a source that the same proposal changes is the proposed number
status: Done
phase: P0.183
opened: 2026-11-22
depends-on: [0452]
governed-by: [0007, 0019, 0025, 0029, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0092, 0100, 0101, 0102, 0104, 0105, 0106, 0110, 0111, 0114, 0116, 0120, 0124, 0127, 0129, 0130, 0137, 0138, 0140, 0163, 0166, 0167, 0168, 0170, 0171, 0174, 0176, 0177, 0184]
writes:
  create:
    - tasks/TASK-0453-the-amber-value-of-a-changed-source-is-the-proposed-number.md
  modify:
    - 3DEngine.App/ProposedSheet.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/ProposedSheetTests.cs
    - docs/adr/0177-the-proposal-of-the-agent-on-the-sheet-page.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
---

# TASK-0453 — On the sheet page the amber value of a source that the same proposal changes is the proposed number

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0071 (class debt, opened 2026-11-21, due 2027-05-20): "On the sheet page the amber value of a proposed
dimension is the value of the design, not of the proposal". A proposal can hold a PlaceSheetDimension for a value and a command
that changes that value, for example SetExtrudeDistance. ADR-0177 item 2 gave the amber value from the design now: the value of
the constraint in the sketches of the window, or the distance of the extrude from GetBodyInfo. So the page showed a number that
is not the number after the accept. The exit of the entry: "a task gives the amber value the proposed number, with a test and
an amendment of ADR-0177 item 2; or the owner accepts the value of the design".

Finding V222 of `docs/reviews/2026-11-20-codebase-review.md` found the gap. Its correction: "give the amber value the proposed
number when the same proposal changes it (`ProposedValues`)". TASK-0426 (P0.171) took the first part of that correction, the
lines of the arc (ADR-0177 item 5), and opened R-0071 for the second part, because it changes item 2 and no answer of the owner
covered it.

`ProposedValues` (ADR-0110 item 1, TASK-0334) already reads the two commands that change a value, SetSketchConstraintValue and
SetExtrudeDistance, from the parameters that the queue keeps, and the host gives them on each card (`ProposalCard.Values`).

## Goal

On the sheet page the amber value of a proposed dimension shows the proposed number when the same proposal changes its source.

## Scope (in)

1. `ProposedSheet.Changes`: the values that the same proposal changes, from `ProposedValues.Of`, one for each source, with the
   number of its last change. `ProposedSheet.OfCard` gives the sheet of a card with its changes, and the window uses it.
2. `ProposedSheet.Text` takes the changes: a constraint or an extrude that a change holds gives the proposed number, in the form
   of the page (a radius on a whole circle is a diameter). Each other value keeps the value of the design, as before.
3. The tests, the amendment of ADR-0177 item 2 with a History line, the close of R-0071, the roadmap rows P0.183, the ledger
   entry v0.437 and the counts of the tests.

## Scope (out)

- The lines of the arc on the page (ADR-0177 item 5, TASK-0426).
- A proposed value of another command that changes a value indirectly, for example a constraint of another kind or a move of a
  point. Only the two commands of `ProposedValues` (ADR-0110 item 1) give a number.
- A source that the design does not hold: it still gives "?", also with a change, because the change fails at the accept.
- A change of `Engine.Contracts`, `Engine.Core`, `Engine.Kernel`, a command, an event or the Document; no new diagnostic code.

## Acceptance criteria

- [x] A proposal with CreateSheet, AddSheetView, PlaceSheetDimension on the distance of an extrude and SetExtrudeDistance of the
  same extrude shows the proposed number in amber, and that number is the value of GetSheet after the accept.
- [x] The same for a constraint with SetSketchConstraintValue.
- [x] A change of another source keeps the value of the design.
- [x] With two changes of one source the amber value is the number of the last change.
- [x] Each new test of the four above failed before the change (see "Method").
- [x] An injection in each part makes a named test fail.
- [x] ADR-0177 item 2 states the rule, with a History line.
- [x] R-0071 is closed with the exit "resolved".
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- **The proposed number (R-0071).** Answered by the standing order of the owner of 2026-11-20 (the correction of finding V222
  that the review recommended). When one proposal holds a PlaceSheetDimension for a source (a constraint or the distance of an
  extrude) and also a command that changes that same source (SetSketchConstraintValue or SetExtrudeDistance), the amber value
  shows the proposed number. With no such command it keeps the value of the design, as before.
- **The unit.** The number of the two commands is in the length unit of the document (ADR-0060 item 2, `length` and
  `constraint-value`), as the value of a constraint in GetSketch and the distance of GetBodyInfo. So the text takes the number
  with no conversion, in the form of each other amber value (`SvgExport.Value`).
- **Two changes of one source: the last one.** The commands of a proposal apply in their order, so the value after the accept
  is the number of the last change, also when a change comes before the PlaceSheetDimension. `ProposedValues.Of` keeps the
  last change of each source already (ADR-0110 item 1), so the page and the pending chips use one rule.
- **The kind of the change must match.** A constraint takes a change of SetSketchConstraintValue only, and an extrude a change
  of SetExtrudeDistance only, also when the two identifiers are the same.
- **The source must exist in the design.** A change of a source that the design does not hold gives "?", as before. The accept
  of such a change fails.
- **An angle stays "?".** The page prints no angle (ADR-0177 item 2), so a change of an angle changes nothing.
- **The card, not two lists.** `ProposedSheet.OfCard` reads the sheet items and the values of one card. The window and
  the tests call it, so a test holds the read of the window from the card of the host. `ProposedSheet.Of(items)` stays for the
  tests of the items, with no changes.

## Outcome

Status: Done · commit (the merge of the branch p0183-amber-proposed-value) · v0.437

## Method

Mechanical: the History line of ADR-0177, the close of R-0071, the roadmap rows, the ledger entry and the counts of
`eng/test-floor.txt`.

Judgement: see "Decisions".

Weakest: the frame of the window is not run by a test. `NativeThreeDEngine` calls `ProposedSheet.OfCard(shownProposal)` with the
card of `ProposedValues.Named`, which changes only the name, the label and the value now of each value, not its number. The
tests call `ProposedSheet.Of` with the card of `HostedSession.Waiting`. No picture of the real window was made.

Tests first: the four tests of the host were written with `ProposedSheet.Of(card.Sheet)` before the change. They failed:
`A_Proposed_Distance_Shows_The_Number_Of_SetExtrudeDistance` (["3"], not ["5"]),
`A_Proposed_Constraint_Shows_The_Number_Of_SetSketchConstraintValue` (["10"], not ["12"]),
`A_Change_Of_Another_Source_Keeps_The_Value_Of_The_Design` (["10", "3"], not ["10", "5"]) and
`Two_Changes_Of_One_Source_Give_The_Number_Of_The_Last_Change` (["3"], not ["7"]). Each test also holds that the amber value is
the value of GetSheet after the accept. `The_Text_Takes_The_Number_Of_A_Change_Of_Its_Source` came with the change, because it
calls the new parameter of `ProposedSheet.Text`.

Injections (`inject0453.js` in `%TEMP%\p0183`), each restored byte for byte (sha256). The script changes one exact text, builds
and runs `ProposedSheetTests` (30 tests) with no shell, and lists the tests that failed. The short names: Distance
(`A_Proposed_Distance_Shows_The_Number_Of_SetExtrudeDistance`), Constraint
(`A_Proposed_Constraint_Shows_The_Number_Of_SetSketchConstraintValue`), Another (`A_Change_Of_Another_Source_Keeps_The_Value_Of_The_Design`),
Last (`Two_Changes_Of_One_Source_Give_The_Number_Of_The_Last_Change`), Text (`The_Text_Takes_The_Number_Of_A_Change_Of_Its_Source`)
and Box (`The_Box_And_The_Text_Follow_The_Sheet`).

| Injection | File | The tests that failed |
|---|---|---|
| `OfCard` gives no changes | `3DEngine.App/ProposedSheet.cs` | Distance, Constraint, Another, Last, Text |
| The changes keep each command, so the first change of a source shows | `3DEngine.App/ProposedSheet.cs` | Last, Text |
| A constraint ignores its change | `3DEngine.App/ProposedSheet.cs` | Constraint, Text |
| An extrude ignores its change | `3DEngine.App/ProposedSheet.cs` | Distance, Another, Last, Text |
| A constraint takes a change of an extrude with its identifier (`Target`) | `3DEngine.App/ProposedSheet.cs` | Text |
| A constraint takes the change of another constraint | `3DEngine.App/ProposedSheet.cs` | Text |
| An extrude takes the change of another extrude | `3DEngine.App/ProposedSheet.cs` | Text |
| A radius on a whole circle is not a diameter (`number` for `2 * number`) | `3DEngine.App/ProposedSheet.cs` | Text, Box |
| `SheetProposal.Read` gives the text no changes | `3DEngine.App/ProposedSheet.cs` | Distance, Constraint, Another, Last |
| The window calls `ProposedSheet.Of(shownProposal.Sheet)` | `3DEngine.App/NativeThreeDEngine.cs` | none |

The last form passes. No test runs the frame of `NativeThreeDEngine`, so the one line that gives the card to `OfCard` has no test
(see "Weakest"). The build of the window compiles it, and `OfCard` holds each other part.

## Progress

- 2026-11-22: branch `p0183-amber-proposed-value` from `be1c82d0` (P0.182, TASK-0452, v0.436, not merged yet). Five new tests
  in `ProposedSheetTests`, the changes of a card in `ProposedSheet`, the amendment of ADR-0177 item 2, the close of R-0071, the
  roadmap rows P0.183 and the ledger entry v0.437.
- 2026-11-22: run undefined:1 of `0f84f26d` passed on each job. Closed at v0.437.
