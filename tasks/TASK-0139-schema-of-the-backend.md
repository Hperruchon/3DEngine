---
id: 0139
title: The schema of the backend is in the list of the surface, and it gives the identity of the backend
status: Done
phase: P0.58
opened: 2026-10-31
depends-on: [0138]
governed-by: []
writes:
  create:
    - tasks/TASK-0139-schema-of-the-backend.md
  modify:
    - Engine.Api.Http.Server/ServerEngine.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - Engine.Tests/Hosting/BackendSelectionTests.cs
    - docs/adr/0008-command-query-event-triad.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0139 — The schema of the backend is in the list of the surface, and it gives the identity of the backend

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R11b (TASK-0134) waits for the visual check of the owner, so this task takes finding E35 of register entry R-0036.
TASK-0036 added `/schema/backend` with no line in ADR-0008 §9, and no gate read it. The HTTP host with the managed
backend gave the version of the assembly `Engine.Core`. Since TASK-0091 each backend has an identity (ADR-0015 item
5), which each log entry records.

## Goal

The list of ADR-0008 §9 is the list of the routes under `/schema`, and `/schema/backend` gives the identity that the
log records.

## Scope (in)

1. ADR-0008 §9 lists `/schema/backend`, with a section of history. A gate in `SchemaEndpointGateTests` holds the
   routes of `EngineSurface.cs` under `/schema` equal to the list.
2. `ServerEngine.Create` gives the host the name and the version of `backend.Identity`. The managed backend gives
   "managed" and "1", and not "1.0.0.0".

## Scope (out)

- The desktop host: `3DEngine/DesktopSurface.cs` gives "manifold" and `NativeVersion`, which are the identity
  already, and R11b holds the files of the desktop.
- E34: the version of the library that loaded.

## Acceptance criteria

- [x] A test of each part fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p058-schema-backend) · v0.140

## Method

Mechanical: one line and a section of history in ADR-0008, one gate that compares two lists, and two calls that read
            the identity.
Judgement:  the list of ADR-0008 §9 was incomplete, which is a false statement about the repository and not a new
            decision, so the task corrects it; the desktop already gives the identity, so its file stays as it is.
Weakest:    the gate reads the text of `EngineSurface.cs`, so a route that a helper maps escapes it.

## Progress

- 2026-10-31: E35 with 1 new test case and 1 new assertion.
- 2026-10-31: 2 injections caught; closed at v0.140.
