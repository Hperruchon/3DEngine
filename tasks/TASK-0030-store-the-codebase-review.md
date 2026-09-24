---
id: 0030
title: The codebase review of 2026-09-23 is a record in the repository
status: Done
phase: governance
opened: 2026-09-23
depends-on: [0029]
governed-by: []
writes:
  create:
    - tasks/TASK-0030-store-the-codebase-review.md
    - docs/reviews/2026-09-23-codebase-review.md
  modify:
    - docs/INDEX.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - docs/adr/**
    - docs/register.md
---

# TASK-0030 — The codebase review of 2026-09-23 is a record in the repository

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-09-23 the owner asked for a review of the code: the architecture, the patterns, the defects,
the next task, the research for that task, and a recommended approach. The review was published as a
private page at <https://claude.ai/artifact/6bvXVUvbVWdAcQ4Tm1mMiN>. The owner then asked where to
store it.

A page outside the repository has the same risk as the twenty objectives had before TASK-0029: a later
session cannot find it. Objective 12 says that a person must be able to continue after three weeks.

## Goal

The review is a file in the repository that a later session reads like each other document.

## Scope (in)

1. `docs/reviews/2026-09-23-codebase-review.md`, with the words of the published page. The four
   diagrams are Mermaid blocks, which GitHub renders.
2. A row for `docs/reviews/` in `docs/INDEX.md`.

## Scope (out)

- Do not act on a finding. Each finding needs its own task, register entry or ADR.
- Do not add a register entry. The owner decides first which findings become work.
- Do not change the code.

## Acceptance criteria

- [x] The file exists, and it says that it is a dated record and not the authority.
- [x] `docs/INDEX.md` names `docs/reviews/` and gives the same rule.
- [x] Each objective and anti-objective that the review cites exists. The objective gate checks this.

## Outcome

Status: Done · v0.30 · the commit that carries this block.

The published page stays as the rendered view for the owner. The file in the repository is the record.
When the two differ, the file wins, because the repository is the memory between sessions.

## Method

**Mechanical.** The conversion from HTML to Markdown. The words are the same. The labels became text
markers, and the diagrams became Mermaid blocks.

**Judgement.** A new folder `docs/reviews/`, and not `docs/proposals/`, because a review describes the
code at one commit and a proposal describes a future decision. One file for each review, with the date
in the name, so that a later review does not replace an earlier one.

**Weakest.** A review does not age. Its findings stay true only until the code changes, and no gate
reads them. The register makes a problem expire; a review does not. The owner must choose which
findings become register entries or tasks.
