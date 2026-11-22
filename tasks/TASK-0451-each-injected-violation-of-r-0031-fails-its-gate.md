---
id: 0451
title: Each injected violation of the second and the third codebase reviews fails its gate
status: Done
phase: P0.181
opened: 2026-11-21
depends-on: [0449]
governed-by: []
writes:
  create:
    - tasks/TASK-0451-each-injected-violation-of-r-0031-fails-its-gate.md
  modify:
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/MarkerGateTests.cs
    - Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
    - eng/test-floor.txt
    - docs/adr/0016-handler-declared-construction.md
    - docs/templates.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0451 — Each injected violation of the second and the third codebase reviews fails its gate

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0031 ("A gate can pass while its rule is broken", class debt) is due on 2026-12-24. Its exit is "each
injected violation of the second review and of the third review fails its gate". The second review is
`docs/reviews/2026-09-30-codebase-review.md` (findings T3 to T7). The third review is
`docs/reviews/2026-10-04-codebase-review.md` (its lines on T3 to T7, and T9, T10, T11 and T13). The last progress line of
R-0031 (2026-11-10) said: "Open: T4, the dispatch part. The entry closes when the dispatch gate reads the desktop host."
Many tasks changed the gates after the reviews, so this task runs each injection again on the gates of today.

## Goal

Each injected violation of the second and the third codebase reviews fails its gate, and R-0031 closes.

## Scope (in)

1. The list of each violation that the two reviews injected or observed in a gate, as a form that a person can apply.
2. A script that applies each form, one at a time, runs the gate, records the result and restores each file byte for byte.
3. A stronger gate, with a test of the gate, for each form that passes its gate.
4. The close of R-0031 when each form fails its gate.

## Scope (out)

- The parts of R-0031 that the owner accepted on 2026-11-10 (the clock of the register gate, the case of a path on Windows,
  the provenance of the native binaries, the local mode of the write-set check, and the three gates that run engine code in
  the test process). They are conditions and not injections.
- Other defects of the gates that this task found (see "Decisions").
- `3DEngine.App`: it is the client of the person, not a host, and this task forbids it.

## Acceptance criteria

- [x] Each form of the table in "Method" fails its gate after the task, and the table says which forms passed before it.
- [x] Each strengthened gate has a test of its new rule, and its injection failed the gate of the base commit first.
- [x] R-0031 is closed with the exit "resolved".
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- The dispatch gate takes its hosts from the classes of `DependencyDirectionGateTests` (`DispatchHosts`: each headless
  client, the surface library and each host that draws), as the review recommended. A new project must be in one class, so a
  new host is read with no list of its own. The application `3DEngine.App` is not in the list: ADR-0072 makes it the library
  of the scene, the user interface and the actions, and it names the command that a control of the window sends (for example
  `ProposedSheet.CreateSheet` and the words of `HistorySteps.KindText`). That is the work of a client and not a dispatch.
  ADR-0016 gets a line in "History".
- The folder of a host is the folder of its tracked project file, because `BookcaseCompare` is in `eng/bookcase-compare`.
- The marker gate reads each tracked file with the extension `.js`, `.mjs`, `.cjs`, `.sh`, `.ps1`, `.cmd`, `.bat`, `.csproj`,
  `.props`, `.targets`, `.vert`, `.frag` or `.comp`, from the list of git, so a folder that git ignores (for example a build of a
  native library) is not read. The root `.gitignore` stays out: its line "Visual Studio temporary files" names files, and is no
  declaration of impermanence. No tracked file of these kinds holds a marker today.
- The review gate requires a value in the second column of each required measure row. Each review in `docs/reviews/` gives
  one.
- `DependencyDirectionGateTests` changes only the access of `TrackedFiles` and the new list `DispatchHosts`; no rule of that
  gate changes, so the ADRs that it enforces get no line.
- The form T7 "archive" of the second review was a false failure, not a pass: the check of a supersession read `docs/adr/` only,
  so the archived ADR-0011 was "absent". The baseline of today holds that pair and passes. The table injects the opposite form
  instead: a supersession of a number that is neither in `docs/adr/` nor archived.
