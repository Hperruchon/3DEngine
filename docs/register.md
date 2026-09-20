# Deferred register

**Status: Accepted** — 2026-09-20, TASK-0015. It replaces `open-questions.md`.

This document holds each known problem, each open question, each accepted compromise and each
temporary mechanism. Each entry has a date of entry and a date of expiry. No entry can stay here
without a limit.

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section
"Language".

## The reason for this document

The Blender design document of the year 2000 listed a broken event system, a user interface that
nobody could configure, and no undo function. The document planned a release 2.5 to correct these
problems. Release 2.5 became stable in April 2011. Eleven years passed between the record of the
problem and the repair.

A record of a problem is not progress. A list with no expiry date becomes longer.

This repository has the same condition today. `nuget.config` calls its folder feed an interim
bootstrap. `ArgParser.cs` says that a later task replaces it. The native build workflow says
"DRAFT — not yet executed", but it ran successfully months before. Two open questions from
2026-05-06 have no change.

## The rules

1. Each entry must have an `opened` date and a `due` date. The gate fails if a date is absent.
2. If the `due` date passes, the build fails. This result is the meaning of a limit. It is not a
   reminder. It is a stop.
3. There are four exits. You must use one exit to make the gate pass again:
   - **Resolve** — correct the problem. Record the commit, the task or the ADR.
   - **Decide** — the entry was a decision, not a question. Write an ADR. Close the entry and
     point to the ADR.
   - **Accept** — keep the problem permanently. Give a reason in writing. Move the entry to the
     section "Accepted compromises". The entry stops to age. This exit is correct behaviour. It is
     not a failure.
   - **Extend** — give a new date, one time only. Give the reason. The gate refuses a second
     extension. Then you must use one of the three exits above.
4. A new entry has a cost. The maximum quantity of open entries is **20**. You cannot add entry 21
   until you close an entry. Growth is the failure condition. This limit controls growth.
5. A temporary thing must have an entry. These words in tracked source or documentation must have a
   register identifier, for example `R-0007`: `TODO`, `HACK`, `interim`, `temporary`, `for now`,
   `bootstrap`, `DRAFT`. The gate fails if an identifier is absent. This rule finds the compromise
   that becomes permanent.
6. Do not close an entry to make the gate pass. Use one of the four exits above. Deletion of an
   entry destroys the register.

## Default lifetimes

Select the class. The class gives the date. Use a different date only with a reason.

| Class | Lifetime | Use for |
|---|---|---|
| `risk` | 30 days | A problem that can cause damage soon, or that costs time now |
| `question` | 90 days | A decision that you do not need now, but that you will need |
| `debt` | 180 days | A known compromise in code that shipped |
| `interim` | 365 days | A mechanism that is temporary by decision |

## Entry format

```
### R-0000 · One line. State the problem. Do not state the repair.
- class: risk | question | debt | interim
- opened: YYYY-MM-DD
- due: YYYY-MM-DD
- extended: no
- refs: path:line, TASK-nnnn, ADR-nnnn
Give the problem and its cost in two or three sentences.
Exit: state the condition that closes this entry.
```

---

## Open

### R-0002 · CI never ran on Windows or on macOS
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: `.github/workflows/ci.yml:11,29,57`
Each job uses `ubuntu-latest`. A person verified the native geometry path on win-x64 by hand only.
The desktop host ran on Windows only. The project requires continuous verification on three
operating systems.
Exit: the build, the tests and the headless smoke test pass on three runners.

### R-0005 · The native package has no licence notices
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: `nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg`, `ADR-0014 §5`
The repository contains a binary file of 4.4 MB. Its nuspec file has no licence element. The
repository has no third-party notices file. ADR-0014 and TASK-0012 do not give a licence. ADR-0014
§5 also requires a checksum, but no checksum exists.
Exit: a file `THIRD-PARTY-NOTICES.md` lists each component and its licence. Checksums exist.

### R-0007 · Native packages come from a local folder feed
- class: interim
- opened: 2026-07-04
- due: 2027-07-04
- extended: no
- refs: `nuget.config:9-11`, `nuget/.gitignore:1-4`, `TASK-0012 §6`
The configuration calls this feed an interim bootstrap. The publish step in the native workflow is
inactive. A person commits each package by hand.
Exit: a real feed exists. Or: accept this mechanism permanently and give the reason.

