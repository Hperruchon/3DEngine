---
id: 0062
title: The hosts of the surface refuse each bracket form, stop well, and own their session
status: Ready
phase: P0.24
opened: 2026-10-11
depends-on: [0061]
governed-by: [0004, 0005, 0006, 0007, 0010, 0011, 0013, 0016, 0017, 0018, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0062-host-gaps-of-the-sixth-review.md
  modify:
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - 3DEngine/**
    - Engine.Core/CommandBus.cs
    - Engine.Tests/**
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0062 — The hosts of the surface refuse each bracket form, stop well, and own their session

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-11 found gaps in the hosts of the surface. Each finding gives its
evidence in `docs/reviews/2026-10-11-codebase-review.md`.

- E48 (medium): seven forms with brackets pass the address check before the start, and Kestrel binds
  each interface for them.
- E49, E50 (medium): a subscriber holds the stop of a host for 30 s, and Ctrl+C stops the surface of
  the desktop and leaves the window.
- E51 to E57 (low): a port in use, the disposal of the backend, the check of created handles, the
  setting `port`, the refusal messages, the public backend of `EngineHost`, and texts that the
  topology made false.

## Goal

Each host refuses each form that Kestrel binds on each interface, stops when it must, and disposes the
session that it owns.

## Scope (in)

1. The address check accepts brackets only as one pair around an IPv6 address, and the seven forms are
   in the tests (E48).
2. A host ends each subscriber when it stops (E49). The desktop ends its window loop when the surface
   stops (E50).
3. A port in use gives a documented exit code, and the desktop disposes its session on each path
   (E51, E52).
4. A test gives a handle two times in the created list, and the clause that cannot decide leaves the
   bus (E53).
5. The setting of the port has a prefix, the refusal messages name the host, `EngineHost.Backend` is
   internal, and the texts of E57 agree with the code (E54 to E57).

## Acceptance criteria

- [ ] Each new test fails before its correction.
- [ ] A host with one subscriber stops in less than 5 s.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.
