---
id: 0018
title: Salvage the unmerged branch and remove each duplicate identifier
status: Done
phase: P0.5
opened: 2026-09-20
depends-on: [0017]
governed-by: [0014, 0016]
writes:
  create:
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Tests/Hosting/EngineHostingTests.cs
    - docs/adr/0015-command-log-persistence.md
    - docs/proposals/render-host-direction.md
    - tasks/TASK-0018-salvage-the-unmerged-branch.md
    - tasks/TASK-0019-command-log-persistence.md
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Tests/Governance/AdrGateTests.cs
    - docs/adr/README.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0018 — Salvage the unmerged branch and remove each duplicate identifier

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0012 records branch `claude/happy-booth-1cef3f`. Nobody merged it. It holds four
identifiers that the main branch uses for different work:

- A different ADR-0014. The branch record is command-log persistence. The main record is the Manifold
  native-interop posture.
- A different TASK-0012. The branch task is persistence. The main task is the Manifold backend.
- A different TASK-0013. The branch task is the engine hosting factory. The main task is Subtract and
  Translate.
- A different v0.12. The branch entry is the hosting factory. The main entry is the charter.

Therefore the reference "ADR-0014" is not clear, and a working hosting factory sits on a branch that
nobody reads.

Register entry R-0013 records a second loss. `docs/proposals/render-host-direction.md` exists only as
an untracked file inside `stash@{0}`. No commit holds it. The analysis disappears if a person removes
the stash.

## Goal

Each identifier names one thing, and each piece of useful work is in a commit on the main line.

## Scope (in)

1. Salvage the hosting factory. Adapt it to the rules that arrived after the branch.
2. Salvage its tests.
3. Re-file the persistence ADR as ADR-0015. Do not change the design text.
4. Re-file the persistence task as TASK-0019. Do not change the scope text.
5. Recover the renderer proposal from the stash into a commit.
6. Close register entries R-0012 and R-0013.

## Scope (out)

- Do not implement persistence. ADR-0015 has the status `Proposed`, and the clamp in `CLAUDE.md`
  holds.
- Do not re-file the branch task for the hosting factory as its own file. Its work lands here, and
  this task records the salvage. A second file for work that this task performs would give two
  records of one change.
- Do not delete the branch and do not delete the stash. A commit now holds each useful part, but the
  owner decides when to remove either one.
- Do not change the decision text of the persistence ADR. The identifier, the status and one note
  changed. Nothing else.

## Acceptance criteria

- [x] One composition point builds the default engine, and both hosts use it.
- [x] The factory does not name `Engine.Geometry.Manifold`, therefore the dependency gate passes.
- [x] The factory does not register a handler by hand. `HandlerCatalog` does that work.
- [x] ADR-0015 exists with front matter, and the index agrees.
- [x] TASK-0019 exists with front matter and a status that agrees with ADR-0015.
- [x] A commit holds `docs/proposals/render-host-direction.md`.
- [x] `dotnet build` gives zero errors and `dotnet test` gives more tests than before.
- [x] Register entries R-0012 and R-0013 are closed.

## Outcome

Shipped as v0.20. The test count went from 173 to 181.

**The factory is not the branch version.** Three rules arrived after the branch, and each one changed
the design:

- ADR-0014 section 4 permits a reference to `Engine.Geometry.Manifold` only at the composition root of
  a host. `CLAUDE.md` permits `Engine.Core` to reference only `Engine.Contracts`. The branch factory
  constructed `InProcessMeshBackend` itself and returned it as a concrete type. This version takes
  `IGeometryBackend` as a parameter. Each host keeps its own three lines of backend selection, and
  that duplication is correct.
- ADR-0016 made `HandlerCatalog` the one list of handlers. The branch factory carried its own
  `RegisterDefaultCommands` and `RegisterDefaultQueries`. Those two methods are not salvaged. A second
  registration surface is the defect that register entry R-0003 recorded, and the branch predates the
  catalog.
- `HandlerCatalog` holds four command handlers today. The branch factory registered two. A verbatim
  merge would have removed `Translate` and `Subtract` from each host.

The kit gains `CreateCommandBus(sink)`, `CreateCommandBus()` and `CreateQueryBus()`. The reason that
the branch gave for not returning a bus still holds: the HTTP host wraps the sink in
`BroadcastingEventSink` before the bus sees it, and the command-line host uses the sink directly. The
two helpers let each host name the sink that it wants and nothing else.

Four of the twelve branch tests are not salvaged, because they tested the two registration methods
that this version removes. Each test that named a command by hand now compares the kit against
`HandlerCatalog`. One new test applies a command through a bus that the kit built, so that the tests
prove composition and not only presence.

**The ADR gate needed one refinement.** ADR-0015 has the status `Proposed`, therefore its
`enforced-by` field says `UNENFORCED`. The budget of two unenforced records would have failed. The
budget now counts a record in force only. A proposal describes work that nobody built, therefore
enforcement cannot exist and the budget must not count it. This is a correction and not a loophole:
the budget still bounds each accepted decision at two.

**Two identifiers that the roadmap already anticipated.** `docs/roadmap.md` phase P8a already said
"ADR-0015" before this task ran. The renumber agrees with it.

## Method

**Mechanical.** The recovery of the proposal with `git show 'stash@{0}^3:...'`, because an untracked
file in a stash lives in the third parent. The renumber of each internal reference. The front matter
of the two re-filed records. The index row.

**Judgement.** The status of ADR-0015. The branch accepted it. This task files it as `Proposed`,
because no code implements the design, no task is ready, and the clamp in `CLAUDE.md` still forbids
persistence. An accepted record with no implementation is the exact drift that the gates of v0.19
exist to prevent. The owner can accept the record with one word, and nothing in the design needs a
change first.

Second judgement: the shape of the factory. A verbatim merge was possible and wrong. The branch was
written before three rules that now govern the same code, therefore the salvage had to keep the intent
and rewrite the mechanism.

**Weakest.** The mechanical saving from the factory is small. Each host previously held five shareable
lines, and the backend selection that cannot move is three lines. A reader can fairly ask whether this
is an abstraction for a requirement that does not exist, which `CLAUDE.md` forbids. The answer given
here: the value is one named composition point, and TASK-0019 attaches persistence to exactly that
point. If the owner rejects that reasoning, the correct action is to delete
`Engine.Core/Hosting/EngineHosting.cs` and inline the five lines again. Nothing else depends on it.

## Correction, 2026-09-21 (TASK-0021)

This task is closed, therefore the text above stays as written. This block gives the correction.

The Outcome block and the Method block each give a reason that was false. Both said that the clamp in
`CLAUDE.md` forbids persistence, and that this fact supports the status `Proposed` on ADR-0015. No
such clamp exists. Milestone v0.17 removed it, in TASK-0015, four days before this task ran.
`CLAUDE.md`, section "Scope clamps", now says "No clamp is active" and "Persistence arrives with
ADR-0015 and its task".

The cause: the agent read the copy of `CLAUDE.md` that was in its context at the start of the session,
and not the file.

The conclusion does not change. Two true reasons remain: no code implements the design, and roadmap
phase P8a is pending. The owner examined the question on 2026-09-21 and kept the status `Proposed`.

The lesson is general, and it is the same lesson as the dependency gate defect in TASK-0017. A
statement about the repository must come from the repository. `CLAUDE.md` is a file that changes, and
a copy of it is not the file.
