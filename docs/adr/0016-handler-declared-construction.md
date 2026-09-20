---
id: 0016
title: Handler-declared command and query construction
status: Accepted
date: 2026-09-20
supersedes: []
superseded-by: []
affects:
  - Engine.Contracts/Handlers/**
  - Engine.Core/Hosting/**
  - Engine.Core/Commands/**
  - Engine.Core/Queries/**
  - Engine.Cli/**
  - Engine.Api.Http/Endpoints/**
enforced-by: Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
---

# ADR-0016 — Handler-declared command and query construction

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

Note on the number: ADR-0015 is reserved for command-log persistence. That document exists on the
branch `claude/happy-booth-1cef3f` and milestone P0.5 merges it. The gap is intentional.

## Context

ADR-0013 made each handler the single source of truth for its own schema. A handler declares its
`Parameters`, and the endpoint `/schema/commands/{name}@{version}` projects that declaration.

Nothing consumes the declaration to **build** a command. Therefore each host holds a switch
statement on the command name, and each case holds parameter parsing written by hand:

- `Engine.Cli/Cli.cs` holds a switch with four cases, plus helper methods that parse a double and a
  GUID.
- `Engine.Api.Http/Endpoints/CommandsEndpoint.cs` holds a switch with four cases, plus four build
  methods that read a `JsonElement`.
- `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` holds one condition for the single query.

Three problems follow.

**A new command changes about ten files.** Two of them are central: `Cli.cs` and
`CommandsEndpoint.cs`. Therefore two agents that add two commands collide in the same two files each
time. The working agreement requires parallel work, and this design prevents it.

**The two hosts can validate the same command differently.** Each host holds its own parsing code
for the same parameter. A difference between them is a defect that no test detects, because no test
compares the two paths.

**Registration is duplicated across seven positions.** The consequence is register entry R-0003: the
canonical replay determinism gate registers two handlers, and each host registers five. Therefore
the gate does not exercise the registration set that the hosts use. The architecture depends on that
one gate.

The declaration that the hosts need is present already. Only the step that reads it is absent.

## Decision

**We add one member to each handler interface, and we add a binder to `Engine.Core`.**

`ICommandHandler` gains this member:

```csharp
Command Create(CommandInput input);
```

`IQueryHandler` gains the equivalent member. Each input type is a record in `Engine.Contracts`:

```csharp
public sealed record CommandInput(
    IReadOnlyDictionary<string, object?> Parameters,
    Guid CommandId,
    long? ExpectedDocumentVersion);

public sealed record QueryInput(
    IReadOnlyDictionary<string, object?> Parameters,
    Guid QueryId);
```

`Engine.Core/Hosting/ParameterBinder.cs` converts raw values into typed values. It reads the
`FieldSchema` declaration of the handler. It reports a missing required field and an incorrect type
as a failure with the field name. It parses each value with the invariant culture.

`Engine.Core/Hosting/HandlerCatalog.cs` holds one explicit list of each handler. Each host and the
canonical gate call `HandlerCatalog.RegisterAll`.

Each host then follows the same three steps: find the handler in the registry, bind the parameters,
and call `Create`. No host holds the name of a command.

## Consequences

**Good.**

- A new command changes its own two files and one line in the catalog. It changes no host.
- One validation path serves the command-line host and the HTTP host. A difference between them
  becomes impossible, not merely untested.
- ADR-0013 becomes true. The handler is the source of truth for the shape and for the construction.
- Register entry R-0003 closes. The canonical gate uses the same set as each host.
- The rule in `CLAUDE.md` about a command and a central file becomes active, and a test enforces it.

**Bad.**

- This is a public shape change to two interfaces in `Engine.Contracts`. Four command handlers and
  one query handler must implement the new member. Each future handler must implement it also.
- The binder holds a conversion for each type in the `FieldSchema` vocabulary. The vocabulary has
  eight values, and two of them are deferred. A command that needs `object` or `array` must extend
  the binder and this ADR.
- The catalog is an explicit list. A new handler that nobody adds to the list is absent from each
  host. This failure is visible, because no host can dispatch the command.

**Not chosen.**

- **Reflection over the record constructor.** This method needs no contract change. We refuse it,
  because it reads the order of the constructor parameters. `CLAUDE.md` forbids a dependency on an
  unordered or incidental order in the command path. A binder that depends on parameter order is a
  rule that no test protects.
- **A source generator.** This method is the best final state and it needs no contract change. We
  defer it. The catalog and the binder give the same result today with less machinery. A generator
  can replace the hand-written `Create` members later, and no interface changes when it does.

**Next.**

- Register entry R-0003 closes with this ADR and its task.
- The `Parameters` vocabulary needs `object` and `array` when a command needs a nested value. That
  work needs its own ADR.
