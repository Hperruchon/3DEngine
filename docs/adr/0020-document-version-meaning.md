---
id: 0020
title: The meaning of the document version
status: Accepted
topic: Contracts, runtime, observability
date: 2026-09-25
supersedes: []
superseded-by: []
amends: ['0006', '0008', '0010']
amended-by: []
affects:
  - Engine.Contracts/Document.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Replay.cs
  - Engine.Api.Http/WebSockets/**
enforced-by: Engine.Tests/ReplayDeterminism/ReplayVersionTests.cs (TASK-0035 creates it)
---

# ADR-0020 — The meaning of the document version

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Three records give the document version the value of the last event sequence number (`Seq`):

- ADR-0006 §6: `expectedDocumentVersion` is "the `Seq` of the last event the client observed".
- ADR-0008 §2: `DocumentVersion` is the "engine's Seq at result time", and `AsOfDocumentVersion` is
  the "Seq at which the query was answered".
- ADR-0010, validation rule 2: "The `snapshot.version` MUST equal the highest `Seq` the engine has
  emitted at the moment of the reset."

The code does this. A rejected command and a cancelled command each emit an event, and each moves the
version (`Engine.Core/CommandBus.cs:159-174`, `Engine.Contracts/Document.cs:11-14`). The log holds
applied commands only. ADR-0015 §1 and its non-goals also refuse to store a rejected command.

Therefore a replay of the log cannot rebuild the version. A probe on 2026-09-23 showed two results:

- A rejected command, then an applied command. The version was 3, and after the replay it was 2.
- The same, and the applied command stated the version that it saw. The replay rejected it, and the
  replayed document had no body. A body was lost.

ADR-0015 §4 item 5 promises the "same `Document.Version`" after a restart, and that promise cannot hold
today. On 2026-09-25 the owner decided that a rejected command does not change the version.
`docs/reviews/2026-09-23-architecture-challenge.md`, section "Decisions of the owner, 2026-09-25",
item 2, gives the question and the answer.

ADR-0005 defines `Seq` as the identity of an event. It does not define the version, and this record
does not change it.

## Decision

1. **The version counts applied commands.** `Document.Version` equals the number of entries in
   `Document.Log`. An applied command adds one. A rejected command and a cancelled command add
   nothing. A replay of the log therefore gives the same version.
2. **`Seq` stays the identity of an event.** Each event gets the next `Seq`, and a rejection also
   gets one (ADR-0005). The version and `Seq` are two counters. The code must not compute one from
   the other.
3. **The fields keep their names.** `Command.ExpectedDocumentVersion`, `CommandResult.DocumentVersion`,
   `QueryResult.AsOfDocumentVersion` and the field `version` of the reset snapshot each carry
   `Document.Version`. `CommandResult.AppliedAtSeq` stays a `Seq`. The wire shape of these fields does
   not change. Their meaning changes, and this record is the decision for that change.
4. **The reset snapshot gives the cursor.** The snapshot of ADR-0010 gains the field `seq`: the highest
   `Seq` at the moment of the reset. A client continues from `seq + 1`. This replaces validation rule
   2 of ADR-0010. Before this record, a client could use `version` as the cursor. After it, a client
   must use `seq`.
5. **A subscriber can follow the version.** Each applied command emits exactly one `command.applied`
   event (`Engine.Core/CommandBus.cs:98-113`). A subscriber adds one for each `command.applied` event
   after a snapshot. It needs no new field on an event.
6. **The document identifier is out of scope.** ADR-0005 says that a reload issues a new identifier.
   ADR-0015 §3 plans to keep it in the file header. The record for persistence decides this.

## Consequences

**Good:** A replay gives the same version and the same bodies. A command with an expected version
gives the same result after a replay, so no body is lost. The promise of ADR-0015 §4 item 5 becomes
possible. The version is the length of the log, which a person can check by counting.

**Bad:** A client must keep two numbers: the version for a command, and `Seq` for the cursor of the
stream. The reset message gains a field, and a client that used `version` as its cursor breaks. Only
tests read the reset today. A rejection no longer changes the version, therefore a client cannot see
from the version alone that another client tried a command. It must read the events for that.

**Next:** TASK-0035 adds the replay fixture of the probe first, and watches it fail. Then it counts
applied commands only, adds `seq` to the reset snapshot, and corrects the comments that cite ADR-0005
or TASK-0001 for the old meaning.
