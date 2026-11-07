---
id: 0285
title: The platform step builds its probe in the phone host, and the proposal gives the volume of the sphere
status: Active
phase: P0.119
opened: 2026-11-07
depends-on: [0284]
governed-by: []
writes:
  create:
    - tasks/TASK-0285-the-host-probe-and-the-sphere-volume.md
  modify:
    - eng/android/check-platforms.js
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0285 — The platform step builds its probe in the phone host, and the proposal gives the volume of the sphere

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The reviews of Codex of v0.266 and v0.268 gave two leads, and the session confirmed both:

- Medium: `RunAnalyzersDuringBuild` false in `3DEngine.Android.csproj` stopped the analyzer CA1416 in the host, and the step
  and the gate passed. The scope of TASK-0281 named a probe build in the host, and TASK-0281 did not add it.
- Low: ADR-0089 gave 0.32 % for the volume of the sphere; the ratio of its mesh gives 0.40 %.

K5c waits for the owner, so this task comes now.

## Goal

The host of the phone has each check of the projects that it reaches, and ADR-0089 gives the right volume.

## Scope (in)

1. The step reads the switches of the analyzers of the host and builds the probe in the host.
2. The gate refuses a switch of the analyzers with the value false in the files of the host.
3. ADR-0089, Q4: 0.40 % for the sphere.

## Scope (out)

- Code of K5c.

## Acceptance criteria

- [ ] With `RunAnalyzersDuringBuild` false in the host, the step and the gate each fail.
- [ ] On the clean tree, the probe in the host gives the error CA1416.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the injection of the switch, against the step and the gate; the ratio of the volume, computed.
Judgement:  the step takes the paths of the SDKs from the job of the APK when it gives them.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: the two leads, confirmed. With `RunAnalyzersDuringBuild` false in the host, the step gives two offences (the
  switch, and no error at the probe) and the gate one. On the clean tree the probe in the host gives the error CA1416. The
  ratio sin(π/32)/(π/32) × (1 + cos(π/32))/2 is 0.99599.
