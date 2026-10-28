---
id: 0096
title: An open applies each entry of the document file and gives its version, or refuses the file
status: Done
phase: P0.37
opened: 2026-10-28
depends-on: [0095]
governed-by: [0004, 0006, 0015, 0020, 0021, 0028, 0031]
writes:
  create:
    - tasks/TASK-0096-open-applies-each-entry.md
  modify:
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/StepHistory.cs
    - Engine.Tests/Persistence/**
    - Engine.Tests/Commands/**
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - tasks/TASK-0098-gate-forms-of-the-eleventh-review.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0096 — An open applies each entry of the document file and gives its version, or refuses the file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-10-28 found one high defect of the document file (finding E75 of
`docs/reviews/2026-10-28-b-codebase-review.md`). The open applies each entry through the bus of the session, and
that bus returns the cached result for a command identifier that it saw before. A file can hold one identifier two
times, because the live bus applies an identifier again after the cache of 1,024 results forgot it (E17). The open
then skips the second entry with no error, so the Document after the open is not the Document that wrote the file.
The next command writes a second line with the same version, and the file does not open again.
`docs/templates.md`, section 7, puts a high finding into a task before the next product phase, so this task comes
before K2.

## Goal

An open gives the Document that wrote the file, or it refuses the file with `E-IO-LOAD-FAILED`.

## Scope (in)

1. The open applies each entry with no look-up in the idempotency cache, and the version of each result must equal
   the version of its entry (ADR-0015 item 8). A difference refuses the file with `E-IO-LOAD-FAILED`.
2. The open refuses a session that already has a command (`DocumentFile.cs:42-43` says "must be new").
3. The probe of the review is a test: an identifier that comes again after 1,024 other results, then an open.
4. The comment of `StepHistory.cs:16` agrees with the code.

## Scope (out)

- The rule that the live bus refuses an identifier that the log holds (E17). This task can add it if the cost is
  low; otherwise E17 stays in register entry R-0028.
- The medium and low findings of the review (TASK-0098, TASK-0099).

## Acceptance criteria

- [x] The probe of E75 fails before the correction and passes after it.
- [x] An injection that brings back the look-up in the cache fails a test, through the check of the version.
- [x] The replay gate passes.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Outcome

Status: Done · v0.98 · the commit that carries this block.

## Method

**Mechanical.** The bus and the session gain an internal `ApplyEntry` with no look-up in the cache; the open uses it
and checks the version of each result and the version of the session before the first entry. The probe of the review
is a test.

**Judgement.** The open still stores each result in the cache, so a client that sends the last command again after
an open gets its result, as ADR-0015 item 8 says ("the cache fills as in a live session"). The check of the version
stays as a guard: after the fix no input reaches it, and the injection that brings back the look-up shows that it
refuses the file. E17 (the live bus applies an identifier again after the cache forgot it) stays in R-0028.

**Weakest.** No test reaches the check of the version with the code as it is; only the injection shows it.

## Notes for the implementer

- **The fix.** Two forms: a check of the version in `DocumentFile.Load`, which refuses the file; or an apply with
  no cache, as `CommandBus.ForReplay` does, which keeps the file. The review recommends both: the open applies with
  no cache, and the check stays as a guard.
- **The governance.** `governed-by` must equal the ADRs whose `affects` intersect the write set; the gate
  `TaskGovernance` gives the list.
- **The size.** About 1 evening.

## Progress

- 2026-10-28: opened by the second codebase review of 2026-10-28 (TASK-0095).
- 2026-10-28: the owner answered the questions of the second codebase review of 2026-10-28, A to each. This first
  commit records the answers: the roadmap rows K2a, K2b, K2 and K0a hold Q1 to Q9, TASK-0098 holds Q10, and register
  entry R-0042 closes.
- 2026-10-28: the open applies each entry with no look-up in the cache, refuses a session that has a command, and
  checks the version of each result. Two new tests; ADR-0015 item 8 says so.
- 2026-10-28: closed at v0.98. CI run 40758728721 passed on the three runners.
