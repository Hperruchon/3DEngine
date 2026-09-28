---
id: 0035
title: The document version counts applied commands, and a replay rebuilds it
status: Ready
phase: P0.14
opened: 2026-09-25
depends-on: [0034]
governed-by: [0004, 0005, 0006, 0008, 0010, 0011, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0035-version-counts-applied-commands.md
    - Engine.Tests/ReplayDeterminism/ReplayVersionTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Replay.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Api.Http/WebSockets/EventBroadcaster.cs
    - Engine.Tests/**
    - docs/adr/0020-document-version-meaning.md
    - docs/glossary.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/Handlers/**
    - Engine.Contracts/Geometry/**
    - Engine.Core/Commands/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0035 — The document version counts applied commands, and a replay rebuilds it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0020 gives the decision and each reason. In short: the version today follows the last event
sequence number, a rejection moves it, and the log holds applied commands only. A replay therefore
gives a different version, and a command with an expected version after a rejection is lost in the
replay. This is finding E1 of `docs/reviews/2026-09-23-codebase-review.md`.

Finding T1 of the same review adds: the replay fixture has `NoOp` and `CreateBox` only
(`Engine.Tests/ReplayDeterminism/ReplayDeterminismFixture.cs:23-29`), so the defect passes the gate.

## Goal

A replay of the log gives the same version and the same bodies as the Document that wrote the log.

## Scope (in)

1. **A failing fixture first.** Add to the replay fixture a rejected command, then an applied
   command with an expected version. Run the gate on the code of today, and record in the Outcome
   block that it fails.
2. **The version.** `Document.Version` equals the count of entries in `Document.Log` (ADR-0020,
   item 1). A rejected command and a cancelled command do not change it.
3. **`Seq`.** The bus keeps its own counter for `Seq`. No code computes the version from `Seq` or
   `Seq` from the version.
4. **The reset snapshot** gains the field `seq`, the highest `Seq` at the moment of the reset
   (ADR-0020, item 4).
5. **The comments.** Each comment that gives the old meaning agrees with ADR-0020. Known places:
   `Engine.Core/CommandBus.cs:12` and `Engine.Contracts/Document.cs:11-13`.
6. The field `enforced-by` of ADR-0020 loses the text "(TASK-0035 creates it)".
7. A glossary term, or a correction of one: document version.

## Scope (out)

- No persistence and no document file. ADR-0015 and register entry R-0025 hold that.
- No new field on an event. ADR-0020, item 5, gives the reason.
- No change to the document identifier.

## Acceptance criteria

- [ ] The new fixture fails on the code before the change, and the Outcome block records the two
      values of the version.
- [ ] After the change, a replay of the fixture gives the same version and the same set of bodies.
- [ ] A rejected command gives a `CommandResult` with the version before the command, and a new `Seq`
      in the stream.
- [ ] A client that takes a reset and continues from `seq + 1` reaches the same state as a client
      that saw each event. The existing reset tests prove this with the new field.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The contract gate.** This task changes `Engine.Contracts/Document.cs`. The pipeline job
  "Contract-touched-needs-ADR" wants an ADR change in the same pull request. Scope item 6 gives it.
- **Tests that assert a version.** Some tests assert the old value after a rejection. Correct each one
  to the meaning of ADR-0020. Do not delete one.
