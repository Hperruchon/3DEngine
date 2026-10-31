---
id: 0156
title: Each runner hashes a native replay with geometry and solved sketches, and a last job compares the hashes
status: Active
phase: P0.67
opened: 2026-10-31
depends-on: [0155]
governed-by: []
writes:
  create:
    - tasks/TASK-0156-determinism-hash.md
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - eng/compare-determinism.js
  modify:
    - .github/workflows/ci.yml
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
---

# TASK-0156 — Each runner hashes a native replay with geometry and solved sketches, and a last job compares the hashes

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-31 the owner gave the order: "After V3: the cross-platform determinism hash in CI (each runner hashes a
replay with geometry and solved sketches; a last job compares them)", and to close the oldest register entry that the
work answers. Anti-objective 16 says that a replay gives the same result on Windows, Linux and macOS, and on x64 and
arm64. Register entry R-0023 (opened 2026-09-25) recorded that no gate compares the native output of one platform with
another: each runner compared with its own constants only.

## Goal

A difference of one bit in a mesh, a bounding box or a solved point between two runners fails the pipeline.

## Scope (in)

1. `CrossPlatformHashTests`: a replay on the native backend of a log with a cut, moves, a sketch with six constraints
   and its extrude, and the SHA-256 of the bits of each mesh, each bounding box and each solved point, in the file
   that `DETERMINISM_HASH_FILE` names.
2. The job "Gate" keeps the file as an artifact for each runner; the job "Compare the determinism hashes" compares the
   three with `eng/compare-determinism.js`, and fails when a runner gave no hash or two hashes differ.
3. R-0023 closes.

## Scope (out)

- A baseline of the hash in the repository; a change of the library or of the compiler shows as a difference between
  runners only when the runners differ.

## Acceptance criteria

- [ ] The compare script passes three equal files and fails a changed line, a changed hash and a missing runner.
- [ ] The pipeline gives one hash on the three runners.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the test, the script and the two steps of the pipeline. On Windows the hash is
  dd2a6132b0e2f1143a84a439bab092f4f815f360101def8de0aa2f81881994a6. The script passed three equal files and failed a
  changed point, a changed hash and a missing runner.
- 2026-10-31: run 41222744710 gave three hashes and equal summaries: the boxes and the solved points agree on the
  three runners, and a mesh differs. Each body line holds a short hash of its positions and of its indices now.