- Defects found and not corrected (outside the exit of R-0031): the marker gate takes the window of a match from line i - 2 to
  line i, but for the first two lines of a file the window also holds the lines below, so a marker on line 1 or 2 can borrow
  the identifier of a later line. The ADR gate does not read the front matter of an archived record, so an archived record can
  lose its `superseded-by` with no failure; `AdrGateTests` says that this is the design.

## Outcome

Status: Done · commit (the merge of the branch p0181-gates-hold-their-rules) · v0.435

## Method

Mechanical: the rows of the roadmap and the ledger, the lines of the register and of the table of gates in
`docs/templates.md`, and the counts of `eng/test-floor.txt`.

Judgement: see "Decisions".

Weakest: the list of forms. It holds each form that the two reviews name as a run or as a line of code of a gate. A form
that a review described only in words (T6, a failed git command in a job of the workflow, and T13 part 1, the comment about
`git diff-tree --cc`) has no injection: T6 needs a run of the workflow, and each job of `.github/workflows/ci.yml` now sets
`shell: bash`; T13 part 1 is a comment, and the judge now compares a merge with the automatic merge of its parents (TASK-0066).

The script `inject0451.js` (in `%TEMP%\p0181`, not in the repository, because no gate test needs it) applies each form to a
clone of the base commit `28dcc1c9` in `%TEMP%\p0181\repo`, or for the dispatch gate to the worktree, because that test runs
engine code and reads the sources of its own checkout. It runs `eng/repo-gates` (`--root` on the clone), `eng/write-set-check`
(in the clone, after the git operations of the form) or `dotnet test --filter DispatchSurfaceGateTests`, each with no shell.
"Before" is the build of the base commit, kept in `%TEMP%\p0181\before`; "after" is the build of this branch. After each form
the script restores each file byte for byte, compares its sha256, and checks that the clone is at the base commit with a clean
status. A form is caught when the gate fails and its output names the expected check.

