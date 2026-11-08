---
id: 0291
title: The HTTP answer of a save lists its warnings
status: Done
phase: P0.122
opened: 2026-11-08
depends-on: [0290]
governed-by: [0016, 0019, 0030, 0090]
writes:
  create:
    - tasks/TASK-0291-the-answer-of-a-save-lists-its-warnings.md
    - docs/adr/0090-the-answer-of-a-save-lists-its-warnings.md
  modify:
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Tests/Http/DocumentEndpointTests.cs
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0291 — The HTTP answer of a save lists its warnings

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding E164 of the codebase review of 2026-11-06 (e): the HTTP answer of a save did not give `W-IO-OLD-FILE-KEPT`, because
a new field changes the shape on the wire. On 2026-11-08 the owner answered: add the field, with an ADR.

## Goal

An agent that saves over HTTP sees each warning of the save.

## Scope (in)

1. ADR-0090, which amends ADR-0030 item 4.
2. The list `warnings` in the answer of `POST /document/save`.
3. A test of a save that keeps the old file, and the list in the test of a plain save.

## Scope (out)

- The answers of `GET /document` and of the open.

## Acceptance criteria

- [x] A save that keeps the old file gives HTTP 200 with `W-IO-OLD-FILE-KEPT` in its list; a plain save gives an empty list.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0122-save-warnings-b) · v0.275

## Method

Mechanical: the new test, and an injection of an answer with an empty list.
Judgement:  a record of the answer of a save only, so the other answers keep their shape.
Weakest:    the test makes the failed delete with a journal of the tests; no run on a file that another program holds.

## Progress

- 2026-11-08: ADR-0090, the field and two tests. An injection of an answer with an empty list fails the new test.
- 2026-11-08: run 42433290350 passed. Closed at v0.275.
