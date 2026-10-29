---
id: 0109
title: The desktop draws the rebuilt solid of an extrude and shows the fault of a body
status: Active
phase: K0c
opened: 2026-10-29
depends-on: [0108]
governed-by: [0007, 0017, 0019, 0022, 0025, 0027, 0029, 0030, 0031, 0032, 0034]
writes:
  create:
    - tasks/TASK-0109-desktop-follows-a-rebuild.md
  modify:
    - 3DEngine/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
    - docs/adr/**
---

# TASK-0109 — The desktop draws the rebuilt solid of an extrude and shows the fault of a body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K0c is the desktop part of the last step of the milestone "CAD on one part" (Q6 A of the codebase
review of 2026-10-29). K0b (TASK-0108, v0.108) rebuilds each live extrude of a sketch after an edit, and
`body.changed` gives the geometry key and the fault of the body (ADR-0037 item 8). The desktop reads that event for
the name and the visibility only, so it draws the first mesh after a rebuild.

## Goal

A person edits a sketch in the desktop and sees the solid of its extrude follow, and a broken profile shows the
fault of the body in the panel.

## Scope (in)

1. `SessionBodies` keeps the geometry key of the mesh of each body, and fetches the mesh again when `body.changed`
   gives another key, so a rebuild and an undo each show their solid.
2. `LiveBody` gains the fault. The row of a body with a fault ends with "(fault)", and the selected body gives the
   reason.
3. The visual check: a person draws a rectangle, extrudes it, edits the sketch, and sees the solid follow.

## Scope (out)

- The pads of the sketch (V40, V41): TASK-0106.
- The chain of a consumed extrude: phase K0.

## Acceptance criteria

- [ ] After an edit of the sketch, the scene holds the rebuilt mesh and one entity for the body; an undo gives back
      the old mesh.
- [ ] A broken profile gives the fault to the live body, and an undo of the edit clears it.
- [ ] The row and the selected body show the fault; a body with no fault shows none.
- [ ] Each new test fails when its injection breaks the code that it covers.
- [ ] The visual check passes, and the log has no warning.
- [ ] `dotnet test` passes, the build gives zero warnings, and CI passes on the branch and on `main`.

## Progress

- 2026-10-29: opened after K0b. `SessionBodies` fetches the mesh again when the geometry key changes, and the
  panel shows the fault. An image test on the GPU and a panel test.
- 2026-10-29: each of 6 injections fails a test. The floor of `3DEngine.ImageTests` is 100, and 106 on Linux: the
  image test runs on Ubuntu only in CI (ADR-0025 item 4).
