---
id: 0091
title: The document file in the engine and the server
status: Active
phase: P8a
opened: 2026-10-28
depends-on: [0088]
governed-by: [0001, 0004, 0005, 0006, 0007, 0008, 0010, 0012, 0013, 0014, 0015, 0016, 0018, 0019, 0020, 0021, 0024, 0025, 0028, 0029]
writes:
  create:
    - tasks/TASK-0091-document-file.md
    - Engine.Contracts/LogEntry.cs
    - Engine.Contracts/Geometry/BackendIdentity.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Geometry/IGeometryBackend.cs
    - Engine.Core/Engine.Core.csproj
    - Engine.Core/CommandBus.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/Replay.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Core/Geometry/**
    - Engine.Core/Commands/**
    - Engine.Core/Persistence/**
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http.Server/**
    - Engine.Tests/Cli/**
    - Engine.Tests/Commands/**
    - Engine.Tests/Geometry/**
    - Engine.Tests/Http/**
    - Engine.Tests/Queries/**
    - Engine.Tests/Persistence/**
    - Engine.Tests/*.cs
    - Engine.Tests/ReplayDeterminism/ManifoldReplayRoundTripTests.cs
    - Engine.Tests/ReplayDeterminism/ReplayVersionTests.cs
    - Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine/Documentation/Running.md
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0010-subscription-reset-snapshot-format.md
    - docs/adr/0012-geometry-backend-wiring.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/INDEX.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
    - tasks/TASK-0090-host-and-desktop-gaps-of-the-tenth-review.md
  forbid:
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - Engine.Cli/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0091 — The document file in the engine and the server

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase P8a, the fourth phase of the path to "CAD on one part" (rule 4 of the order of the roadmap). The
owner decided KD3 and KD4 on 2026-10-27 and answered the questions Q1 to Q8 of the codebase review of 2026-10-28
on 2026-10-28, with option A for each. The owner accepted the contract of ADR-0015 on 2026-10-28, because it
changes the public shape of `Engine.Contracts`. P8f gives the file to the desktop and to an agent.

## Goal

Each applied command of a host with a file is in that file before its result, and a host that opens the file
rebuilds the same Document with the backend that each entry records.

## Scope (in)

1. ADR-0015 is accepted. It amends ADR-0010 §2 and ADR-0012 §1, whose texts change in the same commit.
2. `Engine.Contracts`: `BackendIdentity`, `LogEntry`, `Document.Log` as a list of `LogEntry`,
   `Document.LengthUnit`, and `IGeometryBackend.Identity`. Each backend gives its identity.
3. The bus writes the identity of its backend into each entry, and `Replay.ReplayLog` refuses an entry of another
   backend.
4. `Engine.Core/Persistence/`: the encoder of each handler, the line of an entry, the journal with its flush to
   disk, the open, and `DocumentSession.SaveAs`.
5. `engine-api-http --file`, and exit code 6 for a file that cannot open.
6. The snapshot gives `lengthUnit`.
7. The seven codes of the namespace `IO` in `docs/diagnostics.md`.
8. Tests: the round trip of each handler, the bits of the edge numbers, the write before the result, a failed
   write, a torn last line, a bad line, another backend, another version of the backend, the save, and the server
   with a file.

## Scope (out)

- The open and the save in the desktop, and the HTTP save and open (P8f).
- A list of unsaved documents for a recovery.
- Undo and redo (P8c).

## Acceptance criteria

- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A file that a host wrote opens in a second host and gives the same Document: the same version, log and
      bodies, with names and visibilities.
- [x] A failed write rejects its command, and the Document does not change.
- [x] The seven new codes are in `docs/diagnostics.md`.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The merge
      records the run.

## Outcome

Status: Done · v0.91 · the commit that carries this block.

The document file exists in the engine and the server. Each applied command of a session with a file is in the
`.3de` file, flushed to disk, before its result; each entry records its backend, and the header records the length
unit. An open applies each entry through the bus, refuses an entry of another backend, and cuts a torn last line
with a warning. A save moves the file. `engine-api-http --file` opens or makes the file; with no `--file` the
document is in memory only, and the server says so.

## Method

**Mechanical.** 31 new tests, with each theory case counted: 5 of the backend in the log, 23 of the file (the full
scene on the native backend among them), and 3 process tests of the server. Thirteen injections each failed a test;
two first passed and made two tests stronger. A process test kills the server after its results, and a second
server opens the file to the same renamed body. Run 40671505395 passed on the three runners with the file code. A
command with a file took 0.07 to 0.08 ms on this computer, and 0.001 to 0.009 ms with no file.

**Judgement.** The bus writes the entry after the handler and before the commit, as ADR-0015 item 4 says, so a
failed write is a rejection with no change of the Document. The reader accepts one kind of JSON value for each
declared type, because a `null` for a string reached the binder as the text "null" in the first version. A replay of
a plain list of commands, as the replay fixture, records no backend and keeps its old form. A server with no
`--file` keeps the document in memory, because twenty test classes start the program in the test process; the
owner chose this on 2026-10-28.

**Weakest.** A failed write leaves the body of the handler in the backend. On Linux and macOS the lock of the file
is advisory, so a second process that ignores it can write to the file. No test kills a process during a write,
so the torn last line is tested with lines that a test writes. The desktop has no file until P8f.

## Notes for the implementer

- **The judge.** A commit that changes `Engine.Contracts/**` must also change a file under `docs/adr/`.
- **The size.** About 3 to 4 evenings.

## Progress

- 2026-10-28: opened after TASK-0088 (v0.90). The first commit holds ADR-0015, accepted by the owner, and the text
  changes of ADR-0010 and ADR-0012 that it amends.
- 2026-10-28: the contract (ADR-0015 items 5 to 7). `BackendIdentity` and `LogEntry` in `Engine.Contracts`;
  `Document.Log` holds entries, `Document.LengthUnit` is the millimetre and refuses another unit, and
  `IGeometryBackend.Identity` has no default: `null`, `managed`, `manifold` with its native version, and `test`
  for the test backends. The bus writes the identity of its backend into each entry, `Replay.ReplayLog` of entries
  refuses another backend with `E-IO-BACKEND-MISMATCH`, and the snapshot gives `lengthUnit`. Five new tests; three
  injections each failed one (the bus records another backend, the replay checks no name, the replay checks the
  version too). `SchemaDiagnosticsEndpoint.cs` joined the write set.
- 2026-10-28: the file (ADR-0015 items 1 to 4, 8, 9, 11 to 13). `Engine.Core/Persistence/` holds `ICommandEncoder`,
  which each of the six handlers implements, `EntryCodec` (the header and the line of an entry, with a reader that
  accepts one kind of JSON value for each declared type), `DocumentJournal` (one write and one flush to disk for
  each line, a cut back and a broken state after a failure, a file made at the first command of a document with no
  name, a move at a save) and `DocumentFile` (create, unsaved, open). The bus writes the entry after the handler and
  before the commit, and refuses each command after a failed write. `DocumentSession.SaveAs` and the close of the
  file. Six codes of the namespace `IO`. 23 new tests, with each theory case counted; the full scene runs on the
  native backend. Nine injections each failed a test; two first passed (a broken file that does not refuse, and the
  check of the version before the apply), so the test of a failed write sends a stale command after it, and the
  repeated check of the version after the apply is gone. `Engine.Core.csproj` joined the write set, to give its
  internals to `Engine.Tests`.
- 2026-10-28: the server (ADR-0015 item 10). `EngineHost` takes `DocumentFileOptions`: it opens the named file, or
  makes it when it does not exist, before its surface serves a request, and frees the backend when the file does
  not open; a host with no options has no file. `engine-api-http --file` gives the path, writes the warnings of the
  open on its error output, and stops with exit code 6 when the file does not open. Two process tests: a host
  killed after its results keeps both commands, and a second host opens the file to the renamed body; a bad header
  gives exit code 6. Two injections each failed them (the server ignores `--file`; a bad file gives code 5). The
  glossary gains "log entry", "document file" and "length unit". A command with a file took 0.07 to 0.08 ms on this
  computer, and 0.001 to 0.009 ms with no file (500 commands of `NoOp`, two runs each). Correction of the line
  before: seven injections each failed a test, not nine.
- 2026-10-28: the owner chose on 2026-10-28 that `engine-api-http` with no `--file` keeps the document in memory,
  because twenty test classes start the program in the test process. ADR-0015 item 10 changes by one sentence, and
  the server says so on its error output; a process test holds the line, and an injection that removes it failed
  it. Closed after a clean build and a full local run of 693 tests (564 in `Engine.Tests`, 89 in `3DEngine.Tests`,
  40 in `3DEngine.ImageTests`). Register entry R-0025 is closed.
- 2026-10-28: correction before the merge: the count of injections of the task is 13 (3 of the contract, 7 of the
  file, 3 of the server), not 15; the Outcome and the ledger entry v0.91 give 13.
- 2026-10-28: correction before the merge: the count of injections of the task is 13 (3 of the contract, 7 of the
  file, 3 of the server), not 15; the Outcome and the ledger entry v0.91 give 13.
