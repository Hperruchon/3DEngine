---
id: 0185
title: The preview of a proposal on a fork of the Document, a replay of a copy of its log off the serial lock
status: Accepted
topic: Surface, agent
date: 2026-11-22
supersedes: []
superseded-by: []
amends: ['0092']
amended-by: []
affects:
  - Engine.Core/Preview/**
  - Engine.Api.Http/Proposals.cs
  - Engine.Api.Http/Proposals.Preview.cs
  - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
  - Engine.Api.Http/EngineHost.cs
  - Engine.Api.Http/EngineSurface.cs
  - Engine.Api.Http.Server/ServerEngine.cs
  - 3DEngine/DesktopSession.cs
  - Engine.Tests/Http/ProposalPreviewTests.cs
enforced-by: Engine.Tests/Http/ProposalPreviewTests.cs
notes: P15 (P15a, P15b). ADR-0157 item 8 gives the row; the owner answered Q1 to Q5 of the third codebase review of 2026-11-21 with option A on 2026-11-22.
---

# ADR-0185 — The preview of a proposal on a fork of the Document, a replay of a copy of its log off the serial lock

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P15 of the roadmap: "the host runs the commands of a proposal on a fork of the Document, with no change of the first
Document or of its log, and gives the outcome of each command and four differences: the bodies, the state of the solver, the
measures (K15) and the checks (K11). Its ADR gives the design of the second Document (R-0055, ADR-0157 item 8)."

What exists:

- ADR-0092: a proposal of an agent waits in the queue of the host, outside the log. The window shows its commands and no
  result (item 5, Q2 A of 2026-11-08: "a dry run needs a fork of the session, which does not exist"). An accept applies the
  commands in their order and stops at the first command that the engine does not apply (item 6).
- R-0055 closed on 2026-11-09: the owner allows a second Document in one runtime (ADR-0071).
- ADR-0157 item 8: the preview runs on a second Document, "a fork of the first at a stated version". Item 9: the accept runs
  the dry run again (P16).
- `DocumentFile.Check` replays a document file in a scratch `DocumentSession` on a given backend. A replay of the 507
  commands of K10a took 17 to 19 ms (ledger entry v0.413). A body takes the identifier of the command that makes it, so a body
  of a fork has the identifier that it gets after the accept.
- `DocumentSession` puts each command, query and read of one Document behind one serial lock. A long read holds each command
  of the person.

The third codebase review of 2026-11-21 (sections 4 to 6) researched the design and asked five questions. On 2026-11-22 the
owner answered in the chat "start P15 with the A answers", then "Resume all":

- **Q1 A:** P15 is the next product phase, with K15d beside it (K15d is done, v0.433).
- **Q2 A:** the fork is a copy of the log under the lock, then a replay in a new session off the lock, with its own backend.
  Option B, a copy of the objects of the Document and of the bodies of the backend, needs an operation that copies a body
  from one backend to another, and a copy of each kind of record; no capability of `Engine.Contracts/Geometry` has one today.
- **Q3 A:** the dry run runs when the proposal arrives, and again at each new version of the Document while it waits, off
  the lock. Option B, only when the person opens the preview, gives an agent no preview.
- **Q4 A:** the checks cover each pair with a body that the proposal changes, under the bound of work of `CheckInterference`
  (`E-CHECK-WORK-LIMIT`), with "limit" as a state.
- **Q5 A:** two results compare by the content of their records, and the measures to the double. Option B, the hash of the
  determinism probe, names no difference.

Three constraints remove the other forms of the fork:

- A fork that replays under the serial lock holds each command of the person for the replay. So the replay runs off the lock.
- Manifold holds the bodies of a Document by handle, and a session frees its backend. So each fork needs its own backend, and
  the library `Engine.Api.Http` cannot name the native backend (ADR-0019 item 3). So the host gives a factory of backends.
- A fork must send no event to `/events` or to the window: an event of a body that does not exist misleads each client.

## Decision

1. **The scope.** P15 has two slices. **P15a** (this record and TASK-0456): the fork, the outcome of each command and the
   difference of the bodies, on the HTTP surface. **P15b**: the three other differences of ADR-0157 item 8 (items 8 to 10).
   Row P16 gives the accept after a preview, and row R25 the preview in the window.
2. **The fork (Q2 A).** A fork is a second `DocumentSession` with its own Document, its own backend and the in-memory sink of
   its kit (`Engine.Core/Preview/DocumentFork.cs`). The host copies the log of the first Document inside
   `DocumentSession.Read` (`ForkSource.Of`): the version, the entries, the unit and the kind. Off the lock, the fork applies
   each entry of the copy as an open of a file does (`ApplyEntry`, no look-up in the idempotency cache, ADR-0015 item 8), then
   the commands of the proposal as an accept does (`Apply`), in their order and with their identifiers.
   - An entry that records another backend, or that does not apply again with its version, stops the fork with no outcome:
     the preview is `failed` with the new code `E-API-PREVIEW-FAILED` and the cause in its message. A factory that throws
     gives the same.
   - The apply stops at the first command that the fork does not apply, as an accept (ADR-0092 item 6): `failedIndex` and
     `failedCode` name it. A command whose identifier the fork knows gives an earlier version and applies nothing; the preview
     stops there with `E-API-PROPOSAL-COMMAND-KNOWN` and no result for it, as an accept (finding E198).
   - The host gives a factory of backends (`EngineHost`, parameter `forks`); the desktop and `engine-api-http` give a new
     backend of the kind of their own backend. The host frees each backend after its fork. A host with no factory makes no
     preview. Since P15b (TASK-0459) `DocumentFork.Run` takes the factory, and the fork frees each backend that it takes: the
     backend of the fork and, when items 8 to 10 need it, the backend of the base. The first backend is free before the base
     takes the second one.
3. **When, and the surface (Q3 A).** The post of a proposal copies the log for its first preview, inside the read that adds it
   to the queue. Each commit (`command.applied`) asks for a new preview of each open proposal. One worker of the queue computes
   the previews one after the other, the oldest proposal first, so at most one fork runs at a time. A burst of commits gives
   one copy of the log at the last version, not one for each version.
   - `GET /proposals/{id}/preview` gives the preview. For an open proposal it waits for a preview of the version of the
     Document at the request, or of a later version. For a closed proposal it gives the last preview, and waits only for a fork
     that computes it now. An identifier of no proposal of the queue gives HTTP 404 with `E-API-PROPOSAL-NOT-FOUND`; a host
     with no factory gives HTTP 501 with `E-API-NOT-SUPPORTED`. The host itself calls `Proposals.Preview`.
   - The answer: `proposalId`; `state`, `ready` or `failed`; `documentVersion`, the version that the fork copied; `results`,
     the result of each command that ran, as `GET /proposals/{id}` gives it after an accept; `failedIndex` and `failedCode`;
     `bodies`; `error`, null or `{ code, message }` for a failed preview. Since P15b (TASK-0459) also `sketches` (item 8),
     `measures` (item 9) and `checks` (item 10), each after `bodies`; each of the four is null for a failed preview.
4. **The bodies (Q5 A).** `bodies` holds `added`, `removed` and `changed` (`{ before, after }`), each in the order of the body
   identifiers. The fork compares its bodies before the first command of the proposal with its bodies after the last command
   that ran. A body changes when its two records are not equal. The equality of a record compares its content while no member
   is a list; `ProposalPreviewTests` fails when a member of a body record is a list, so the comparison changes with it. By the
   replay rule of ADR-0012 the bodies before the first command equal the bodies of the first Document at that version.
5. **The bound.** The queue keeps one preview for each proposal: at most 8 open and 32 closed (ADR-0092 item 3), so at most
   40. A closed proposal keeps its last preview and gets no new one, so P16 can compare with the result that the person saw. A
   proposal that leaves the queue takes its preview with it. A preview holds records and results, not geometry; the backend of
   a fork lives only during the fork.
6. **The events.** A fork sends no event to a client: its sink is the in-memory sink of its kit, with no broadcaster. The
   first Document, its log, its version and its events do not change. No new event kind exists.
7. **The cancellation.** A fork that a new version makes old runs to its end, and its preview serves each caller that asked
   before its copy of the log; the worker then copies the log again. So a stream of commits cannot hold a caller for ever. The
   request of a caller can stop its own wait. Only the stop of the host stops a fork, at its next command; the stop waits for
   the end of the fork, so no fork runs after its host. A command that runs ends first (ADR-0006 section 4).
8. **P15b: the state of the solver.** For each sketch that the proposal changes, its state after the commands: solved, under
   or over constrained, with its residual, from the records of the fork. The record of P15b decides the form.

   The form (TASK-0459): `sketches` holds one `{ sketchId, before, after }` for each sketch that the commands add or change, in
   the order of the sketch identifiers. A sketch changes when its record after the last command that ran is not the same as
   its record before the first command. Two sketch records compare by content, member by member (`SketchContent`), because
   the equality of the record compares each list by reference; `ProposalPreviewTests` fails when a sketch record or one of
   its items gets a member that the comparison does not read. `before` is null for a sketch that the commands add. Each
   state is `{ state, freedom, residual }` from the query `GetSketch` on the fork, so the preview and the query give the
   same values:
   - `state`: `solved` when each equation is within its tolerance and `freedom` is 0; `under` when each equation is within
     its tolerance and `freedom` is above 0; `over` when an equation is not within its tolerance, because the constraints
     conflict or the iteration did not converge (ADR-0035 items 5, 6 and 8). The solver gives no other sign of a conflict,
     so `over` also names an iteration that did not converge.
   - `freedom`: the count of the free degrees of `GetSketch`.
   - `residual`: the largest absolute residual of the constraints of the sketch, 0 with no constraint, or null when a
     residual is not finite (finding E88).
9. **P15b: the measures (K15).** For each body that the proposal adds or changes, the measures of ADR-0180 after the commands,
   and for a changed body before them, to the double (Q5 A).

   The form (TASK-0459): `measures` holds one `{ bodyId, before, after }` for each body of `bodies.added` and
   `bodies.changed`, in the order of the body identifiers. Each side is `{ mass, error }`: `mass` is the answer of the query
   `GetMassProperties` on the fork (`volume`, `centre`, `facets`, ADR-0180 item 3), or null with the refusal of the query in
   `error`, for example `E-MESH-UNSUPPORTED`. `before` is null for an added body. `GetDistance` and `GetAngle` measure two
   items, so they are not measures of one body, and the preview does not give them.

   **The same solid.** A committed geometry key names one solid (ADR-0059 item 2), and a body with a fault gives no solid. So
   a changed body whose geometry key and fault do not change keeps its solid: a rename or a change of the visibility. Its
   measures before are its measures after, and the fork does not read them again.
10. **P15b: the checks (K11, Q4 A).** `CheckInterference` on the fork, for each pair with a body that the proposal adds or
    changes, under the bound of work of ADR-0182; above it the pair has the state `limit`. Only the pairs whose state changes
    are a difference.

    The form (TASK-0459): `checks` holds one `{ bodyIdA, bodyIdB, before, after }` for each pair whose state changes, A before
    B in the order of the body identifiers. Each side is `{ state, check, error }`. `state` is `apart`, `touching` or
    `overlapping` (the class of ADR-0182 item 3), `limit` (the refusal `E-CHECK-WORK-LIMIT`), or `refused` (another refusal
    of the check, for example `E-GEOM-BODY-FAULTED`). `check` is the answer of the check for the pair (ADR-0182 item 4), or
    null; `error` is the refusal, or null.
    - **The pairs.** After the commands: each pair of a body with a new solid (an added body, or a changed body that does not
      keep its solid, item 9) and another live body. Before them: each pair of a changed body with a new solid or of a
      removed body, and another body that is live before. A pair whose two bodies keep their solids keeps its state, so the
      fork does not check it. A removed body counts, although Q4 A names the bodies that the proposal changes: `Translate`
      and each boolean make a new body and remove the old one (ADR-0021), so a move that ends an overlap shows only in the
      pair of the removed body.
    - **One check for each pair.** The fork asks `CheckInterference` with the two bodies of the pair in `bodyIds`. So the
      bound of work of ADR-0182 item 8 applies to each pair: a pair above it has the state `limit`, and the other pairs are
      checked. No intended pair is given (P11 and P13 put them in the Document), so `fault` is true for each overlap.
    - **Before.** "Before" is the fork before the first command of the proposal. By the replay rule of ADR-0012 it equals the
      first Document at the version of the copy, so the first Document is not read again and its serial lock is not taken.
      The session of the fork holds the state after the commands, so the fork replays the same copy again in a second
      session with a new backend of the factory, the base, and reads each value before the commands there. The base runs
      only when a value before is necessary: a changed sketch that exists before, a changed body with a new solid, or a pair
      whose two bodies are live before.
    - **The difference.** A body that does not exist counts as apart: `before` is null when a body of the pair does not exist
      before the commands, and `after` is null when a body does not exist after them. A pair is a difference when its state
      changes, or when the code of its refusal changes.
11. **The cost.** Each commit with open proposals costs one replay of the log and the commands of each open proposal, off the
    lock: for the bookcase of K10a, 57 entries and a few commands. A log of 507 commands replayed in 17 to 19 ms (v0.413), so 8
    open proposals cost about 0.15 s of a thread for each burst of commits. A native backend is made and freed for each fork.

    The cost of P15b (TASK-0459), measured in a Release build on the computer of the owner, the median of 12 forks of the
    bookcase on the native backend: a proposal that changes no body takes 0.7 ms (the replay of 57 entries); a rename of a
    shelf 1.0 ms (one measure, no check, no base); the new shelf "Milieu" of the guide 41 ms (one measure and 19 checks); the
    lift of module 1 and a rename 28 ms (two measures, 36 checks and the base). So a check of a pair of boxes costs about 1 to
    2 ms, and the checks are most of the cost: 8 open proposals of one new body each cost about 0.35 s of a thread for each
    burst of commits. Each check resolves its two bodies again, so a body with n pairs is resolved n times. The worst case of
    one pair is the bound of ADR-0182 item 8, about 13 s for an intersection at the limit.

## Consequences

**Good:** an agent reads the outcome of its proposal and the bodies that it changes before the person decides, with no change
of the Document, of its log or of `Engine.Contracts`. The person keeps the lock: a command never waits for a fork. One fork
serves P16, R25, K12 and P15b.

**Bad:**

- Each commit with open proposals costs a replay for each of them, also when nobody reads a preview. The cost grows with the
  log; no limit of the log exists for a fork.
- The fork knows only the identifiers of the log. The first session also keeps in its idempotency cache the result of each
  refused command, so a command that reuses such an identifier gives another outcome at the accept. The guide says so.
- The fork has a new document identifier, so the caches of the kernel that are keyed by the Document (`KernelProjection`,
  `ViewBreps`, `SectionCut`, each with a bound of 64) can take entries of a fork, and push out entries of the first
  Document. The result does not change; a later read can build again.
- The window shows no preview until R25, and the preview has the bodies only until P15b.
- The code `E-API-PROPOSAL-COMMAND-KNOWN` is now also a code of the preview, where nothing applies.
- P15b (TASK-0459): the checks are most of the cost of a preview, and one worker computes the previews one after the other.
  A preview with many pairs near the bound of work of ADR-0182 can take minutes and holds the previews of the other
  proposals. No bound of the sum of the work of one preview exists; it needs the counts of the pieces before the checks,
  which only the check computes.
- P15b: a second replay (the base) runs for each preview that needs a value before the commands, which is each proposal with
  a `Translate`. The base and the fork take more entries in the caches of the kernel that are keyed by the Document.
- P15b: the rule of the same solid (item 9) trusts that a committed geometry key names one solid (ADR-0059 item 2). A
  command that changes a solid and keeps its key breaks the rule, and the cache of `KernelProjection` too.
- P15b: `over` names a conflict and an iteration that did not converge with one word, because the solver gives no other sign
  (ADR-0035 item 6).

**Next:** P16 and R25. No register entry.

## History

- 2026-11-22 · TASK-0456 · The record, Accepted: the answers of the owner of 2026-11-22 (option A of Q1 to Q5 of the third
  codebase review of 2026-11-21) and the design of P15; P15a ships items 1 to 7 and 11.
- 2026-11-22 · TASK-0456 · Amends ADR-0092 item 2: a fifth route, `GET /proposals/{id}/preview`.
- 2026-11-22 · TASK-0459 · P15b ships items 8 to 10 and gives their form: `sketches`, `measures` and `checks` in the answer
  of item 3, the base for the values before the commands, the rule of the same solid, the pairs of a removed body, and one
  check for each pair. Item 2: the fork takes the factory of backends and frees each backend. Item 11: the measured cost.
