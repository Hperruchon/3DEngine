---
id: 0137
title: The command line checks a name before the backend, and the comments of the replay are true
status: Active
phase: P0.56
opened: 2026-10-31
depends-on: [0136]
governed-by: [0002, 0004, 0006, 0014, 0015, 0016, 0018, 0019, 0020, 0021, 0028, 0031, 0033, 0036, 0037]
writes:
  create:
    - tasks/TASK-0137-cli-and-comment-debt-of-r0036.md
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Core/Replay.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Geometry/InProcessMeshBackend.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/Commands/EngineDebtTests.cs
    - Engine.Tests/Hosting/BackendSelectionTests.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Api.Http/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0137 — The command line checks a name before the backend, and the comments of the replay are true

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R11b (TASK-0134) waits for the visual check of the owner, so this task takes two more findings of register entry
R-0036 that change no file of the desktop: E37 and E31. Each one has a correction that its text makes clear.

## Goal

With no native library, a misspelled command or query gives its own error, and the comments and the message of the
replay say what the code does.

## Scope (in)

1. E37: `Cli.Apply` and `Cli.Query` find the handler in the catalog and bind the parameters before the backend
   loads. An unknown name gives `E-CMD-UNKNOWN` or `E-QRY-UNKNOWN` with the exit code 1, and not the exit code 3.
2. E31: the comments of `Replay.cs`, `CommandBus.cs`, `InProcessMeshBackend.cs` and
   `ManifoldGeometryBackend.cs` say what a repeated identifier gives. The message of `ReplayDivergenceException`
   does not end a clause with two full stops.

## Scope (out)

- The comment of the reset in `CommandBus.cs`: it is finding E18 of register entry R-0033.
- E25, a session on a replayed Document: the correction needs a decision on the handover of the bus and its sequence
  number, and an open of a document file (ADR-0015) is the load path today.
- The other findings of R-0036.

## Acceptance criteria

- [ ] A test of each finding fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: E37 and E31 with 2 new test cases and 2 new assertions.
