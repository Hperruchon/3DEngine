---
id: 0041
title: CLAUDE.md holds each rule that a session needs, and the rules that sessions broke are in it
status: Done
phase: governance
opened: 2026-09-26
depends-on: [0039]
governed-by: []
writes:
  create:
    - tasks/TASK-0041-claude-md-method.md
  modify:
    - CLAUDE.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/CHARTER.md
    - docs/adr/**
    - docs/templates.md
---

# TASK-0041 — CLAUDE.md holds each rule that a session needs, and the rules that sessions broke are in it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner asked on 2026-09-26 whether `CLAUDE.md` works for the model that runs the sessions. The
rules that earlier sessions broke were not in the file. The owner put them in the prompt of each
session instead: read a file before you cite it, prove a gate by injection, `set -o pipefail`, an
absent tool is not evidence, Node.js and not Python, a clean build for a warning count. By the test
of the owner, a rule that a person pastes into each prompt is a recurring question. The owner said:
clean the file, and make sure that what belongs in it is there.

## Goal

`CLAUDE.md` holds each rule that a session needs on each run, and no rule that another file holds.

## Scope (in)

1. One section "Method" replaces the sections "Tests" and "Rules with no other home". It holds the
   test rule with the write-set command, the six rules that sessions broke, the commit trailer, and
   the behaviour rules that have no other home.
2. Each rule in the section is one bullet with the command or the check that goes with it.
3. The ledger entry v0.34 gains a paragraph for this change, because the branch is not merged and
   the next ledger entry must be the codebase review.

## Scope (out)

- No change to the language rules, the workflow, the authority diagram, the determinism rules, the
  diagnostic code rule or the stop conditions.
- No change to `docs/templates.md`. The commit form and the write-set command stay there; the
  section "Method" repeats the command, because a session needs it in the file it reads first.
- No evaluation with a model. That is a dry run that the owner does: start a session with "start",
  and count the files it reads, whether it runs the write-set check before its first commit, and
  whether it states one thing that it did not read.

## Acceptance criteria

- [x] `CLAUDE.md` has one section "Method" and no section "Tests" or "Rules with no other home".
- [x] Each of the six rules from the prompt of the session of 2026-09-25 is one bullet in "Method".
- [x] The write-set command in "Method" is the command of `docs/templates.md`, section 2.
- [x] The determinism rules are unchanged.
- [x] `dotnet test` passes, and the write-set check passes with `WRITE_SET_TASK=TASK-0041`.

## Outcome

Status: Done · v0.34, addendum · the commit that carries this block.

## Method

**Mechanical.** The merge of two sections into one, and the six bullets from the prompt.

**Judgement.** The write-set command appears in `CLAUDE.md` and in `docs/templates.md`. This is the
one repeat that the rule for rules permits, because a session must have the command in the file it
reads first, and the template holds the form. The ledger change is a paragraph in the entry v0.34
and not a new entry, because the branch is unmerged and v0.35 must be the codebase review.

**Weakest.** The sentence "the computer of the owner has no Python" is a fact about one computer in a
public file. A second computer with Python makes it false, and no gate reads it. The rule that
matters, "write a script in Node.js", stays true on each computer.
