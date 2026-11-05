---
id: 0186
title: Each number parameter of a command declares its unit kind in the schema
status: Ready
phase: P8h
opened: 2026-11-05
depends-on: []
governed-by: [0004, 0008, 0012, 0016, 0019, 0021, 0028, 0031, 0033, 0036, 0037, 0040, 0045, 0049]
writes:
  create:
    - tasks/TASK-0186-unit-kind-in-the-command-schema.md
    - docs/adr/0060-unit-kind-in-the-command-schema.md
  modify:
    - Engine.Contracts/Schema/FieldSchema.cs
    - Engine.Core/Commands/AddSketchCircleCommandHandler.cs
    - Engine.Core/Commands/AddSketchConstraintCommandHandler.cs
    - Engine.Core/Commands/AddSketchPointCommandHandler.cs
    - Engine.Core/Commands/CalibrateImageCommandHandler.cs
    - Engine.Core/Commands/CreateBoxCommandHandler.cs
    - Engine.Core/Commands/ExtrudeCommandHandler.cs
    - Engine.Core/Commands/MoveSketchPointCommandHandler.cs
    - Engine.Core/Commands/PlaceImageCommandHandler.cs
    - Engine.Core/Commands/SetExtrudeDistanceCommandHandler.cs
    - Engine.Core/Commands/SetSketchConstraintValueCommandHandler.cs
    - Engine.Core/Commands/TranslateCommandHandler.cs
    - Engine.Api.Http/Schema/**
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - docs/adr/0013-command-query-schema-declaration.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - docs/INDEX.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine/**
    - Engine.Core/Persistence/**
    - Engine.Kernel/**
---

# TASK-0186 — Each number parameter of a command declares its unit kind in the schema

## Context

A client builds a command from `/schema/commands` (ADR-0013, ADR-0016). `FieldSchema` gives a type
and "required" only, so a number has no stated meaning. Each client holds the meaning in its own
code: `3DEngine/Ui/DesktopUi.cs` holds the names of the pad fields and `CosineAndSine`. An agent and
the module declarations of the desktop redesign must copy them again.

The owner asked for this change on 2026-11-05, from the module-declaration proposal of the desktop
redesign. The owner chose two points of the ADR draft on the same date: the unit
`constraint-value` for `SetSketchConstraintValue` (no new parameter), and `scale` as its own unit
kind. The draft is in the proposal document "3DEngine Module UI Declaration"
(https://claude.ai/artifact/VbCXKp3kkjAbYjXTtM3mhq), section "ADR draft".

In `AddSketchConstraint` the meaning of `value` and `secondValue` depends on `kind`
(`Engine.Core/Sketch/SketchConstraints.cs`): distance and radius take one length, fixed takes two
lengths, angle takes a cosine and a sine, and tangent between two curves takes −1 or 1. So a fixed
unit on the field is not sufficient.

## Goal

Each number parameter of each registered command declares its unit kind in the schema, and a gate
holds the rule.

## Scope (in)

- ADR-0060 "Unit kind in the command schema", from the draft, with the two choices of the owner.
  It amends ADR-0013 (status Amended, `amended-by`, one History line).
- `FieldSchema` gets `Unit` and `UnitBy`; a new record `UnitByField(Field, Units)`.
- A unit on each of the 21 number parameters of the 11 command handlers (count of 2026-11-05;
  the count at the start of the task is the truth):

  | Command | Parameter | Unit |
  | --- | --- | --- |
  | AddSketchCircle | radius | length |
  | AddSketchConstraint | value | UnitBy kind: distance, radius, fixed → length; tangent → sign; angle → cosine |
  | AddSketchConstraint | secondValue | UnitBy kind: fixed → length; angle → sine |
  | AddSketchPoint | x, y | length |
  | CalibrateImage | distance | length |
  | CreateBox | sizeX, sizeY, sizeZ | length |
  | Extrude | distance | length |
  | MoveSketchPoint | x, y | length |
  | PlaceImage | originU, originV | length |
  | PlaceImage | scale | scale |
  | SetExtrudeDistance | distance | length |
  | SetSketchConstraintValue | value, secondValue | constraint-value |
  | Translate | dx, dy, dz | length |

- The schema endpoint projects both members. An absent member is not written.
- The gate rules of ADR-0060.

## Scope (out)

- The units of query results and command outputs. They need the nested schema that ADR-0013
  defers. Add a register entry.
- Any change in the desktop. `DesktopUi.cs` keeps `CosineAndSine` and its pad fields; a later task
  can read the units from the schema.
- The module declaration files of the desktop redesign. They have no ADR and no roadmap row yet.
- A new command, a renamed parameter, or a new `SchemaVersion`.
- Any conversion of a value. The engine reads each number as it reads it today.

## Acceptance criteria

- [ ] ADR-0060 exists with the decision of the draft and the two choices of the owner of
      2026-11-05, and ADR-0013 shows `amended-by: ['0060']` and status Amended.
- [ ] `GET /schema/commands/AddSketchConstraint@1` gives `unitBy` for `value` and `secondValue`
      with the map above; `GET /schema/commands/CreateBox@1` gives `"unit": "length"` for `sizeX`.
- [ ] The gate fails when a number parameter has no unit, when a unit is not in the vocabulary,
      when a parameter has both `Unit` and `UnitBy`, and when `UnitBy.Field` does not name a string
      parameter of the same command. Each failure is shown first by an injected violation
      (CLAUDE.md, Method).
- [ ] No `SchemaVersion` changes, and the canonical replay gate gives the same result as before.
- [ ] `dotnet test` passes; the write-set check and `eng/repo-gates` pass; the ledger has an entry.

## Notes for the implementer

- Read ADR-0013 too. The gate does not list it in `governed-by`, because its `affects` names
  `Engine.Contracts/Handlers/**` and `Engine.Api.Http/Endpoints/Schema**`, not the paths of this
  task. ADR-0060 amends it, and its `affects` covers `Engine.Contracts/Schema/**`.
- The unit vocabulary of V1: `length`, `scale`, `cosine`, `sine`, `sign`, `ratio`,
  `constraint-value`. `ratio` has no user in this task; keep it, because a later command with a
  direction component needs it, or remove it and say why in the ADR.
- A kind that is not in a `UnitBy` map takes no value. Do not add an entry for it.
- Keep `FieldSchema` positional: existing calls `new("number", Required: true)` must compile with
  no change except the added unit argument.
- Take this task after K4c (roadmap, section "Order"), unless the owner gives another order.
- Check the numbers at commit: 0186 and ADR-0060 were free on `main` at 7256f92 (v0.187). K4c took
  ADR-0059 first, so on 2026-11-05 the owner gave ADR-0060 to this task.

## Progress

- 2026-11-05: opened from the module-declaration proposal of the desktop redesign.
- 2026-11-05: takes ADR-0060, because K4c took ADR-0059 first (0b527cd).
- 2026-11-05: governs the line that closes the roadmap row K4, because it is the only open task.
