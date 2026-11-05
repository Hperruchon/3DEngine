---
id: 0060
title: The unit kind of each number parameter in the command schema
status: Accepted
topic: Contracts, clients
date: 2026-11-05
supersedes: []
superseded-by: []
amends: ['0013']
amended-by: []
affects:
  - Engine.Contracts/Schema/**
  - Engine.Core/Commands/**
  - Engine.Api.Http/Schema/**
enforced-by: Engine.Tests/Http/SchemaEndpointGateTests.cs
---

# ADR-0060 — The unit kind of each number parameter in the command schema

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

A client builds a command from `/schema/commands` (ADR-0013, ADR-0016). `FieldSchema` gives a type and "required"
only, so a number has no stated meaning. A client cannot show `mm` beside a length, and it cannot open an angle pad. It
cannot know that `value` and `secondValue` of an angle constraint are a cosine and a sine (determinism rule 2, ADR-0033
item 3). Each client holds these meanings in its own code: `3DEngine/Ui/DesktopUi.cs` holds the names of the pad
fields and `CosineAndSine`. An agent and the module declarations of the desktop redesign must copy them again.

The meaning of a number is design truth, because it decides what the log records. So it belongs in the engine schema,
not in a client file. The symbol of a unit does not: the header of the document holds the length unit (P8e).

In `AddSketchConstraint` the meaning of `value` and `secondValue` depends on `kind`
(`Engine.Core/Sketch/SketchConstraints.cs`). A fixed unit on the field cannot describe it. One command for each kind
would change the log of each saved document, and the project does not rewrite the log.

The owner asked for this record on 2026-11-05, from the module-declaration proposal of the desktop redesign. On the
same date the owner chose `constraint-value` for `SetSketchConstraintValue`, `scale` as its own unit kind, and
`pixel` for the points of `CalibrateImage`.

## Decision

1. **Two members.** `FieldSchema` gets two optional members, and the existing calls stay valid:
   `FieldSchema(string Type, bool Required = false, string? Unit = null, UnitByField? UnitBy = null)` and
   `UnitByField(string Field, IReadOnlyDictionary<string, string> Units)`. `UnitKinds` in the same file holds the
   names of the vocabulary as constants, so a wrong name in a handler is a compile error.
2. **The vocabulary.**
   - `length`: a length in the length unit of the document.
   - `pixel`: a position on an image, in pixels (ADR-0045 item 5).
   - `scale`: a length for one pixel of an image (ADR-0045 item 1). Its value follows the length unit of the document.
   - `cosine` and `sine`: the two numbers of an angle. A client shows a pair as one angle in degrees, and the client
     computes the cosine and the sine.
   - `sign`: −1 or 1.
   - `constraint-value`: see item 4.

   The draft of this record had `ratio`, a number with no unit. No parameter uses it, and a later change can add a
   unit at a low cost (`CLAUDE.md`, "Method"), so the vocabulary does not hold it.
3. **A unit that follows a kind.** `UnitBy` names a `string` parameter of the same command and gives the unit for
   each of its values. `AddSketchConstraint.value`: distance, radius and fixed give `length`; tangent gives `sign`;
   angle gives `cosine`. `AddSketchConstraint.secondValue`: fixed gives `length`; angle gives `sine`. A kind that is
   not in the map takes no value.
4. **The edit of a value.** `SetSketchConstraintValue` has no `kind` parameter. Its `value` and `secondValue` declare
   `constraint-value`: the client reads the kind of the constraint from `GetSketch` and uses the map of
   `AddSketchConstraint`.
5. **Metadata only.** `ParameterBinder` converts no value. Nothing new enters the log, a document file or a replay. The
   `SchemaVersion` of a command does not change. The schema endpoints write `unit` and `unitBy` only when a field
   declares them, so a field with no unit keeps its form of ADR-0013.
6. **The rule.** Each `number` parameter of a registered command declares `Unit` or `UnitBy`, not both. Each unit is
   in the vocabulary. `UnitBy.Field` names a `string` parameter of the same command, and its map is not empty. A field
   that is not a number declares no unit. The list of the vocabulary in the code equals item 2.
7. **Out of this record.** The units of query results and of command outputs. They need the nested schema that
   ADR-0013 defers.

## Consequences

- One source for the meaning of each number. Clients and agents can stop to hold tables of units in code. The module
  declarations of the desktop redesign name no unit. An agent knows from the schema that an angle enters as a cosine
  and a sine.
- `FieldSchema` grows two members, a change of `Engine.Contracts`. 25 number parameters in 11 command handlers declare
  a unit (count of 2026-11-05).
- `UnitBy` is a second rule that a reader must learn. `constraint-value` depends on a query and not on the command
  alone.
- A new number parameter of a command fails the gate until it declares a unit, and a new unit fails it until this
  record names it.

## History

- 2026-11-05 · TASK-0186 · Accepted from the draft of the module-declaration proposal, with the three choices of the
  owner of 2026-11-05.
