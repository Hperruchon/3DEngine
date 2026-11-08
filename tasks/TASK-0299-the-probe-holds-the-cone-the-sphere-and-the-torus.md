---
id: 0299
title: The determinism probe holds the cone, the sphere and the torus
status: Active
phase: K5f
opened: 2026-11-08
depends-on: [0298]
governed-by: [0004, 0074, 0083, 0089]
writes:
  create:
    - tasks/TASK-0299-the-probe-holds-the-cone-the-sphere-and-the-torus.md
  modify:
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - docs/android-test-sheet.md
    - docs/adr/0074-the-probe-of-determinism-is-in-the-engine.md
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0299 — The determinism probe holds the cone, the sphere and the torus

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0089 item 5 says that the determinism probe of ADR-0074 takes a primitive of the record in a later slice, with the new
hash in the test sheet of the phone. The cone, the sphere and the torus shipped at v0.273 and v0.282.

## Goal

The pipeline compares the bits of each vertex of the cone, the sphere and the torus on Windows, Linux, macOS and the
emulator of the phone (anti-objective 16).

## Scope (in)

1. The log of the probe makes a cone, cuts it with a moved cylinder, moves the result, and makes and moves a torus and a
   sphere. A cut by the sphere runs the exact kernel, which is too slow for the emulator.
2. Step 10 of the test sheet of the phone names the new hash.

## Scope (out)

- A change of a command or of the kernel.

## Acceptance criteria

- [ ] The test of the probe passes, and the four platforms give one hash in the pipeline.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the hash on this computer, and the hashes of the four platforms in the job that compares them.
Judgement:  sizes and moves with fractions of a power of two, as the other entries of the probe.
Weakest:    to be written at the close.

## Progress

- 2026-11-08: the probe holds seven bodies; Windows gives 6337a321f5ec00977c81dca4cad0ea0f8a3c170dfbd6b4013ed0634c4128a723.
- 2026-11-08: run 42444929625 failed: the emulator wrote no hash in the wait of the smoke test, because the cut of the
  cone by a sphere ran the exact kernel on 3968 triangles (the test took 14 s here, against 0.9 s before). The probe now
  cuts the cone by a cylinder and moves the sphere and the torus: eight bodies, 1 s, and Windows gives
  e67c318bf264fbf291b22e27ee7887b4fcd1943cee58f3e2dd2531d1773fc873.
