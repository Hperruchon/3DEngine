---
id: 0472
title: The rewrite of the history and the answers of the owner of 2026-11-26 are a record, with three new rows and their order
status: Done
phase: P0.190
opened: 2026-11-26
depends-on: [0471]
governed-by: []
writes:
  create:
    - tasks/TASK-0472-the-rewrite-of-the-history-and-the-answers-of-2026-11-26.md
    - docs/history-map-2026-11-26.txt
  modify:
    - tasks/TASK-0468-the-findings-of-the-review-of-2026-11-25.md
    - docs/INDEX.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - docs/CHARTER.md
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Api.Http/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0472 — The rewrite of the history and the answers of the owner of 2026-11-26 are a record, with three new rows and their order

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-25 found V238: a test held the folder of the owner on the computer of the owner, with the user
name of Windows, in a public repository. On 2026-11-26, after the merge of P21 (v0.454), the session gave the owner a list of
the open points that wait for the owner. The owner answered:

| Point | Answer of the owner |
|---|---|
| V238, the history of git | "rewrite the history". The session gave two facts: each commit already names the owner as its author, and the rewrite gives each later commit a new id. The owner answered "Rewrite anyway". |
| R-0056, the phone opens and saves no project file | "give the phone project files". |
| P21, ADR-0189 | "let's move on": ADR-0189 stays Accepted, with its fields and its order. |
| R-0080, the order of the collection | "task fixes": a task gives the snapshot and `CheckInterference` the order of the log. |
| R-0070, the sizes of the lamp | "measure what ?": the session gave the list of the measures; the owner did not give the sizes yet. |
| R-0059, the size of the window on the phone | "how do i install the app on my smartphone to test it ?": the session gave the steps of `docs/android-test-sheet.md`. The app writes no size of its window to the log, so a run cannot give the value yet. |

The same evening the owner gave a standing order: "can you work in autonomy till tomorrow morning ?", with the recommended
option of each question: merge each task after green continuous integration, the queue P0.190, P0.189 and then P11.0, and the
end at 09:00 on 2026-11-27.

**The rewrite.** The session did the rewrite in a mirror copy of the repository, not in the working copy of the owner:

1. `git fast-export --all --signed-commits=verbatim`. With no option, the export drops the signature of the first commit,
   which GitHub signed, and each later commit then gets a new id.
2. A Node.js filter replaced 28 texts: `Users\` and the user name in 16 places, `Users-` and the user name in one place, and
   the name as a part of a `Path.Combine` in 11 versions of `ProjectPathTests`. Each replacement is `owner`, which has the same
   length, so each block of the stream keeps its size.
3. `git fast-import` into a new repository. Checks: 1665 commits and 491 references, as before; 329 commits keep their id
   and 1336 get a new id; the author, the committer, the dates and the message of each commit are the same; one commit only
   changes by itself, the first commit that held the folder (2026-10-27), and each other new id follows from it; `git fsck`
   passes. On `main` the change is 10 lines in 5 files. `ProjectPathTests` and `WaitingWordTests` (29 tests) and
   `eng/repo-gates` pass on the new tip.
4. A push of the 436 heads that moved, in batches, with a lease on the old id of each head. GitHub refused none, and each
   head and tag on GitHub then agreed with the new copy. The 4 tags and the 10 references of the pull requests did not move:
   each one is older than 2026-10-27.

## Goal

The repository holds the record of the rewrite and a map from each old id to its new id, and the roadmap, the register and the
task of P0.189 hold the answers of 2026-11-26, so that a later session opens each new row with no question on its scope.

## Scope (in)

1. `docs/history-map-2026-11-26.txt`: the 1336 pairs of an old id and a new id, and a line in `docs/INDEX.md`.
2. The roadmap: row P0.190; the new rows R34 (the phone opens and saves a project file), R35 (the phone writes the size of its
   window and the time of a frame to the log) and K25 (the order of the log in the snapshot and in `CheckInterference`); a
   paragraph in the section "Order"; and a line in the section "Shipped".
3. The register: one progress line in each of R-0056, R-0059, R-0061, R-0070 and R-0080.
4. TASK-0468 (P0.189): V238 is done by the rewrite.
5. The ledger entry v0.455.

## Scope (out)

- Each code change. Each new row opens its task at its start, and its ADR when it changes the Document or the contract.
- The ids that the ledger, the tasks and the reviews cite. Each record is permanent; the map translates an old id.
- The commits that GitHub still keeps by their old id, and the runs of continuous integration of the old ids. Only GitHub can
  remove them.

## Acceptance criteria

- [x] Each answer of the table has a place in the roadmap, the register or a task, and the ledger entry names each place.
- [x] The map holds each commit whose id changed, and no version of a file in the new history holds the folder of the owner.
- [x] The new rows name the answer and the register entry that they close, and no row of track R, K, P or V names this task.
- [x] `docs/register.md` holds 13 open entries of 15, as before.
- [x] `AdrGateTests`, `TaskGovernanceGateTests`, `RegisterGateTests` and `MarkerGateTests` pass.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes. The local
  part passes; continuous integration is not run yet.

## Notes for the implementer

- `depends-on` names TASK-0471 (P21), the last task that `main` merged at v0.454.
- The section "Order" names K17 after P21. K17 is done since v0.427 (TASK-0443, ADR-0183). This is a false statement about the
  repository, so this task corrects it (`CLAUDE.md`, "Stop and ask").
- A branch or a worktree that a session made before the rewrite holds the old history. A merge of such a branch brings each
  old commit back. Make each new branch from `origin/main` after the rewrite.

## Outcome

Status: Done · commit (the merge of the branch p0190-owner-answers) · v0.455

## Method

Mechanical: the text of each answer in the row, the entry or the task; the map from the marks of the export and the import.
Judgement:  the rewrite of the public history on the order of the owner, after the session gave its cost; the numbers R34,
            R35 and K25; R35 as a row of its own, because a run on the phone needs it before R-0059 and R-0061 can close.
Weakest:    GitHub keeps each old commit by its id until GitHub removes the objects that no reference holds, and the clones
            and the worktrees on the computer of the owner keep the old history. The rewrite removes the folder from the
            branches, not from each copy.

## Progress

- 2026-11-26: opened on the answers of the owner and the standing order of the evening, branch `p0190-owner-answers` from
  `origin/main` at `a715d982` (v0.454, the tip of `main` after the rewrite).
- 2026-11-27: run 45940188942 of `439159b2` passed on each job. Closed at v0.455.
