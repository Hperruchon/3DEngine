---
id: 0016
title: Remove both dispatch switch statements and add one handler catalog
status: Done
phase: P0.3
opened: 2026-09-20
depends-on: [0015]
governed-by: [0013, 0016]
writes:
  create:
    - docs/adr/0016-handler-declared-construction.md
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Api.Http/Endpoints/JsonParameters.cs
    - Engine.Tests/Hosting/ParameterBinderTests.cs
    - Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
    - tasks/TASK-0016-remove-dispatch-switches.md
  modify:
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Contracts/Handlers/IQueryHandler.cs
    - Engine.Core/Commands/NoOpCommandHandler.cs
    - Engine.Core/Commands/CreateBoxCommandHandler.cs
    - Engine.Core/Commands/TranslateCommandHandler.cs
    - Engine.Core/Commands/SubtractCommandHandler.cs
    - Engine.Core/Queries/GetBoundingBoxQueryHandler.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Tests/Cli/CliApplyTests.cs
    - Engine.Tests/Cli/CliCreateBoxScenarioTests.cs
    - Engine.Tests/Cli/CliBooleanTransformScenarioTests.cs
    - Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs
    - Engine.Tests/ReplayDeterminism/ManifoldReplayRoundTripTests.cs
    - CLAUDE.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - BlazorApp/**
    - .github/workflows/**
---

# TASK-0016 — Remove both dispatch switch statements and add one handler catalog

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0003 records a defect in the canonical replay determinism gate. The gate registered
two handlers. Each host registered five. Therefore the gate did not exercise the registration set
that the hosts use, and the architecture depends on that one gate.

The cause is duplication. Seven positions in the solution registered handlers. Each host also held a
switch statement on the command name with parameter parsing written by hand. A new command changed
about ten files.

ADR-0013 made each handler the source of truth for its schema. Nothing read that declaration to build
a command.

`CLAUDE.md` v0.17 marked one anti-pattern inactive, because a person could not obey it while the
switch statements existed.

## Goal

A new command changes its own two files and one line in the catalog. No host names a command.

## Scope (in)

1. Write ADR-0016 for the contract change.
2. Add `Create` to `ICommandHandler` and to `IQueryHandler`, with an input record for each.
3. Implement `Create` on four command handlers and one query handler.
4. Add `ParameterBinder` and `HandlerCatalog` to `Engine.Core/Hosting`.
5. Remove the switch statement from the command-line host and from each HTTP endpoint.
6. Generate the usage text from the handler declarations.
7. Point each host and each replay gate at the catalog. Close R-0003.
8. Add a gate test that fails when a host names a command. Activate the rule in `CLAUDE.md`.

## Scope (out)

- Do not add a source generator. ADR-0016 defers it.
- Do not extend the schema vocabulary with `object` or `array`. That needs its own ADR.
- Do not change a narrow unit test that registers its own handlers on purpose. Such a test checks one
  behaviour of the bus and must not receive each handler.
- Do not change the geometry backend, the render host or the web shell.

## Acceptance criteria

- [x] No file in `Engine.Cli` or `Engine.Api.Http` contains a quoted command name or query name.
- [x] A gate test proves the previous item, and the gate fails when a violation exists.
- [x] Each host and each replay gate register through `HandlerCatalog`.
- [x] `dotnet build` on the solution gives zero errors.
- [x] `dotnet test` gives more passing tests than before and zero failures.
- [x] Register entry R-0003 is closed.
- [x] ADR-0016 exists and the index lists it.

## Outcome

Status: Done. Version v0.18.

Both switch statements are removed. Each host follows three steps: find the handler, bind the
parameters, call `Create`. `Engine.Cli/Usage.cs` generates the command list and each example from the
declarations.

`dotnet build` gives zero errors. Two warnings remain in the vendored sample framework and predate
this task. `dotnet test` gives **151 passed, zero failed, zero skipped**, up from 134.

The gate was verified by injection. A quoted command name was added to `Engine.Cli/Cli.cs`. The gate
failed and reported `Engine.Cli/Cli.cs:84 names "CreateBox"`. The violation was then removed.

The command-line host applies `NoOp` and `CreateBox` through the new path.

## Method

Mechanical: the five `Create` implementations; the conversion of a `JsonElement` to a neutral value;
the catalog list; the ledger, the roadmap, the ADR index and the register entry.

Judgement: five decisions.

1. The binder takes `object?` values, not strings. A string form would send each number through a
   string round trip, and that can change a logged number. The HTTP surface therefore converts a
   `JsonElement` itself and keeps a number as a double. `Engine.Core` needs no reference to
   `System.Text.Json`, and the kernel stays free of a transport concern.
2. An unknown field fails the bind. A silent discard would hide a typo in a script and in an agent
   request.
3. The catalog is an explicit list, not a scan of the assembly. Replay determinism needs a fixed set
   and a fixed order.
4. A narrow unit test keeps its own registration. Nine test classes register one or two handlers on
   purpose, because each one checks a behaviour of the bus. Only the two replay gates and the two
   hosts must agree, and only those four changed.
5. Three CLI tests changed their assertion. Each one asserted the exact wording of a message from a
   hand-written parser, and ADR-0016 replaced that parser. Each test now asserts that the message
   names the field and the rejected value, which is a stronger contract than the exact wording and it
   survives a later change to the message. The behaviour did not change: the exit code is 2 and the
   usage text appears.

Weakest: the gate test finds a quoted command name. It does not find a command name that a host
builds from parts, for example a concatenation or an interpolated string. A host that wanted to
re-introduce a switch statement could defeat the gate that way. I judged the risk low, because such
code has no reason to exist, but no test detects it.

Second weakest: the CLI and the HTTP query path each render one concrete result type, `Aabb`. A second
query with a different result type needs a typed render path. ADR-0016 records this item under "Next".
It is not a register entry, because no second query exists and the register limit is a cost.
