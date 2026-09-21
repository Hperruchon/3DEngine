---
id: 0022
title: Make the write set of a task mechanical
status: Done
phase: P0.7
opened: 2026-09-21
depends-on: [0017]
governed-by: []
writes:
  create:
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - tasks/TASK-0022-write-set-gate.md
  modify:
    - .github/workflows/ci.yml
    - docs/templates.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - .gitignore
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0022 — Make the write set of a task mechanical

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0017 records this gap. Each task file declares a `writes` block with a create list, a
modify list and a forbid list. Nothing read that block. An agent could change a file that the task
forbids, and no check found the error.

TASK-0017 put this work outside its scope, because the check needs a step in continuous integration
and a decision about a local hook. The entry holds a limit of 2027-03-19.

`docs/templates.md` listed the gate in its table of twelve gates, and it described a hook before each
commit. That description did not agree with the repository, because neither the hook nor the gate
existed.

`docs/working-agreement.md` section 2.1 gives the rule: stay inside the write set. Section 8.1 depends
on it for two agents that work at the same time.

## Goal

A change that touches a file outside the write set of its task fails the build.

## Scope (in)

1. Add the gate in `Engine.Tests/Governance/`.
2. Add the job in continuous integration that gives the gate the list of changed paths.
3. Correct the description in `docs/templates.md`.
4. Close register entry R-0017.

## Scope (out)

- Do not add a hook before each commit. A hook needs an installation step on each computer, and a
  person can pass it with one flag. The gate gives the protection. The decision is recorded here.
- Do not add front matter to task files 0001 to 0013. Each one predates `docs/templates.md`. The gate
  exempts them with a budget that must not grow.
- Do not change code in a project other than `Engine.Tests`.

## Acceptance criteria

- [x] A test reads the `writes` block of each task file.
- [x] The gate fails when a changed path matches a forbid pattern.
- [x] The gate fails when a changed path is in no create list and in no modify list.
- [x] The gate fails when a change touches no task file.
- [x] The gate fails when a task declares a path that it both writes and forbids.
- [x] The gate fails when a task with the status `Done` names a file that it did not create.
- [x] The gate fails when a new task file carries no front matter.
- [x] Each check was verified by injection.
- [x] Continuous integration supplies the list of changed paths.
- [x] Register entry R-0017 is closed.

## Outcome

Shipped as v0.22. The test count went from 181 to 187.

**One implementation, two modes.** The gate is a test and not a script. Each static check always runs,
therefore `dotnet test` covers them on a local computer and on each of the three runners. The dynamic
check runs when the environment variable `WRITE_SET_FILES` gives a list of changed paths, one per
line. The new job `write-set-gate` sets that variable from `git diff --name-only`. The logic that
continuous integration uses is therefore the logic that the test suite covers.

**A forbid beats a permit.** A change can touch more than one task file. The gate takes the union of
each create list and each modify list, and it refuses a path that matches any forbid pattern of any of
those tasks. The strict reading is the safe one, because a forbid records a boundary and a permit
records an intention.

**A change with no task file fails.** `CLAUDE.md`, section "Anti-patterns", says: do not do work
outside the scope of the active task. The gate makes that rule real. Only a task that the same change
touches can govern that change, so a task from an older change cannot authorise a file today.

**Each of the seven checks was verified by injection.** The dynamic check ran four times: a legitimate
change passed; a change to `Engine.Contracts/Handlers/ICommandHandler.cs` reported the forbid pattern
and the task that owns it; a change to `docs/CHARTER.md` reported that no list names it; and a change
with no task file reported that no write set governs it. The three static checks each failed on a
deliberate violation and the message named the task and the path.

**The hook is refused, not deferred.** `docs/templates.md` described a hook before each commit. A hook
needs an installation step on each computer, and a person passes it with one flag. The gate runs where
nobody can pass it. The row in `docs/templates.md` now describes the gate.

**The gate found a defect on its first live use.** The first run against this task reported
`Engine.Api.Http/Properties/` as a change with no declaration, and it named the forbid pattern that
covers it. The file is `launchSettings.json`. The SDK generates it when a person runs the web host,
and it carries random port numbers. It was untracked and absent from `.gitignore`, therefore it
would appear in `git status` after each run and it would give a false difference on each computer.
The stash from TASK-0018 holds an older copy with different ports, which confirms the behaviour.
`.gitignore` now names it, and the write set of this task gained `.gitignore` to record that change.

## Method

**Mechanical.** The pattern matcher. Two stars match each character and one star matches each
character except the separator.

**Judgement.** Three decisions. First, a test and not a script, so that one implementation serves both
modes and the test suite covers the logic. Second, a forbid beats a permit across tasks. Third, a
change with no task file fails; the alternative was to pass such a change quietly, which would leave
the largest hole in the rule.

`RepositoryFiles.ReadFrontMatter` is flat and the `writes` block has two levels, therefore this gate
carries its own small reader for that one shape. A general YAML parser is not needed and is not
wanted.

**Weakest.** The rule that a change must touch a task file is strict. A correction of one word in a
document now needs a task file. The owner may find that cost too high. Two answers exist if that
happens: give the budget an exception list, or accept that a one-word correction belongs to the next
task that touches the same area. The second answer agrees with `CLAUDE.md` and it needs no code.

The second weak point: the gate reads the tasks that a change touches, and not a status. A change
that touches a task file with the status `Deferred` would use that write set. No such change exists
today, and a stricter rule needs evidence before it earns its cost.
