---
id: 0439
title: The guide for an agent gives the loop read, measure, propose and check on the HTTP surface, and a test keeps each example true
status: Active
phase: P17
opened: 2026-11-21
depends-on: [0437]
governed-by: []
writes:
  create:
    - tasks/TASK-0439-the-guide-for-an-agent.md
    - docs/guides/agent-loop.md
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
  modify:
    - docs/INDEX.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0439 — The guide for an agent gives the loop read, measure, propose and check on the HTTP surface, and a test keeps each example true

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0157 item 13 puts a guide for an agent first (P17), with the SVG of a view (P10a) as its picture. Each part of the loop
exists now: the measures (K15a, K15b, ADR-0180), the pair check (K11, ADR-0182), the proposals (ADR-0092) and the SVG of a
sheet (P10a). The second codebase review of 2026-11-21 (TASK-0437, sections 4 to 6) recommends P17 next and finds that a
guide in text only has no gate: `DocumentPathGateTests` reads the paths of `docs/INDEX.md` only.

## Goal

A guide in `docs/` tells an agent the exact requests and answers of the loop on the HTTP surface, and a test fails when an
example of the guide and the host disagree.

## Scope (in)

1. `docs/guides/agent-loop.md`: the rules, how to read the examples, the example document (the bookcase of K10a with one
   sheet), the schema, the four steps read, measure, propose and check with the action of the person, the picture
   (`GetViewLines`, and `/export/svg` when a sheet exists), the refusal codes and what to do, and the limits.
2. `Engine.Tests/Guides/AgentGuideExamplesTests.cs`: the test reads the guide, sends each marked request to a host on
   127.0.0.1 in the order of the guide, plays the person with `Proposals.Accept`, and compares each answer with its shape.
3. The row of the guide in `docs/INDEX.md`, the roadmap, the ledger entry and the count of the tests.

## Scope (out)

- The command line (answer Q4 A): a later guide.
- A new route, a list of the sheets, a list of the bodies by a query, or any change of `Engine.Api.Http`, `Engine.Core`,
  `Engine.Contracts`, `Engine.Kernel` or `3DEngine.App`. A defect of the surface goes into the limits of the guide.
- A server of the Model Context Protocol (ADR-0157 item 13): a later row.

## Acceptance criteria

- [x] The guide gives each step of the loop with its exact request and the shape of its answer, in Simplified Technical
      English.
- [x] The guide names the refusal codes that an agent meets, with what to do, and the limits of today.
- [x] Each fenced block of the guide is a marked example, and the test sends each one; a refused request or a different
      shape fails the test.
- [x] An injection in the guide and in the code makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

The owner answered Q1 to Q4 of the second codebase review of 2026-11-21: answered by the standing order of the owner of
2026-11-20 (Q1 to Q4 of the second codebase review of 2026-11-21). Each answer is the recommended option A:

- Q1 A: P17, the guide for an agent, is the next product phase.
- Q2 A: the picture of a part for an agent is `GetViewLines` of a view, which changes nothing, and the SVG of a sheet when a
  sheet exists.
- Q3 A: a test posts each JSON example of the guide to a test host, and checks that each one is accepted and that the answer
  has the shape of the guide.
- Q4 A: the guide covers the HTTP surface only; the command line comes later.

This task takes these decisions; none changes the Document, the log or a contract, so no ADR records them:

- The path `docs/guides/agent-loop.md`, with its row in `docs/INDEX.md`, so the path gate reads it.
- The worked example is the bookcase of K10a (`BookcaseCase`): the agent adds a middle shelf "Milieu" between "Bas" and "Haut".
  The test makes the example document with the builder and one sheet with a front view, because the bookcase file of the
  owner has no sheet and no route lists the sheets.
- The marks of a block: `http agent-request`, `http agent-refusal`, `json agent-result`, `text agent-result` and
  `text agent-person`. A request is written as an HTTP message, so an agent copies it whole, the header too.
- A placeholder `<name>` binds the value of the first answer that gives it, and a later answer must give the same value. A
  shape is a subset: an object needs its members, and an array is a subsequence in order; an empty array needs an empty
  array.
- The read step uses the snapshot of `/events`, because it is the only answer that gives each live body with its name.

## Outcome

Status: Active · commit (the merge of the branch p17-agent-guide) · v0.422

## Method

Mechanical: the rows of the index and of the roadmap, the count of `eng/test-floor.txt`, and the requests of the guide from
the endpoints and the handlers.

Judgement: the form of the marks and of the shapes. A shape with each member of an answer breaks at each new member of an
answer; a subset holds the values that an agent uses and lets the surface grow. The test reads the expected values from the
guide only, so the guide is the one place of each example.

Weakest: the shape is a subset, so a member that the guide does not name can change and no test sees it. The text between
the blocks has no test: a statement in prose can become false. The claim that the new shelf is on the sheet rests on the code
(`SheetLayout.Place` reads the live bodies); the test checks only the start of the SVG file.

First run: the main test failed on the kind of a body ("box" in the guide, "Solid" from the host); the
guide now gives "Solid".

Injections (`inject0439.js` in `%TEMP%\p17`), each restored byte for byte (sha256). Each one made the named tests fail:

- a parameter name of a request (`bodyIdA` to `bodyIdX`), a member name of a shape (`distance` to `gap`), a value of a shape
  (a gap of 373 to 374), the order of two items of a shape, the state after the accept, the header `Content-Type` removed, a
  refusal that the host accepts, and the start of the SVG answer:
  `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape`;
- a route of the guide (`/proposals/<id>` to `/proposal/<id>`), the mark of a request removed, the mark of a result removed,
  and the action of the person removed: that test and
  `Each_Block_Of_The_Guide_Is_A_Marked_Example_And_The_Loop_Has_Each_Route`;
- the route of one proposal in `EngineSurface` (code): `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape`;
- a matcher that accepts each answer (code):
  `The_Shape_Of_A_Result_Refuses_A_Missing_Member_A_Wrong_Value_An_Order_And_A_Changed_Placeholder` and the main test.

## Progress

- 2026-11-21: opened after the second codebase review of 2026-11-21 (TASK-0437, v0.421), branch `p17-agent-guide` from
  `9073010d`. The guide, three tests, fourteen injections. Ledger entry v0.422.
