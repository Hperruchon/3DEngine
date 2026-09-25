---
id: 0033
title: The solution holds no web shell, and ADR-0003 is withdrawn
status: Done
phase: governance
opened: 2026-09-25
depends-on: [0032]
governed-by: [0003, 0007, 0009]
writes:
  create:
    - tasks/TASK-0033-remove-blazor.md
  modify:
    - BlazorApp/**
    - 3DEngine.sln
    - 3DEngine.Core/Documentation/Architecture.md
    - .github/PULL_REQUEST_TEMPLATE.md
    - CLAUDE.md
    - Engine.Tests/Governance/AdrGateTests.cs
    - docs/adr/0003-blazor-as-thin-viewer.md
    - docs/adr/README.md
    - docs/templates.md
    - docs/INDEX.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
---

# TASK-0033 — The solution holds no web shell, and ADR-0003 is withdrawn

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding C1 of `docs/reviews/2026-09-23-codebase-review.md`: `BlazorApp` and `BlazorApp.Client` are a
template with fixed text. They make no call to the engine, they run in the Server and the
WebAssembly modes, and they carry 44 Bootstrap files of 8.7 MB. ADR-0003 asks for a WebAssembly
viewer of two pages over the HTTP and WebSocket surface, and the projects are not that viewer.

On 2026-09-25 the owner said: "a browser client is not the main idea, we may need to rethink how to
show the app". `docs/reviews/2026-09-23-architecture-challenge.md`, section "Decisions of the owner,
2026-09-25", item 6, gives the question and the answer. Register entry R-0021 holds the open question
of how to show the application.

## Goal

The solution and the documents hold no Blazor project, and no rule points to one.

## Scope (in)

1. Delete `BlazorApp/**` and its two entries in `3DEngine.sln`.
2. ADR-0003 gets the status `Withdrawn` and a note in its front matter. Its text does not change.
3. The ADR index gives the new status. The legend and `docs/templates.md` say that the owner can
   withdraw an accepted record when no later record replaces it.
4. `AdrGateTests` lowers the budget of unenforced ADRs from 2 to 1. ADR-0003 was one of the two.
5. `CLAUDE.md`, `docs/INDEX.md`, `.github/PULL_REQUEST_TEMPLATE.md` and
   `3DEngine.Core/Documentation/Architecture.md` name no Blazor project.

## Scope (out)

- No change to the text of an accepted ADR. ADR-0002, ADR-0004, ADR-0005, ADR-0008, ADR-0009,
  ADR-0011 and ADR-0012 name Blazor as history. ADR-0007 keeps `BlazorApp/**` in its field
  `affects`, which only the text of a new ADR can change.
- No decision on how to show the application (R-0021).
- No change to `3DEngine/Documentation/Architecture.md`. It says that the desktop host holds no
  Blazor component, which stays true. TASK-0038 changes its line about ASP.NET Core.

## Acceptance criteria

- [x] `git ls-files` lists no file under `BlazorApp/`.
- [x] `dotnet build 3DEngine.sln` succeeds with zero warnings in a clean build.
- [x] The ADR gate passes with the budget of 1. With the budget at 0 it fails and names ADR-0007.
- [x] Outside the archive, the ledger, the reviews, the task files and the text of the accepted
      ADRs, no file names a Blazor project. `3DEngine/Documentation/Architecture.md` is the one
      exception, for the reason in "Scope (out)".

## Outcome

Status: Done · v0.33 · the commit that carries this block.

## Method

**Mechanical.** The deletion, the solution entries, and the lines that named the projects.

**Judgement.** The status is `Withdrawn` and not `Superseded`, because no later ADR replaces
ADR-0003. The template gave `Withdrawn` to a proposal only, so this task extends the rule to an
accepted record that the owner takes back. The reason goes into the field `notes`, because the text
of an accepted record is permanent.

**Weakest.** The field `notes` is not in the list of fields that `docs/templates.md` lets a person
change after acceptance. ADR-0006 and ADR-0012 already carry the field, so the practice exists, but
the rule does not name it.
