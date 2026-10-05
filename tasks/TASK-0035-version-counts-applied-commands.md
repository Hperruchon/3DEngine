---
id: 0035
title: The document version counts applied commands, and a replay rebuilds it
status: Active
phase: P0.14
opened: 2026-09-25
depends-on: [0034, 0047]
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
    - docs/register.md
    - docs/roadmap.md
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
8. **The replay reports a divergence.** `Replay.ReplayLog` stops at the first result that is not
   `Applied` and names the command and the error, and it does not pass through the idempotency
   cache. These are finding E7 and the replay part of finding E17 of
   `docs/reviews/2026-10-04-codebase-review.md`. The owner decided this on 2026-10-04 (question Q1
   of that review): a replay that hides a rejection cannot prove the goal of this task.

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

- **What TASK-0034 left.** Section 5 of the codebase review of 2026-10-04 lists each place: the three
  calls of `AdvanceVersion` in `Engine.Core/CommandBus.cs`, the check in `TakeSeqs`, which couples
  the two counters and must go, `Document.AdvanceVersion`, which must refuse a lower value (the rest
  of finding E10), the E9 test in `Engine.Tests/Http/HttpConcurrencyTests.cs`, which must read the
  new field `seq`, and four assertions in `Engine.Tests/CommandBusTests.cs`.

- **The contract gate.** This task changes `Engine.Contracts/Document.cs`. The pipeline job
  "Contract-touched-needs-ADR" wants an ADR change in the same pull request. Scope item 6 gives it.
- **Tests that assert a version.** Some tests assert the old value after a rejection. Correct each one
  to the meaning of ADR-0020. Do not delete one.

## Progress
- 2026-10-05: the tests first. The fixture gives `Beta` the expected version 2, and
  `ReplayVersionTests.cs` runs a live session with a rejection and then a command with an expected
  version. On the code before the change: the fixture replay gave version 5 where 4 is expected; the
  live session gave version 4 and its replay gave 2, with the box lost; a rejected command reported
  version 2 where 1 is expected; the replay of a rejection threw nothing; and a log with one
  `CommandId` two times replayed to 1 entry.
- 2026-10-05: the version is the count of the log. `Document.AdvanceVersion` is gone, so no code can
  set the version, and finding E10 is closed. The bus keeps `Seq` alone, and `TakeSeqs` lost its
  check. The reset snapshot gives `seq`, and the E9 test reads it. The reset test now holds a
  rejection, so the snapshot gives version 5 and seq 6. Seven assertions of the old meaning in
  `CommandBusTests.cs` changed, with the E9 test and the reset test; none was deleted. The bus also gains a replay form with no cache, which the next commit uses.
