---
id: 0164
title: The exact predicates of the profile of a sketch
status: Done
phase: K1
opened: 2026-11-02
depends-on: [0162]
governed-by: [0004, 0033, 0035, 0036, 0037, 0039, 0040, 0050]
writes:
  create:
    - tasks/TASK-0164-exact-predicates.md
    - docs/adr/0050-exact-predicates.md
    - Engine.Core/Exact/Predicates.cs
    - Engine.Tests/Exact/PredicateTests.cs
  modify:
    - Engine.Core/Sketch/SketchProfile.cs
    - docs/adr/0036-extrude-of-a-sketch-profile.md
    - docs/adr/0040-extrude-of-curves.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - 3DEngine.Core/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
---

# TASK-0164 — The exact predicates of the profile of a sketch

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-02 the owner gave the order "start K1". K1 is the next phase that blocks K4, which needs K0, K1 and K2. The
owner answered A to two questions on the same day:

- **Q1 A.** `SketchProfile` uses the exact predicates for the live command, the open and the rebuild. A saved file
  that only the rounded rule accepted no longer opens, and the format version stays 1.
- **Q2 A.** K1 gives the 2D predicates of the profile. The 3D predicate arrives with K4, its first consumer.

ADR-0050 holds the decision. A search with exact arithmetic found the cases where the rounded checks of
`SketchProfile` are wrong.

## Goal

Each sign that decides the profile of an extrude is the sign of the true coordinates, on each platform, so K4 can
build on predicates that are correct.

## Scope (in)

1. `Predicates.Orient2D`: a rounded filter with a bound, and an exact path with integers (ADR-0050 items 2 and 3).
2. `Predicates.AreaSign`, exact each time (item 4).
3. `SketchProfile` uses them for the checks of a cross, a touch and the area, and refuses a vertex that is not finite
   (item 4).

## Scope (out)

- `Orient3D` and each other 3D predicate (Q2 A). They arrive with K4.
- An exact boolean. `docs/CHARTER.md` defers it, and Manifold computes each solid.
- The integer checks of `ContourProposal` in `3DEngine.Core`. They are already exact in `long`, and the render kernel
  does not reference `Engine.Core`.
- A tool that repairs a saved file that the exact rule refuses.

## Acceptance criteria

- [x] Each case of the context of ADR-0050 has a test that fails with the rounded form and passes with the exact form.
- [x] A test compares the filter with the exact path on 200,000 cases near one line.
- [x] An injection of each part of the filter (the bound, the smallest permanent) fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k1-exact-predicates) · v0.166

## Method

Mechanical: a search with exact BigInt arithmetic in Node.js for each case where the rounded checks are wrong; an
            injection of each old or wrong form against its tests; a comparison of the filter with the exact path on
            200,000 cases near one line.
Judgement:  the filter bound 4ε and the smallest permanent 1e-270, from the bound of Shewchuk and the error of an
            underflow; an exact path with BigInteger and not the expansions of Shewchuk, because its proof is the
            arithmetic of integers.
Weakest:    the bound of the filter rests on the proof of Shewchuk and on the argument of ADR-0050 item 2; the tests
            show no counterexample, but they do not prove the bound.

## Progress

- 2026-11-02: opened.
- 2026-11-02: closed at v0.166.
