---
id: 0471
title: The queries GetSheets and GetBodies give an agent the live sheets and the live bodies with their names
status: Active
phase: P21
opened: 2026-11-26
depends-on: [0470]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0060, 0083, 0089, 0112, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0188, 0189]
writes:
  create:
    - tasks/TASK-0471-the-lists-of-the-sheets-and-of-the-bodies.md
    - docs/adr/0189-the-lists-of-the-sheets-and-of-the-bodies.md
    - Engine.Core/Queries/GetSheetsQuery.cs
    - Engine.Core/Queries/GetSheetsQueryHandler.cs
    - Engine.Core/Queries/GetBodiesQuery.cs
    - Engine.Core/Queries/GetBodiesQueryHandler.cs
    - Engine.Tests/Queries/DocumentListQueryTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
    - docs/guides/agent-loop.md
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0471 — The queries GetSheets and GetBodies give an agent the live sheets and the live bodies with their names

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P17 (TASK-0439) found two gaps of the HTTP surface for an agent, which register entry R-0075 holds. No query and no field of
the snapshot gives the sheets of the Document, so an agent learns a sheet only from the event `sheet.created` while it listens.
No query gives the live bodies with their names: the snapshot of `/events` gives them, and `GetFeatures` gives the identifiers
with no name. On 2026-11-25 the owner answered Q5 A (TASK-0464): add the two queries, with an ADR and tested examples of the
guide. Row P21 does the exit of R-0075. On 2026-11-26, after the merge of R33 (v0.453), the owner ordered "Start P21".

## Goal

An agent gets each live sheet and each live body with its name from one request on `/queries`, with no WebSocket.

## Scope (in)

1. The query `GetSheets`: each live sheet with its identifier, its name, its paper, its orientation, its projection and the
   identifier of each view.
2. The query `GetBodies`: each live body with the fields of `GetBodyInfo`.
3. One order for the two lists, the order of the log, and its reason in ADR-0189.
4. The registration in `HandlerCatalog`, so that `/queries`, `/schema/queries` and `engine query` reach the two queries.
5. One example of each query in section 12 of `docs/guides/agent-loop.md`, which the test of the guide sends to a host.
6. ADR-0189, the exit of R-0075, the rows of the roadmap, the ledger entry and the counts of the tests.

## Scope (out)

- A change of `Engine.Contracts`, of the snapshot of `/events`, or of the order of `CheckInterference` with no `bodyIds`.
- A parameter that filters a list, the lines of a view in `GetSheets`, and the geometry key or the fault of a body in
  `GetBodies`.
- A new route, a new event kind and a new diagnostic code.
- A tool of the window.

## Acceptance criteria

- [x] A Document with no sheet and no body gives two empty lists with no error, and no query changes the version.
- [x] Each created sheet is listed with the fields of its record and its views in their order; a removed sheet is not listed,
  and an undo of the remove gives it back at its place.
- [x] Each live body is listed with its kind, its name, its visibility and the distance of an extrude, equal to the answer of
  `GetBodyInfo`; a hidden body is listed, and a consumed body and a body of an undone command are not.
- [x] Each list is in the order of the log, also when that order differs from the order of the collection of the Document.
- [x] An agent reaches the two queries through `/queries` of HTTP, and section 12 of the guide has one example of each that a
  host answers.
- [x] An injection in each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass. The local part passes; continuous
  integration is not run yet.

## Decisions

ADR-0189 records each decision. The answer Q5 A of the owner of 2026-11-25 approves the two queries; this task takes these
decisions on their shape:

- The names `GetSheets` and `GetBodies`, in the form of the other queries, with no parameter.
- A sheet gives the fields of its record that need no read of the design, and the identifier of each view, because the
  commands of a view take it. `GetSheet` keeps the lines, the dimensions and the title block.
- A body gives the record of `GetBodyInfo`, so one body and each body have one shape. The geometry key and the fault of the
  snapshot stay out: no query takes the key, and `GetFeatures` gives the fault.
- The order of the log, not the order of the collection of the Document, which the snapshot gives. The test saw the
  collection put an extrude second after a `Translate` consumed a box.
- The result records in `Engine.Core/Queries`, as the results of the measure queries and of `GetConnectors`. No file of
  `Engine.Contracts` changes; the row and R-0075 named "a new shape of `Engine.Contracts`", and the shape is that of the public
  surface.

## Notes for the implementer

- `depends-on` names TASK-0470, the last task that `main` merged at v0.453.
- `governed-by` must equal the ADRs in force whose `affects` meet the write set. Let the gate give the list.
- `CodebaseReviewGateTests` counts this task as a product milestone: row P21 names it. After the review of v0.451 it is the
  third.

## Outcome

Status: Active · commit (the merge of the branch p21-agent-queries) · v0.454

## Method

Mechanical: the two queries in the form of `GetFeatures` and `GetBodyInfo`, their registration, the examples of the guide,
the index of the ADRs, the rows of the roadmap, the exit of R-0075 and the counts of `eng/test-floor.txt`.
Judgement:  the order of the log. The first form took the order of the collection of the Document, as the snapshot does; a
            test of a `Translate` and an extrude gave the extrude second, in the place of the consumed box. The order of the
            log is a property of the Document that an agent can predict, and it does not depend on the .NET runtime.
Weakest:    the order of the log assumes that the identifier of each live sheet and body is the identifier of a command of the
            log (ADR-0012 section 4, and `CreateSheet`). A record with another identifier would not be in the list. The test
            `Each_Live_Body_Is_Listed_With_Its_Name_And_A_Consumed_Body_Is_Not` compares the whole list with the live bodies
            of four kinds of command (`CreateBox`, `CreateCylinder`, `Translate` and `Extrude`), and the test of the guide
            finds four bodies of the bookcase and the new shelf in their order.

Injections (`inject.js` in the folder `p21` of the temporary folder of the owner), each restored byte for byte (sha256).
Each of the fourteen injections made a named test fail:
- the sheets in the order of the collection, in the reverse order, and each sheet in landscape:
  `A_Created_Sheet_Is_Listed_With_Its_Record_And_Its_Views_And_A_Removed_Sheet_Is_Not`;
- no identifier of a view: that test and `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape`;
- only the first live record: the test of the sheets, `Each_Live_Body_Is_Listed_With_Its_Name_And_A_Consumed_Body_Is_Not`
  and the test of the guide;
- the bodies in the order of the collection, no distance of an extrude, and each body visible: the test of the bodies;
- no name of a body: the test of the bodies and the test of the guide;
- an error for an empty list: `An_Empty_Document_Gives_No_Sheet_And_No_Body`;
- no registration of `GetSheets`, and of `GetBodies`: the empty test, the test of that query and the test of the guide;
- `GetSheet` in place of `GetSheets` in the guide: the test of the guide and
  `Each_Block_Of_The_Guide_Is_A_Marked_Example_And_The_Loop_Has_Each_Route`;
- the old name "Milieu" of the shelf in the example of `GetBodies`: the test of the guide.

## Progress

- 2026-11-26: opened on the order of the owner "Start P21", branch `p21-agent-queries` from `origin/main` at `052876d8`
  (v0.453). ADR-0189, the two queries, three tests, two examples of the guide. Ledger entry v0.454.
- 2026-11-26: before the push, the session added register entry R-0080 (the order of the collection) and the count of
  v0.453 from the report of R33 in the correction of the ledger entry.