### R-0008 · The CLI argument parser is a placeholder
- class: interim
- opened: 2026-05-11
- due: 2027-05-11
- extended: no
- refs: `Engine.Cli/ArgParser.cs:3-5`
The parser reads `--param k=v` pairs. The file says that the wire-format task replaces it with
JSON input.
Exit: JSON input dispatch exists. Or: accept the parser permanently.

### R-0009 · The native build workflow says DRAFT
- class: debt
- opened: 2026-08-25
- due: 2026-10-24
- extended: no
- refs: `.github/workflows/build-manifold-native.yml:10`
The header says "DRAFT — not yet executed". The workflow ran successfully on the main branch, and
the repository contains its output. This comment gives incorrect information to the next reader.
Exit: the header agrees with the facts.

### R-0010 · The boundary document is not correct but the index calls it canonical
- class: debt
- opened: 2026-08-25
- due: 2026-10-24
- extended: no
- refs: `docs/architecture/engine-runtime-boundaries.md`, `docs/INDEX.md:5`
Nobody changed this document after the first ADR. It is older than ADR-0009 to ADR-0014. It gives a
`project.json` format that never existed. It says that Manifold is the V1 backend, but phase P7a
shipped a managed substitute. It lists CI gates that nobody built.
Exit: a person rewrites the document. Or: the document moves to an archive and `INDEX.md` points
to a different document.

### R-0011 · Two diagnostic codes have no source that emits them
- class: question
- opened: 2026-08-25
- due: 2026-11-23
- extended: no
- refs: `docs/diagnostics.md:17,26`
No inbound queue exists, therefore no code emits `E-CMD-BUS-BUSY`. The fallback backend removes
the need for `E-GEOM-BACKEND-INIT`. Reserved codes are correct. An unlimited quantity of reserved
codes makes the register unreliable.
Exit: a source emits each code. Or: each code becomes permanently reserved and gives a reason.

### R-0014 · The `.claude` directory is not in `.gitignore`
- class: question
- opened: 2026-05-06
- due: 2026-10-20
- extended: 2026-08-25 (imported from open-questions.md; the original lifetime had already passed)
- refs: this entry replaces OQ-0001
`git status` shows session metadata after each run.
Evidence 2026-09-20: the dependency direction gate must exclude this directory, because a worktree
under it holds a full copy of each project file. A stale copy shadowed the real one and the gate
reported a broken rule as satisfied.
Exit: `.gitignore` contains the directory. Or: the repository holds the directory by decision. Or:
accept the condition.

### R-0015 · The CLI escapes each apostrophe in JSON output
- class: question
- opened: 2026-05-06
- due: 2026-10-20
- extended: 2026-08-25 (imported from open-questions.md; the original lifetime had already passed)
- refs: this entry replaces OQ-0002
`JavaScriptEncoder.Default` writes each apostrophe as a Unicode escape. The output is correct JSON.
The tool `jq` reads it correctly. A person who reads the raw output sees noise.
`UnsafeRelaxedJsonEscaping` corrects this, but it escapes fewer characters.
Exit: the CLI uses the different encoder. Or: accept the output.

### R-0016 · Reduce the maximum quantity of open entries from 20 to 12
- class: question
- opened: 2026-08-27
- due: 2026-11-25
- extended: no
- refs: this file, rule 4
The limit is 20 to hold the entries from the architecture investigation. The limit controls growth.
A limit of 20 is too high to have an effect.
Exit: the limit becomes 12 after a person examines each imported entry. Or: 20 becomes the
permanent limit.

---

### R-0017 - No check verifies the write set of a task
- class: debt
- opened: 2026-09-20
- due: 2027-03-19
- extended: no
- refs: `docs/templates.md`, `tasks/TASK-0017-mechanical-governance.md:9-47`
Each task file declares a `writes` block with create, modify and forbid lists. Nothing reads that
block. An agent can change a forbidden file, and no check finds the error. The declaration is a
wish and not a rule.
Exit: a step in continuous integration compares the changed files against the write set of the
active task. A decision exists about a local hook.

## Accepted compromises

These are conditions that the project keeps permanently and by decision. They do not age.

_No entries._

---

## Closed

The register keeps each closed entry. Do not delete an entry. Move it here and give the outcome.

Use one of these three lines:

