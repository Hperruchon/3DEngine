---
id: 0159
title: The PNG reader refuses each error of the decompressor, so an image cannot stop the desktop
status: Ready
phase: P0.68
opened: 2026-11-01
depends-on: [0158]
governed-by: [0009, 0022, 0023, 0046]
writes:
  create:
    - tasks/TASK-0159-png-reader-refuses-each-zlib-error.md
  modify:
    - 3DEngine.Core/Imaging/PngReader.cs
    - 3DEngine.Tests/PngReaderTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0159 — The PNG reader refuses each error of the decompressor, so an image cannot stop the desktop

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-01 found V68 (high): a PNG file whose zlib header sets the flag FDICT makes the decompressor throw `ZLibException`, which is not the `InvalidDataException` of ADR-0046 item 1, and the render loop of the desktop ends at the first frame that draws the image, at each start. It comes before the next product phase, by `docs/templates.md`, section 7.

## Goal

Each PNG file that the reader cannot decode gives `InvalidDataException`, so the desktop skips the image and keeps its window.

## Scope (in)

1. V68: `Inflate` gives `InvalidDataException` with a reason for each exception of the decompressor, `ZLibException` and each other `IOException` included.
2. A test of the flag FDICT, and a test that each malformed stream of the review gives `InvalidDataException`.

## Scope (out)

- The other findings of the review (P0.69 to P0.71).
- A catch of each exception in the render loop of the host.

## Acceptance criteria

- [ ] The test of the flag FDICT fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