| Form | Review | Violation | Gate | Caught before | Caught after |
|---|---|---|---|---|---|
| T3a | 2026-09-30 | a change of its own inside a merge commit | `eng/write-set-check` | yes | yes |
| T3b | 2026-09-30 | a commit that names a Done task (TASK-0026) and moves the cut-off | `eng/write-set-check` | yes | yes |
| T3c | 2026-09-30 | a wide permit (Engine.Tests/**) and a change to RegisterGateTests.cs | `eng/write-set-check` | yes | yes |
| T4-dep | 2026-09-30 | Engine.Core.csproj: one Include with two paths, the first 3DEngine.Core | `DependencyDirectionGateTests` | yes | yes |
| T4-det-a | 2026-09-30 | double.Pow(2, 3) in Engine.Core | `DeterminismCallGateTests` | yes | yes |
| T4-det-b | 2026-09-30 | Sin(1) after using static System.Math | `DeterminismCallGateTests` | yes | yes |
| T4-det-c | 2026-09-30 | Math.Pow with the parenthesis on the next line | `DeterminismCallGateTests` | yes | yes |
| T4-mark-a | 2026-09-30 | // TODO R-9999 (no such entry) | `MarkerGateTests` | yes | yes |
| T4-mark-b | 2026-09-30 | nuget/.gitignore says "Interim bootstrap" with no identifier | `MarkerGateTests` | yes | yes |
| T4-disp-a | 2026-09-30 | the desktop host 3DEngine names a command | `DispatchSurfaceGateTests` | no | yes |
| T4-disp-b | 2026-09-30 | the third host project (Engine.Api.Http.Server) names a command | `DispatchSurfaceGateTests` | yes | yes |
| T4-disp-c | 2026-09-30 | a later host, the phone program 3DEngine.Android, names a command | `DispatchSurfaceGateTests` | no | yes |
| T5-status | 2026-09-30 | TASK-0034 with status: ready and governed-by: [] | `TaskGovernanceGateTests` | yes | yes |
| T5-fence | 2026-09-30 | the register with its closing code fence removed and R-0027 overdue | `MarkerGateTests`, `RegisterGateTests` | yes | yes |
| T5-affects-a | 2026-09-30 | ADR-0016 affects with quotes, and TASK-0449 without 0016 | `TaskGovernanceGateTests` | yes | yes |
| T5-affects-b | 2026-09-30 | ADR-0016 affects as an inline list, and TASK-0449 without 0016 | `TaskGovernanceGateTests` | yes | yes |
| T7-index | 2026-09-30 | the column "Amended by" of ADR-0006 emptied in the ADR index | `AdrGateTests` | yes | yes |
| T7-archive | 2026-09-30 | ADR-0019 supersedes 0999, a number that is neither in docs/adr/ nor archived | `AdrGateTests` | yes | yes |
| T7-review-a | 2026-09-30 | a measure row of the last review with an empty value | `CodebaseReviewGateTests` | no | yes |
| T7-review-b | 2026-09-30 | previous: none in a review that is not the first | `CodebaseReviewGateTests` | yes | yes |
| T3-done | 2026-10-04 | the form of commit 4715b88: a test changed under TASK-0034, which is Done | `eng/write-set-check` | yes | yes |
| T4-det-d | 2026-10-04 | an alias of System.Math: M.Pow(2, 3) | `DeterminismCallGateTests` | yes | yes |
| T4-det-e | 2026-10-04 | a line break before .Pow | `DeterminismCallGateTests` | yes | yes |
| T4-det-f | 2026-10-04 | a "//" inside a string before Math.Pow on the same line | `DeterminismCallGateTests` | yes | yes |
| T4-det-g | 2026-10-04 | a generic T.Sin | `DeterminismCallGateTests` | yes | yes |
| T13-mark-a | 2026-10-04 | // TODO R-0016 (a closed entry) | `MarkerGateTests` | yes | yes |
| T13-mark-b | 2026-10-04 | // TODO R-0000 (the template) | `MarkerGateTests` | yes | yes |
| T13-mark-c | 2026-10-04 | a TODO with no identifier in a script of eng/ (the gate read .cs and four configuration files only) | `MarkerGateTests` | no | yes |
| T13-mark-d | 2026-10-04 | a TODO with no identifier in a project file | `MarkerGateTests` | no | yes |
| T5-status-3 | 2026-10-04 | a wrong status (status: ready) in TASK-0034, by a review agent | `TaskGovernanceGateTests` | yes | yes |
| T9a | 2026-10-04 | Engine.Tests/Engine.Tests.csproj changed under a task that permits Engine.Tests/** | `eng/write-set-check` | yes | yes |
| T9b-ref | 2026-10-04 | Engine.Core/Directory.Build.props with a reference to 3DEngine.Core | `DependencyDirectionGateTests` | yes | yes |
| T9b-x86 | 2026-10-04 | Engine.Core/Directory.Build.props with the runtime identifier win-x86 | `DeterminismCallGateTests` | yes | yes |
| T10 | 2026-10-04 | R-0027 overdue and the next heading as #### R-0028 | `RegisterGateTests` | yes | yes |
| T11 | 2026-10-04 | a later commit of one push widens the write set that judges an earlier commit | `eng/write-set-check` | yes | yes |

The result: 30 of 35 forms failed their gate before this task, and 35 of 35 after it. The five forms that passed before are
T4-disp-a, T4-disp-c, T7-review-a, T13-mark-c and T13-mark-d. Each one failed the gate of the base commit after the change
only, and its new test holds the rule. A first run of the script gave two more passes, T4-det-b and T7-archive. T4-det-b
passed because a second run of the script reset the clone while the first used it; the script now leaves the clone alone in a
run of the dispatch forms only, and the full run above gave each form its own state. T7-archive had a wrong form at first (see
"Decisions").

## Progress

- 2026-11-21: branch `p0181-gates-hold-their-rules` from `28dcc1c9` (P0.180, TASK-0449, v0.434, not merged yet).
- 2026-11-22: run 44962984251 of `a4e957e7` passed on each job. Closed at v0.435.
