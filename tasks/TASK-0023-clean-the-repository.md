---
id: 0023
title: Clean the repository and remove the manual step from the gate
status: Done
phase: P0.9
opened: 2026-09-21
depends-on: [0022]
governed-by: []
writes:
  create:
    - tasks/TASK-0023-clean-the-repository.md
  modify:
    - .github/workflows/ci.yml
    - .gitignore
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0019-command-log-persistence.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0023 — Clean the repository and remove the manual step from the gate

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner gave three instructions. Direct the work of the agent, and do not do the work by hand.
Clean the branches. Close each entry that a decision can close, so that the same question does not
come back.

Three conditions made a person necessary:

1. `.github/workflows/ci.yml` ran on a push to the main branch only. The gate therefore gave no report
   on a branch, and a person had to open a pull request by hand before register entry R-0002 could
   close. That condition made the owner the slowest part of the loop.
2. Twelve local branches, five remote branches, three git worktrees and two stashes held work in an
   unclear state. Register entry R-0012 covered one branch only.
3. Register entry R-0014 asked whether `.gitignore` must contain the `.claude` directory. Register
   entry R-0009 was closed in fact and open in the register.

## Goal

A push gives a report with no action by a person, and each branch, worktree and stash that the
repository holds has a reason to exist.

## Scope (in)

1. Run the gate on each push, on each branch.
2. Put `.claude/` in `.gitignore`. Close R-0014.
3. Delete each branch, each worktree and each stash that holds nothing unique. Preserve each one that
   does, with a tag.
4. Close R-0009, which the register holds open although TASK-0017 did the work.

## Scope (out)

- Do not delete the remote main branch and do not delete `p0-foundations`.
- Do not delete a branch without a check. A branch that is not fully merged takes a tag first.
- Do not change code. This task changes configuration, branches and documentation.

## Acceptance criteria

- [x] A push on a branch runs the gate.
- [x] `.gitignore` contains `.claude/`.
- [x] Each deleted branch was fully merged, or a tag preserves it.
- [x] Each deleted stash is preserved by a tag.
- [x] No worktree other than the main one exists.
- [x] R-0009 and R-0014 are closed.

## Outcome

Shipped as v0.23.

**The gate no longer needs a person.** The trigger is now `push` with no branch filter, plus
`pull_request` for the two jobs that need a base reference. A branch with an open pull request runs
the gate two times. That cost is accepted, because the repository is public and a run is free.

**Twelve local branches became two.** Ten were fully merged and `git branch -d` deleted each one,
which refuses an unmerged branch and therefore gave the check. Two held unique commits and each one
received a tag first:

- `archive/happy-booth-1cef3f` — TASK-0018 salvaged its useful work into v0.20.
- `archive/p7b-integrate-package` — its one commit reached the main branch as `831ebbc`.

**Three worktrees became zero.** Each one lived under `.claude/worktrees/` and each one held a full
copy of every project file, pinned between 31 and 51 commits behind the main branch. A stale copy of
this kind made the dependency direction gate report a broken rule as satisfied; TASK-0017 recorded
that defect and gave the gate a filter. The filter stays as a second defence.

**Two stashes became zero.** Neither held a change to a tracked file. Each one held untracked files
only, and each of those files is accounted for: `docs/proposals/render-host-direction.md` entered a
commit in v0.20, `launchSettings.json` entered `.gitignore` in v0.22, and
`.claude/settings.local.json` enters `.gitignore` here. The tags `archive/stash-0` and
`archive/stash-1` preserve both.

**The cleanup found an uncommitted draft ADR.** The worktree `happy-booth-1cef3f` refused removal,
because it held `docs/adr/0015-handler-owned-command-construction.md` with the status `Proposed`, and
a change to the index. The decision in that draft is the decision that ADR-0016 carries, which shipped
in v0.18 and which a gate enforces. No new record is needed. The draft is not lost: a commit in that
worktree captured it, and `archive/happy-booth-1cef3f` points at that commit.

The draft gave one thing that the repository did not hold. It names
`Engine.Core/Persistence/CommandCodec.cs` as a third position that would dispatch on a command name,
and it states that the persistence work must use handler-owned construction from the first day and not
add a switch that a later change deletes. TASK-0019 now carries that constraint.

**R-0014 is decided.** `.gitignore` contains `.claude/`. The directory holds settings, a transcript
and a worktree. Each one is host-specific and regenerable, nothing under it was ever tracked, and a
worktree under it broke a gate.

**R-0009 was closed in fact and open in the register.** TASK-0017 corrected the false `DRAFT` header
in the native build workflow and its Outcome states that R-0009 is resolved. Nobody moved the entry.
The register and the repository now agree.

## Method

**Mechanical.** The trigger. The `.gitignore` entry. The branch deletions, because `git branch -d`
gives the merge check.

**Judgement.** Three decisions that the owner asked the agent to take. First, a tag before each
deletion that is not provably safe. A tag costs nothing and it makes each deletion reversible, so the
instruction "clean" needs no trade against the instruction "lose nothing". Second, `.claude/` enters
`.gitignore` rather than staying open as a question; the evidence was already in the entry. Third, the
draft ADR does not become a record, because ADR-0016 decides the same question and a second record
would give two answers to one question.

**Weakest.** A push on a branch with an open pull request runs the gate two times. A concurrency group
does not remove this, because the push event and the pull request event give different keys. The
alternatives are worse: a manual trigger returns the person to the loop, and a branch filter returns
the original defect. The duplicate stands until a run costs something.
