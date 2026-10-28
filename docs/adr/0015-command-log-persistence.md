---
id: 0015
title: The document file
status: Accepted
topic: Persistence, contracts
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0010', '0012']
amended-by: []
affects:
  - Engine.Contracts/Document.cs
  - Engine.Contracts/LogEntry.cs
  - Engine.Contracts/Geometry/IGeometryBackend.cs
  - Engine.Contracts/Geometry/BackendIdentity.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/DocumentSession.cs
  - Engine.Core/Replay.cs
  - Engine.Core/Persistence/**
  - Engine.Api.Http/EngineHost.cs
  - Engine.Api.Http.Server/Program.cs
enforced-by: Engine.Tests/Persistence/DocumentFileTests.cs (TASK-0091 creates it)
notes: Written again by TASK-0091 (P8a) after decisions KD3 and KD4 of 2026-10-27 and Q1 to Q8 of 2026-10-28. The first text (2026-05-27) planned one log for each engine-api-http process; git holds it.
---

# ADR-0015 — The document file

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Each state of the engine is in memory. A stop of a host loses each body and each command. Objective 12 of
`docs/CHARTER.md` asks that a person can continue the work after three weeks, and objective 17 keeps the
command log, the event stream and replay determinism. The log of the Document is therefore the natural document
file: a replay of the log gives the same Document (ADR-0005 §5, the replay gate).

The first text of this record (2026-05-27) planned one log for each `engine-api-http` process and no file for
the desktop. Register entry R-0025 records that it describes a server and not a document. On 2026-10-27 the owner
decided that the document file is the command log in JSON Lines, which the desktop opens and saves, with a flush
to disk before each result, and one length unit in its header (KD4). The owner also decided that each log entry
records the backend of its command and that the bus writes it (KD3), because anti-objective 10 says: "A command
records the backend that performed the operation. A replay uses the recorded backend." `Command` has no such
field, and a later change would have to rewrite each saved file.

The codebase review of 2026-10-28 found these facts (sections 5 and 6 of the review). No code writes the log to
a file. `IGeometryBackend` has no name. No command or field carries a unit. No code turns a command back into its
parameters. A rejection also takes a sequence number, so a count of sequences cannot check a file. The owner
answered its questions Q1 to Q8 on 2026-10-28, with option A for each.

## Decision

1. **The file.** One document is one file of JSON Lines: UTF-8 with no byte order mark, one JSON object on each
   line, and a line feed at the end of each line. The extension is `.3de`. The first line is the header. Each
   later line is one entry.
2. **The header.** `{"format":"3dengine-document","formatVersion":1,"lengthUnit":"mm"}`. The format version 1 and
   the unit `mm` are the only values for now. An open refuses another format, another version or another unit.
3. **The entry.** One entry for each applied command, in the order of the log:
   `{"version":1,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"CreateBox","schemaVersion":1,"commandId":"…","expectedDocumentVersion":null,"parameters":{…}}}`.
   `version` is `Document.Version` after the command, so the entries count 1, 2, 3 with no gap. The parameters
   have the names and the types of the declaration of the handler (ADR-0013). A number is a JSON number in the
   shortest form that reads back to the same double, and -0 keeps its sign. A rejected or cancelled command
   writes no entry (ADR-0020 item 1).
4. **The write is a part of the commit.** The order in the serial section is: the handler, the checks of the
   lists of bodies, the encoding of the entry, the write of the line and the flush to disk, the commit, the result
   in the cache, then the events. So each applied command is in the file before its result (KD4).
   - A command with a value that the file cannot hold exactly (a number that is not finite, or a string that is not
     well-formed UTF-16) is rejected before the write with `E-IO-NOT-ENCODABLE`.
   - A failed write or flush rejects the command with `E-IO-WRITE-FAILED`. The file is cut back to its last
     complete line, and the session refuses each later command with the same code.
   - The rejection takes a sequence number and gives `command.rejected` (ADR-0005), and the version does not change
     (ADR-0020).
5. **The log entry in memory (KD3).** `Engine.Contracts` gains two records, `BackendIdentity(string Name, string
   Version)` and `LogEntry(Command Command, BackendIdentity Backend)`. `Document.Log` becomes
   `IReadOnlyList<LogEntry>`. `IGeometryBackend` gains `BackendIdentity Identity { get; }`, with no default, so each
   backend states its own name. The bus writes the identity of its backend into each entry. No command payload
   changes.
6. **A replay uses the recorded backend.** `Replay.ReplayLog` and an open refuse an entry whose backend name is not
   the name of the backend of the replay, with `E-IO-BACKEND-MISMATCH`. Another version of the same backend gives
   `W-IO-BACKEND-VERSION` one time for each open, and the open continues. Capability negotiation never selects a
   backend.
7. **The length unit (KD4).** `Document.LengthUnit` is a string, `mm` by default and the only value for now. Each
   number stays a double in that unit, and no code converts a number. The snapshot of ADR-0010 gains `lengthUnit`.
8. **The open.** A host opens a file at its start, before its surface serves a request.
   - It makes the Document with the unit of the header, and applies each entry through the bus of its session,
     with no write. Each entry must be applied and give the version of the entry.
   - The idempotency cache fills as in a live session. The Document gets a new identifier (Q1, ADR-0005 §1), so an
     old cursor of a subscriber cannot resume on another replay. This decides item 6 of ADR-0020.
   - A last line that is not complete or does not parse is cut off with `W-IO-TRUNCATED`. A bad line before it, an
     entry that is not applied, or a version that does not agree refuses the open with `E-IO-LOAD-FAILED`.
9. **A document with no name (Q4).** It writes from its first command into a file in the folder of unsaved
   documents, `<local application data>/3DEngine/unsaved/<document identifier>.3de`, which the host can set.
   `DocumentSession.SaveAs` moves the file to a new name in the serial section, and later entries go there. The
   target must not exist. A failure gives `E-IO-SAVE-FAILED`, and the file stays where it was. P8f gives the save
   to the desktop and to an agent.
10. **The hosts.** `engine-api-http` opens the file that `--file` names, and makes it with a header when it does not
    exist. With no `--file`, it keeps the document in memory only and writes one line that says so at its start; the
    folder of unsaved documents is for the desktop (P8f). A file that cannot open stops the start
    with exit code 6. The desktop opens a file in P8f (Q5). The command line has no file, because each run is a
    one-shot session (ADR-0019 item 7). A test that makes an `EngineHost` gets no file unless it gives one.
11. **The code (Q7).** The journal, the encoding and the open live in `Engine.Core/Persistence/`. Each handler in
    `Engine.Core/Commands/` gives its encoder through an interface of `Engine.Core`, so `ICommandHandler` does not
    change. A test holds that each handler of `HandlerCatalog` has an encoder, and that `Create(Encode(c))` gives
    `c` for each one.
12. **Determinism.** The write and the read use no transcendental function (`CLAUDE.md`, rule 1). The reader reads
    each number as a double. A test holds the bits of -0, a subnormal number, 0.1 and the largest double through a
    write and a read.
13. **The flush.** Each line is one write call, then `FileStream.Flush(true)`. On macOS, .NET uses `F_FULLFSYNC`.
    The host opens the file with no share for a write, so a second host cannot write to it; on Linux and macOS
    that lock is advisory.

This record amends ADR-0010 §2 (the snapshot gains `lengthUnit`) and ADR-0012 §1 (`IGeometryBackend` gains
`Identity`).

## Consequences

**Good:** The work of a person survives a stop, a crash and three weeks (objective 12). The file is the log, so
nothing converts the Document into a second form, and a replay is the proof that the file is correct
(objective 17). Each entry records its backend (anti-objective 10). The design needs no rewrite of the log for
undo, because undo appends a command (P8c).

**Bad:**
- The public shape of `Engine.Contracts` changes: two new records, one member of `IGeometryBackend` that each
  backend must give (the test backends too), the type of `Document.Log` (each reader of the log changes), and
  `Document.LengthUnit`.
- The snapshot gains a field, and a client that reads it strictly must accept it.
- A command with a value that is not finite is applied with no file and rejected with a file, so the command line
  and a host can differ for it.
- `SaveAs` moves the file and does not copy it.
- A failed write leaves the body of the handler in the backend: a leak, not a wrong Document.
- A flush to disk for each command costs time, which P8a measures.

**Next:** TASK-0091 (P8a) applies this record and registers the seven new codes of the namespace `IO`. P8f gives
the open and the save to the desktop and to an agent. A list of unsaved documents for a recovery is not decided.
Register entry R-0025 closes with P8a.

## History

- 2026-05-27 · TASK-0018 · The first text: one log for each process of `engine-api-http`, accepted on a branch that
  nobody merged, and filed on `main` as Proposed on 2026-09-20.
- 2026-10-28 · TASK-0091 · Written again for the document file, after KD3 and KD4 of the owner of 2026-10-27 and
  the answers Q1 to Q8 of 2026-10-28 to the codebase review of that date. The owner accepted the contract on
  2026-10-28.
- 2026-10-28 · TASK-0091 · The contract of items 5 to 7 lands: `BackendIdentity`, `LogEntry`, `Document.Log` of entries,
  `Document.LengthUnit` and `IGeometryBackend.Identity`. A replay of a plain list of commands, as a fixture,
  records no backend and uses the backend that the caller gives.
- 2026-10-28 · TASK-0091 · Item 10: `engine-api-http` with no `--file` keeps the document in memory and says so, because
  twenty test classes start the program in the test process and would write into the folder of unsaved documents
  of the account. The owner chose this on 2026-10-28.
