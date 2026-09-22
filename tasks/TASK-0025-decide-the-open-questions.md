---
id: 0025
title: Decide each open question that does not need new work
status: Done
phase: P0.11
opened: 2026-09-22
depends-on: [0024]
governed-by: [0016]
writes:
  create:
    - Engine.Tests/Governance/DiagnosticsReserveGateTests.cs
    - docs/archive/engine-runtime-boundaries.md
    - tasks/TASK-0025-decide-the-open-questions.md
  modify:
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - Engine.Cli/ArgParser.cs
    - Engine.Cli/JsonRenderer.cs
    - docs/diagnostics.md
    - docs/INDEX.md
    - docs/architecture/engine-runtime-boundaries.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0025 — Decide each open question that does not need new work

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner gave one instruction: close whatever is necessary, so that the same question does not come
back. Four entries held a question and not a piece of work. Each one waited for a decision.

- R-0008 asked whether the command-line parser is temporary.
- R-0010 asked whether to rewrite the boundary document or to archive it.
- R-0011 asked whether two diagnostic codes become permanently reserved.
- R-0015 asked whether the command line uses a different JSON encoder.

## Goal

Each of the four entries leaves the section "Open", and no decision returns as the same question.

## Scope (in)

1. Decide each of the four entries and record the reason.
2. Give R-0011 a limit and not only an answer, because the entry names quantity as the risk.
3. Close R-0010, R-0011 and R-0015. Move R-0008 to "Accepted compromises".

## Scope (out)

- Do not change `Engine.Api.Http`. The encoder decision applies to the command line only.
- Do not change a row of `docs/diagnostics.md`. `CLAUDE.md` permits an addition to that file and
  nothing else, therefore the reservation is a new section.
- Do not delete the boundary document. An archive keeps the record.

## Acceptance criteria

- [x] Each of the four entries leaves the section "Open".
- [x] R-0011 has a gate and not only a note.
- [x] The gate was verified by injection.
- [x] `docs/INDEX.md` no longer calls the archived document canonical.
- [x] The command line prints an apostrophe as an apostrophe.
- [x] `dotnet build` gives zero errors and each test passes.

## Outcome

Shipped as v0.26. The register holds two open entries.

**R-0008 — the parser is accepted permanently.** Three reasons. The convention `--param k=v` is
normal for a command line. ADR-0016 gave type coercion and validation to `ParameterBinder`, therefore
`ArgParser` splits text and makes no decision about a value. JSON input on the command line would copy
the HTTP surface and give nothing that the HTTP surface does not give. The comment in the file said
that a wire-format task would replace it; that sentence is now false and it is replaced by the
decision. The entry moves to "Accepted compromises", which held no entry before today.

**R-0010 — the boundary document is archived.** It is at
`docs/archive/engine-runtime-boundaries.md` with a header that forbids its use. `docs/INDEX.md` now
points at the section "Authority diagram" in `CLAUDE.md`. A rewrite was the other exit and it was
refused: each part that is still correct lives in that section and in the ADRs, therefore a rewrite
would give a second place for one decision, which is the condition that made this document wrong.

**R-0011 — a limit, and not only an answer.** The entry names the risk in one sentence: an unlimited
quantity of reserved codes makes the registry unreliable. An answer about two codes would return as
the same question at the third code, which is the outcome that the owner asked to prevent.
`docs/diagnostics.md` gains a section that names each reserved code with its reason, and
`Engine.Tests/Governance/DiagnosticsReserveGateTests.cs` bounds the quantity at two. A third reserved
code fails the build, states the quantity and names each code.

**R-0015 — the command line uses the relaxed encoder.** `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`
replaces the default. The command line now prints `it's here <&>` and not a line of Unicode escapes.
The word Unsafe in that name gives one risk: a page that writes JSON into HTML with no further
encoding. `Engine.Api.Http` keeps the default encoder for that reason, because a browser client can
put a response into a page. No test asserted the escaped form, therefore nothing else changed.

## Method

**Mechanical.** The archive move. The section in `docs/diagnostics.md`. The encoder line.

**Judgement.** Four decisions, and one rule behind each: prefer the answer that removes the question
rather than the answer that delays it.

- The parser earns a permanent place, because the plan that called it temporary no longer exists.
- The document earns an archive and not a rewrite, because a rewrite recreates the duplication that
  made it wrong.
- The reserved codes earn a budget, because a decision about two codes does not answer the question
  about the third.
- The encoder changes for one client and not for two, because the risk in the name is real for the
  other one.

**The write-set gate found a gap in itself.** This task renames a document, and `git status`
writes a rename as `old -> new`. The gate read that whole line as one path and reported that no
list names it. Both sides of a rename are a change and the write set must cover both, therefore
`ChangedFiles` now splits the arrow. `git diff --name-only`, which continuous integration uses,
gives one path per line and passes through unchanged. The defect appeared because this task ran
the gate against its own change, which is the habit that v0.22 started.

**Weakest.** The encoder decision is the one with a real trade. The relaxed encoder does not escape
`<`, `>`, `&`, `'` and `+`. Nothing in this repository writes command-line output into a page, and
the HTTP surface keeps the strict encoder, therefore the trade is safe today. If a later client reads
the command-line output and writes it into HTML, that client must encode it. This note is the record
of that condition.

The archive decision has a second-order cost. A reader who follows an old link now lands on a
document with a header that tells it to go elsewhere. That is one extra step, and it is better than a
document that reads as current and is not.
