---
id: 0283
title: The cone, the sphere and the torus are a proposal for the owner
status: Active
phase: governance
opened: 2026-11-07
depends-on: [0282]
governed-by: []
writes:
  create:
    - tasks/TASK-0283-the-proposal-of-the-cone-the-sphere-and-the-torus.md
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
  modify:
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.App/**
---

# TASK-0283 — The cone, the sphere and the torus are a proposal for the owner

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The order of the owner of 2026-11-06 puts K5 (the cone, the sphere and the torus) after the redesign slices that need no
change of the contract; those slices ended at v0.263. The cone needs a new capability in `Engine.Contracts`, so the
standing order records the question (Q2 of the codebase review of 2026-11-07 (c)) and does not answer it.

## Goal

The owner finds the research and the options of K5c in one record, and answers it in one step.

## Scope (in)

1. ADR-0089, Proposed: the capability, the facets, the commands and the names, the volume; each option with its cost.
2. The checks of the research on disk: the 64-gon of ADR-0040, the kernel solid of the cylinder, and the names that the
   built Manifold library exports.

## Scope (out)

- Code. A change of `Engine.Contracts`. Each waits for the answer of the owner.

## Acceptance criteria

- [ ] `AdrGateTests` passes with the Proposed record.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the export names in `manifoldc.dll` of the tests; the code of `SketchProfile.Turn` and `KernelSolid.Cylinder`.
Judgement:  the volume of each primitive is an estimate from the polygons of 64 and 32, not a measure.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: ADR-0089, Proposed. The built `manifoldc.dll` exports `manifold_of_meshgl64` and `manifold_revolve`; the
  source of Manifold is not in the repository, so how a revolve computes its steps is not verified.