```
Closed YYYY-MM-DD · resolved · commit <hash>
Closed YYYY-MM-DD · decided  · ADR-nnnn
Closed YYYY-MM-DD · accepted · the reason to keep the condition
```

### R-0001 · The preview SDK version prevents a build on a new computer
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: `global.json:3-5`, `3DEngine/Documentation/Running.md:7`
`global.json` sets `rollForward` to `disable` for a preview SDK. A public feed does not supply
that SDK. A computer with only release SDKs cannot build the solution.
Exit: `global.json` gives a release version and uses `latestFeature`. Running.md agrees with
`global.json`.
Closed 2026-09-20 - resolved - TASK-0014, v0.16

### R-0003 · The replay determinism gate uses a different set of handlers
- class: risk
- opened: 2026-08-26
- due: 2026-09-25
- extended: no
- refs: `Engine.Tests/ReplayDeterminism/`, `Engine.Cli/Cli.cs:240-260`, `Engine.Api.Http/EngineHost.cs:36-50`
Seven positions in the solution register handlers. Therefore the determinism gate does not use the
set of handlers that the hosts use. The architecture depends on this one test.
Exit: the hosts and the tests use one handler catalog.
Closed 2026-09-20 - resolved - TASK-0016, ADR-0016, v0.18



### R-0004 · The dependency direction gate does not exist
- class: debt
- opened: 2026-08-25
- due: 2026-11-23
- extended: no
- refs: `CLAUDE.md`, `.github/PULL_REQUEST_TEMPLATE.md:16`, `docs/adr/0009-3dengine-core-peer-render-kernel.md:109`
`CLAUDE.md` lists this gate. No CI step examines project references. No test examines project
references. An agent in an unfamiliar area can break this rule, and no check finds the error.
Exit: a test reads each project file and verifies the rules. `dotnet test` runs the test.
Closed 2026-09-20 - resolved - TASK-0017, v0.19

### R-0006 · The ADR index does not record supersession or amendment
- class: debt
- opened: 2026-08-25
- due: 2026-10-24
- extended: no
- refs: `docs/adr/README.md:19,27`, `docs/adr/0004-engine-runtime-is-authority.md:38`, `docs/adr/0011-server-default-deployment-topology.md:23-25`, `docs/adr/0012-geometry-backend-wiring.md:193`
ADR-0011 contradicts the deployment default in ADR-0004, but the index gives no note. ADR-0012
contains Amendment 1, but the index does not give it. A person maintains this index by hand, and
it is now incorrect.
Exit: each ADR has front matter with reciprocal supersession. A tool generates the index. CI
verifies the index.
Closed 2026-09-20 - resolved - TASK-0017, v0.19
Note on the exit. The exit line asked for a tool that generates the index. The work used a test
that compares the index against the front matter instead. The protection is the same: drift fails
the build. The cost is much lower. TASK-0017, section Scope (out), records this decision.

### R-0012 · A branch contains duplicate identifiers
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: branch `claude/happy-booth-1cef3f`
This branch contains a different ADR-0014, a different TASK-0012, a different TASK-0013 and a
different version v0.12. The main branch uses the same identifiers for different work. The branch
also contains a working engine hosting factory that nobody merged. The reference "ADR-0014" is
therefore not clear.
Exit: a person keeps the useful work and gives it new numbers. Or: a person records that the
project abandons the branch.
Closed 2026-09-20 - resolved - TASK-0018, v0.20
The project keeps the useful work and gives it new numbers. The hosting factory is merged, in an
adapted form. The persistence record is ADR-0015. The persistence task is TASK-0019. The duplicate
v0.12 label is recorded in the v0.20 ledger entry. The branch itself stays, because the owner
decides when to remove it.

### R-0013 · The renderer proposal exists only in a stash
- class: risk
- opened: 2026-08-25
- due: 2026-10-20
- extended: 2026-09-20 (milestone P0.5 salvages the branch and the stash together)
- refs: `stash@{0}`, file `docs/proposals/render-host-direction.md`
A large analysis of options exists in a stash. No commit contains it. The analysis disappears if a
person removes the stash.
Exit: a commit contains the file in `docs/proposals/`. Or: a person removes the analysis by
decision.
Closed 2026-09-20 - resolved - TASK-0018, v0.20
A commit holds `docs/proposals/render-host-direction.md`. The file came from the third parent of
the stash commit, where git keeps an untracked file. A header note records that two statements in
the analysis are out of date.
