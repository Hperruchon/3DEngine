---
date: 2026-09-25
commit: 3c7ced9
ledger: v0.33
branch: rules-review-2026-09-25
kind: rules-review
---

# Rules review — 2026-09-25

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This review examines each rule and the organization of the repository. It proposes a smaller set of
rules that protects the same things. The owner approves each change before a session makes it. This
file changes no rule.

The codebase review gate reads only names that end in `-codebase-review.md`
(`Engine.Tests/Governance/CodebaseReviewGateTests.cs:24-25`). It does not read this file.

Two tests from the owner apply to each proposal:

- **The bottleneck test.** "When a process makes me the bottleneck, fix the process."
- **The question test.** "Prefer an answer that removes a recurring question."

Each statement carries one label: **[Observed]** for a fact read on disk in this session,
**[Inferred]** for a conclusion from observed facts, **[Recommended]** for a proposal.

## Summary

**[Observed]** Thirteen files hold rules. They hold 2,971 lines. Fifty-two sentences use "must" and
eighty use "do not". The inventory in section 2 lists 124 rules. A gate enforces 30 of them in full
and 24 in part. Seventy rules are text only.

**[Observed]** The rules that cost the most are the duplicates. `CLAUDE.md` repeats the charter, the
dependency gate, the glossary and the repository map. `docs/working-agreement.md` repeats
`CLAUDE.md` and `docs/templates.md`. `docs/templates.md` repeats `docs/register.md`. A new session
reads eighteen files to find its position for the next task.

**[Observed]** Fourteen statements in the rule files describe a repository that no longer exists.
Five rules contradict the practice that the ledger records. Five gates read a fixed list that a new
file does not enter.

**[Recommended]** The proposal keeps each protection and removes three files. `CLAUDE.md` becomes a
position file of about 120 lines. Five small gates and five gate corrections give ten rules a
mechanism. Thirteen questions in
section 6 need the owner. Each one has a recommendation. Sections 4.0 and 4.6 to 4.8 answer the four
directions that the owner gave after the first summary.

## 1 Measures, today

| Measure | Value | Method |
|---|---|---|
| Rule files | 13 | The files in the first column of the table below, without the three CI files and the ledger. |
| Lines in the rule files and the CI files | 2,971 | `wc -l` on the sixteen files below. |
| Sentences with "must" | 52 | `grep -o -w -i must <file> \| wc -l` on the thirteen rule files. |
| Sentences with "do not" | 80 | `grep -o -i -E '\bdo not\b' <file> \| wc -l` on the same files. |
| Rules in the inventory | 124 | Rows in section 2. One row is one instruction that an agent can obey or break. |
| Rules that a gate enforces in full | 30 | Rows in section 2 whose column "Gate" starts with a test name or a job name. |
| Rules that a gate enforces in part | 24 | Rows whose column "Gate" starts with `partial`. |
| Rules in text only | 70 | Rows whose column "Gate" starts with `none`. |
| Files that a session reads to find its position | 18 | See the method below the table. |
| Open register entries | 10 of 15 | Section "Open" of `docs/register.md:76-211`: R-0007, R-0018 to R-0026. |
| Gate classes | 13 | Test classes whose name ends in `GateTests`. Same method as the review of 2026-09-23. |
| Tests | 206 | `dotnet test 3DEngine.sln --no-build`: 206 passed, 0 failed, 0 skipped, exit code 0. |
| Build warnings, clean build | 0 | `dotnet build 3DEngine.sln --no-incremental`: 0 warnings, 0 errors, exit code 0. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19. Proposed: 1 (0015). Withdrawn: 1 (0003). |
| ADRs in force with no enforcement | 1 | ADR-0007. The budget in `AdrGateTests.cs:40` is 1. |
| ADRs in force whose `enforced-by` names an absent file | 4 | ADR-0018 to ADR-0021. Checked with a shell loop over each `enforced-by` value. |
| Task files | 38 | `tasks/`. With front matter: 25. Ready: 6. Done: 31. Deferred: 1. |
| Ready tasks whose `governed-by` equals the rule of `docs/templates.md:173` | 6 of 6 | The script `governed-by.js`, section 3.4, finding F23. |

Line count of each file:

| File | Lines |
|---|---|
| `CLAUDE.md` | 260 |
| `docs/CHARTER.md` | 281 |
| `docs/working-agreement.md` | 168 |
| `docs/conventions.md` | 79 |
| `docs/templates.md` | 299 |
| `docs/INDEX.md` | 53 |
| `docs/glossary.md` | 51 |
| `docs/open-questions.md` | 13 |
| `docs/register.md` | 482 |
| `docs/diagnostics.md` | 58 |
| `docs/roadmap.md` | 140 |
| `docs/adr/README.md` | 57 |
| `.github/PULL_REQUEST_TEMPLATE.md` | 23 |
| `docs/CURRENT-STATE.md` (the ledger) | 804 |
| `.github/workflows/ci.yml` | 190 |
| `eng/write-set-cutoff.txt` | 13 |
| Total | 2,971 |

Other sizes: the 21 ADRs hold 2,639 lines. The 38 task files hold 5,553 lines. The 13 gate classes
and the two helpers hold 2,388 lines.

**Method for "files to find position".** Follow `CLAUDE.md:215-219` for the next task. Count each
file that a rule tells the session to read.

1. `CLAUDE.md` (`CLAUDE.md:217`).
2. `docs/CURRENT-STATE.md`, the last entry (`CLAUDE.md:218`).
3. `docs/roadmap.md`, because six tasks have the status `Ready` and only `roadmap.md:125-132` gives
   their order.
4. `tasks/TASK-0034-document-session.md`, the first Ready task in that order.
5. `docs/CHARTER.md`, for the scope test (`CLAUDE.md:27-28`).
6. `docs/working-agreement.md` (`CLAUDE.md:23`).
7. to 18. The twelve ADRs in `governed-by` of TASK-0034 (`docs/working-agreement.md:98-99`).

The count is 18. `CLAUDE.md:235` says: examine the plan again when "a session read more than five
files to find its position". The rules themselves trip this condition on each Ready task.

## 2 Inventory

One row is one rule. The column "Gate" names the test class or the CI job that fails when the rule
breaks, or "none". A cell that starts with `partial:` names a gate that covers part of the rule. The
column "Serves" names the objective or the anti-objective, or the ADR, or
"none". The column "Cost" gives the cost in plain words. The column "Proposal" gives the target:
**kept**, **merged** (into the named file), **gate** (a test replaces the text), **free text** (kept
as guidance, not as a rule) or **deleted**.

Objective numbers refer to `docs/CHARTER.md:53-96`. Anti-objective numbers refer to
`docs/CHARTER.md:194-254`.

### 2.1 `CLAUDE.md`

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| C1 | Write documentation and replies in Simplified Technical English. Eleven sub-rules. | `CLAUDE.md:7-21` | none | objectives 5, 12 | Each reply and each document. A check by eye. | kept |
| C2 | Navigation: eleven files, each with a purpose. | `CLAUDE.md:25-43` | none | objective 12 | Repeats `docs/INDEX.md:25-43`. | merged into `docs/INDEX.md` |
| C3 | Find the minimum information. Do not read all ADRs or tasks. | `CLAUDE.md:45` | none | objective 12 | none | kept |
| C4 | Read order: charter, CLAUDE.md, the ADR, the ledger. | `CLAUDE.md:47-52` | none | objective 12 | Repeats `docs/CHARTER.md:279-281`. | kept; the charter copy is the owner's |
| C5 | The authority diagram. The two kernels never reference each other. | `CLAUDE.md:54-79` | `DependencyDirectionGateTests` | anti-objective 6 | Repeats ADR-0009. `docs/INDEX.md:5` points here. | kept |
| C6 | The deployment topology, target and today. | `CLAUDE.md:81-102` | none | anti-objective 8 | Twenty lines that repeat ADR-0019 and ADR-0011. | free text; one paragraph that points at ADR-0019 |
| C7 | Embedded mode permits one client, no observers, no state between invocations. | `CLAUDE.md:98-100` | none | anti-objective 8 | Repeats ADR-0011. | deleted; ADR-0011 holds it |
| C8 | Engine code knows nothing about HTTP and processes. | `CLAUDE.md:102` | `DependencyDirectionGateTests` (Engine.Core references only Engine.Contracts) | anti-objective 6 | small | merged into C5 |
| C9 | Dependency rules, ten bullets. | `CLAUDE.md:104-120` | partial: `DependencyDirectionGateTests`, twelve tests; bullet 9 (composition root only) has none | anti-objective 6 | Fifteen lines that repeat the gate, ADR-0009, ADR-0014 and ADR-0017. | gate; one sentence names the test |
| C10 | Do not change `3DEngine/` unless a task gives that scope. | `CLAUDE.md:122-126` | `WriteSetGateTests` | objective 5 | Repeats `docs/INDEX.md:45-49`. | deleted; the gate is the rule |
| C11 | The triad. Each change uses a command. A query must not change state. | `CLAUDE.md:128-137` | none | anti-objectives 2, 3 | Repeats `docs/glossary.md:9-11` and ADR-0008. | merged into `docs/glossary.md` |
| C12 | Scope clamps: none is active. Do not add a non-goal without an ADR and a task. | `CLAUDE.md:139-147` | none | charter non-goals | A section that says nothing is active. Repeats `docs/CHARTER.md:166-169`. | deleted |
| C13 | Determinism rules 1 to 6. | `CLAUDE.md:149-169` | none | anti-objective 16, ADR-0007 | The list is the value. No gate scans for a forbidden call. | kept in force; new gate for rules 1, 3, 4, 5 (question Q10) |
| C14 | Geometry kernel rules 7 to 9. | `CLAUDE.md:171-180` | none | anti-objectives 13, 14, 5 | Repeats `docs/CHARTER.md:211-213` and `:236-243`. | deleted; the charter holds each one |
| C15 | Register each diagnostic code in the same change. Add only. Each code has a namespace. | `CLAUDE.md:182-185` | partial: `DiagnosticsRegistryGateTests` (three of five engine projects), `DiagnosticsReserveGateTests` | ADR-0008 §4 | "Add only" blocks TASK-0036 (`tasks/TASK-0036-honest-geometry-backend.md:92-93`). | kept; "add only" becomes "do not remove a code, do not change its meaning" |
| C16 | Stop and ask: five conditions. | `CLAUDE.md:187-196` | partial: `contract-gate` for condition 1, on a pull request only; none for 2 to 5 | anti-objectives 1, 6 | Condition 5 forbids a correction that the owner then approves (ledger v0.21, `docs/CURRENT-STATE.md:343-389`). | kept; condition 5 says "report both; the owner decides" |
| C17 | Run narrow tests while you work. Do not run the gate on your computer. | `CLAUDE.md:198-203` | none | objective 12 | Contradicts practice. Each ledger entry since v0.19 reports a local full test run (`docs/CURRENT-STATE.md:755`, `:785`, `:803`). The write-set check runs locally (`docs/templates.md:174`). | rewritten to match practice (question Q8) |
| C18 | The gate runs five checks. | `CLAUDE.md:205-211` | `.github/workflows/ci.yml` | objective 8 | Stale. Thirteen gate classes, three jobs and two smoke tests exist. | deleted; one line points at `ci.yml` |
| C19 | A session starts with CLAUDE.md, the last ledger entry, the next Ready task. | `CLAUDE.md:213-219` | none | objective 12 | "Next Ready task" is undefined with six Ready tasks. | kept; "in roadmap order, with each `depends-on` Done" |
| C20 | A codebase review is due before the seventh milestone. | `CLAUDE.md:221-222` | `CodebaseReviewGateTests` | objective 12 | Each milestone counts, also one that changes documents only. | kept (question Q5) |
| C21 | A session ends only in five conditions. | `CLAUDE.md:224-230` | partial: CI for build and tests, `RegisterGateTests`, `DiagnosticsRegistryGateTests`; none for the ledger entry and the task status | objective 12 | Repeats `docs/templates.md:196-213`. | kept; templates section 5 deleted |
| C22 | Examine the plan again: six conditions. | `CLAUDE.md:232-240` | none | objective 12 | No check reads them. Condition 2 trips on each Ready task (section 1). | deleted, except the owner-report condition |
| C23 | Do not put business logic in `3DEngine/`. | `CLAUDE.md:246` | none | anti-objective 2 | Repeats `docs/CHARTER.md:100`. | deleted |
| C24 | Do not bypass the CommandBus. | `CLAUDE.md:247` | none | anti-objective 2 | Repeats the anti-objective. | deleted |
| C25 | Do not treat a `3DEngine.Core` object as the authority. | `CLAUDE.md:248` | none | anti-objective 1 | Repeats the anti-objective. | deleted |
| C26 | Do not add a diagnostic code without a register entry. | `CLAUDE.md:249` | `DiagnosticsRegistryGateTests` | ADR-0008 | Repeats C15. "Register entry" is ambiguous: `docs/register.md` or `docs/diagnostics.md`. | deleted; C15 holds it |
| C27 | Do not add an abstraction for a requirement that does not exist. The cost test. | `CLAUDE.md:250-253` | none | objectives 12, 17 | judgement | free text, one bullet |
| C28 | Do not do work outside the scope of the active task. | `CLAUDE.md:254` | `WriteSetGateTests` | objective 12 | Repeats W2. | kept; one line names the gate |
| C29 | Do not put a host projection in the design boundary. | `CLAUDE.md:255-256` | none; ADR-0007 is unenforced | anti-objective 1 | Repeats `docs/CHARTER.md:23-25`. | deleted |
| C30 | A new command changes only its own files and `HandlerCatalog.cs`. | `CLAUDE.md:258-260` | partial: `DispatchSurfaceGateTests`, five fixed files | ADR-0016 | The gate does not read a new host file. | gate; the list becomes a scan of each host project |

### 2.2 `docs/CHARTER.md`

The charter is the owner's document. This review lists its rules and proposes no change to its
text. Two questions in section 6 touch it.

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| H1 | Mission. Each change to design truth uses a command. | `docs/CHARTER.md:8-28` | none | anti-objective 2 | none | kept |
| H2 | Twenty objectives. A change needs the owner. | `docs/CHARTER.md:45-96` | `ObjectiveReferenceGateTests` | all | none | kept |
| H3 | No consumer holds business logic. | `docs/CHARTER.md:100` | none | anti-objective 2 | judgement | kept |
| H4 | `Engine.Api.Http` binds to localhost only. | `docs/CHARTER.md:106-107` | none | non-goal "bind" | No test reads the bind address. | kept |
| H5 | A host that draws owns ephemeral state only. | `docs/CHARTER.md:108-110` | none; ADR-0007 is unenforced | anti-objective 1 | judgement | kept |
| H6 | A handler reaches geometry only through a capability. | `docs/CHARTER.md:111-112` | none | anti-objective 9 | judgement | kept |
| H7 | "V1 and V1.x are complete." | `docs/CHARTER.md:118` | none | none | Two labels that no current file defines. | question Q9 |
| H8 | The feature boundary is closed. A change needs an ADR. | `docs/CHARTER.md:142-162` | none | anti-objectives 11 to 14 | judgement | kept |
| H9 | Non-goals: fourteen items. An absent item is a non-goal. | `docs/CHARTER.md:164-187` | none | none | judgement | kept |
| H10 | Anti-objectives 1 to 16. | `docs/CHARTER.md:189-254` | partial: 4: `CommandBusTests` (ADR-0006); 6: `DependencyDirectionGateTests`; 16: `ReplayDeterminismGateTests`, one platform (R-0023); others none | all | Anti-objective 10 conflicts with the architecture challenge (question Q6). | kept |
| H11 | The scope test, four steps. | `docs/CHARTER.md:256-277` | none | all | Step 4 repeats `CLAUDE.md:187-196`. | kept |
| H12 | Rule of thumb: the read order. | `docs/CHARTER.md:279-281` | none | objective 12 | Repeats `CLAUDE.md:47-52`. | kept |

### 2.3 `docs/working-agreement.md`

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| W1 | The quality limit is higher for an agent. The owner must understand the work. | `docs/working-agreement.md:18-28` | none | objectives 5, 10 | A principle, not a rule. | free text; one sentence in `CLAUDE.md` |
| W2 | 2.1 Stay inside the write-set. Stop if you must change another file. | `:34-36` | `WriteSetGateTests` | objective 12 | Repeats C28. | deleted; the gate holds it |
| W3 | 2.2 Do not improve a thing that nobody asked for. Add a register entry. | `:38-40` | none | objective 12 | judgement; useful | merged into `CLAUDE.md` |
| W4 | 2.3 One topic in one change. | `:42` | none | objective 12 | Repeats `docs/templates.md:192`. | merged into templates section 4 |
| W5 | 2.4 Add a file instead of a change to a shared file. No dispatch switch. | `:44-45` | `DispatchSurfaceGateTests` | ADR-0016 | Repeats C30. | deleted; the gate holds it |
| W6 | 3.1 Do not change the text of an approved ADR. A status field may change. | `:51-54` | none | objective 17 | Conflicts with `docs/templates.md:107-108` and with practice (finding F10). | merged into templates section 2 (question Q3) |
| W7 | 3.2 Do not change ledger history. Add a new entry. | `:56-57` | none | objective 12 | none | merged into `CLAUDE.md`, one line |
| W8 | 3.3 Do not close a register entry to make a gate pass. | `:59-60` | none | objective 12 | Repeats `docs/register.md:47-48`. | deleted; register rule 6 holds it |
| W9 | 3.4 Documentation and code disagree: stop, report both. | `:62-64` | none | objective 17 | Repeats `CLAUDE.md:196`. | deleted; C16 holds it |
| W10 | 4.1 Report what you did not verify. | `:70-71` | none | objective 12 | none | merged into `CLAUDE.md` |
| W11 | 4.2 Narrow tests are not sufficient. | `:73-74` | CI | objective 8 | Repeats C17. | deleted |
| W12 | 4.3 The Method block: mechanical, judgement, weakest. | `:76-83` | none | objective 12 | Repeats `docs/templates.md:159-166`. | deleted; the task form holds it |
| W13 | 4.4 Put each assumption in the register. | `:85-86` | none | objective 12 | Repeats C21, condition 5. | deleted |
| W14 | 4.5 Do not invent a reference. | `:88-89` | partial: `ObjectiveReferenceGateTests` for objective numbers | objective 12 | none | merged into `CLAUDE.md`: "read a file before you cite it" |
| W15 | 5.1 Give the ADR number in the code. | `:95-96` | none | objective 12 | Repeats `docs/conventions.md:70-79`. | merged into templates, section "Code conventions" |
| W16 | 5.2 Read the ADRs that control your write-set. Do not read the others. | `:98-99` | none | objective 12 | Eight to twelve ADRs on each Ready task. | kept in the `CLAUDE.md` read order |
| W17 | 5.3 Read the rejected decisions before you propose one. | `:101-103` | none | objective 12 | none | merged into templates section 2 |
| W18 | 5.4 Not in the roadmap means out of scope. | `:105-106` | none | none | Repeats `docs/CHARTER.md:264-266` and `docs/roadmap.md:12-13`. | deleted |
| W19 | 6.1 Each ADR declares its enforcement. The unenforced count must not grow. | `:114-116` | `AdrGateTests` | objective 17 | Repeats `docs/templates.md:105`. | deleted; the field rule and the gate hold it |
| W20 | 6.2 Write a gate for an important boundary. | `:118-120` | none | none | Cites closed R-0004 as open. | merged into `CLAUDE.md`, one line |
| W21 | 6.3 Prefer a compile error, then a test, then CI, then a convention. | `:122-129` | none | none | none | merged into `CLAUDE.md`, one line |
| W22 | 7.1 No long branch. Each part merges to main green. | `:135-138` | none | objective 12 | Practice differs: `p0-foundations` merged twelve milestones at once (commit `0b88334`). The owner merges. | rewritten: each push is green (question Q4) |
| W23 | 7.2 Leave the repository green. | `:140-141` | CI | objective 12 | Repeats C21. | deleted |
| W24 | 7.3 Write for a reader who is not you. | `:143-145` | none | objective 12 | none | merged into C1 |
| W25 | 8.1 Two agents must not hold write-sets that intersect. | `:151-152` | none | objective 12 | Repeats `docs/templates.md:177-178`. | deleted; templates holds it |
| W26 | 8.2 Generate a central list, or add to it only. | `:154-156` | partial: `AdrGateTests` for the ADR index | objective 12 | none | free text in templates |
| W27 | 8.3 Each agent has its own worktree. Hours, not days. | `:158` | none | objective 12 | No mechanism. | deleted |

### 2.4 `docs/conventions.md`

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| V1 | One command in one file. The wire name is the `Name` property. | `docs/conventions.md:5-18` | none | objective 17 | none | merged into templates, section "Code conventions" |
| V2 | Type suffixes. | `:20-32` | none | none | none | merged, same section |
| V3 | Diagnostic code grammar. | `:34-38` | partial: the scanner regex `DiagnosticsScanner.cs:9-11` | ADR-0008 | Repeats `docs/diagnostics.md:5-9`. | deleted; diagnostics.md holds it |
| V4 | Event `Kind` grammar. | `:40-51` | none | none | The carve-outs make it a description. | free text; merged |
| V5 | A schema is a projection of the handler. | `:53-57` | partial: `SchemaEndpointGateTests`, fixed name lists at `:112` and `:129` | ADR-0013 | Repeats ADR-0013. | deleted; the gate list becomes `HandlerCatalog` |
| V6 | A contract change needs an ADR in the same pull request. | `:59-68` | partial: `contract-gate`, pull request only | objective 17 | Line 61 names a directory and a section that do not exist. | merged into templates section 2; gate runs on push (question Q4) |
| V7 | Cite the ADR at the decision point. | `:70-79` | none | objective 12 | Repeats W15. | merged, with W15 |

### 2.5 `docs/templates.md`

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| T1 | The register entry form. The class gives the due date. | `docs/templates.md:17-34` | `RegisterGateTests` | objective 12 | Repeats `docs/register.md:50-72`. | deleted; register.md holds it |
| T2 | The three close forms. An accepted entry moves to "Accepted compromises". | `:36-46` | none | objective 12 | Repeats `docs/register.md:236-246`. | deleted |
| T3 | Extend an entry one time. | `:48-56` | `RegisterGateTests` | objective 12 | Repeats `docs/register.md:38-39`. | deleted |
| T4 | The ADR form. | `:60-95` | partial: `AdrGateTests`, the fields | objective 17 | `docs/adr/README.md:53` gives a different instruction: copy the last ADR. | kept; README step 1 says "use the form" |
| T5 | ADR field rules: status set, reciprocity, `affects`, `enforced-by`. | `:97-105` | partial: `AdrGateTests`; none for `affects`; none for the existence of the `enforced-by` file | objective 17 | Four in-force ADRs name an absent test (finding F17). | kept; new existence gate (question Q10) |
| T6 | After `Accepted` only `status`, `superseded-by` and `enforced-by` change. | `:107-108` | none | objective 17 | Stale metadata cannot be corrected. Practice changes `amended-by` and `notes`. | rewritten (question Q3) |
| T7 | The task form with front matter. | `:112-149` | `WriteSetGateTests` | objective 12 | none | kept |
| T8 | The Outcome block and the Method block. | `:151-166` | none | objective 12 | none | kept |
| T9 | Task field rules: `depends-on`, `governed-by`, `writes`, `forbid`. | `:168-178` | partial: `WriteSetGateTests` for `writes` and `forbid`; none for `depends-on`; none for `governed-by` | objective 12 | Line 173 says "a gate can verify this". None does. | kept; new gate (question Q10) |
| T10 | The commit message form. One topic in one commit. | `:182-192` | none | objective 12 | The trailer names the task, and nothing reads it. | kept; the write-set gate reads the trailer (question Q11) |
| T11 | The checklist to end a session. | `:196-213` | partial, as C21 | objective 12 | Repeats C21. Line 198 says so. | deleted |
| T12 | The gate table. "Two gates exist today." | `:217-237` | none | none | Stale. Fourteen rows. The row "ADR index" names a generated index that does not exist. | rewritten: one row for each of the thirteen gate classes |
| T13 | The codebase review form and its rules. | `:241-299` | `CodebaseReviewGateTests` | objective 12 | none | kept |

### 2.6 `docs/INDEX.md`, `docs/glossary.md`, `docs/open-questions.md`

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| I1 | The archived boundary document must not be used. | `docs/INDEX.md:5` | partial: `ObjectiveReferenceGateTests` excludes the archive | none | none | kept |
| I2 | A change to the shape of `Engine.Contracts` needs an ADR. | `docs/INDEX.md:11` | partial: `contract-gate` | objective 17 | Repeats V6. | kept, one cell |
| I3 | Do not change a project outside the spine without a task. | `docs/INDEX.md:45-49` | `WriteSetGateTests` | objective 5 | Repeats C10. | kept; C10 deleted |
| I4 | Three stale statements: `docs/architecture/` (`:38`), the job list of `ci.yml` (`:43`), "Replaces `open-questions.md`" (`:33`). | `docs/INDEX.md` | none; the exit of R-0019 asks for a path gate | none | An agent reads this file first. | corrected; new path gate (question Q10) |
| G1 | Do not rename a term without an ADR. | `docs/glossary.md:3` | partial: `contract-gate` | objective 17 | none | kept |
| G2 | The version mirrors the last emitted `Seq`. | `docs/glossary.md:29` | none | none | ADR-0020 changes the meaning. The code holds the old meaning until TASK-0035. | kept until TASK-0035 ships |
| Q1 | Open questions live in `open-questions.md`, per `CLAUDE.md`. | `docs/open-questions.md:3-5` | none | none | False. `CLAUDE.md:230` puts them in the register. OQ-0001 and OQ-0002 are closed entries R-0014 and R-0015 (`docs/register.md:387`, `:473`). | deleted, the whole file (question Q2) |

### 2.7 `docs/register.md`, `docs/diagnostics.md`, `docs/roadmap.md`, the ledger, the ADR index, the tasks

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| R1 | Each entry has an `opened` date and a `due` date. | `docs/register.md:28` | `RegisterGateTests` | objective 12 | none | kept |
| R2 | A passed `due` date fails the build. | `:29-30` | `RegisterGateTests` | objective 12 | none | kept |
| R3 | Four exits. Extend one time only. | `:31-39` | partial: `RegisterGateTests` for the extension | objective 12 | none | kept |
| R4 | The limit is 15 open entries. | `:40-42` | `RegisterGateTests` | objective 12 | none | kept |
| R5 | A marker word needs a register identifier. | `:43-46` | `MarkerGateTests`; the gate also reads `FIXME` and `XXX` (`MarkerGateTests.cs:25`) | objective 12 | The gate list is larger than the rule list. | kept; the rule lists the gate words |
| R6 | Do not delete an entry. | `:47-48` | none | objective 12 | none | kept |
| R7 | The entry format. | `:61-72` | `RegisterGateTests` | objective 12 | Repeated in templates section 1. | kept here only |
| D1 | Each `E-`, `W-`, `I-` code in `Engine.*` appears in the registry. | `docs/diagnostics.md:3` | partial: `DiagnosticsRegistryGateTests`, three of five engine projects (`DiagnosticsScanner.cs:17-22`) | ADR-0008 §4 | `Engine.Api.Http` and `Engine.Geometry.Manifold` raise codes and are unread. | kept; the scanner reads each `Engine.*` project |
| D2 | Append-only. Mark a code obsolete if it is no longer raised. | `:3`, `:38` | none | none | "Append-only" and "mark obsolete" conflict. TASK-0036 must move a row (`tasks/TASK-0036-honest-geometry-backend.md:92-93`). | rewritten: do not remove a code, do not change its meaning |
| D3 | Three positions: the row, the constant, the same change. | `:32-36` | partial: `SchemaEndpointGateTests` (constant to `/schema`), the scanner (source to row) | none | none | kept |
| D4 | At most two reserved codes. | `:49-50` | `DiagnosticsReserveGateTests` | none | none | kept |
| M1 | The task status is the authority. The roadmap is the menu. | `docs/roadmap.md:5-7` | none | objective 12 | none | kept |
| M2 | Move a phase from Pending to Shipped. | `:9-10` | none | none | A second status vocabulary (finding F14). | deleted; the Shipped list stays (question Q7) |
| M3 | Each approved objective must have a track. | `:12-13` | none | all | none | kept |
| M4 | The order rules. | `:123-132` | none | none | `depends-on` in each task holds the same order and nothing reads it. | kept; `depends-on` gate (question Q10) |
| L1 | One entry for each milestone. Reference an ADR by its number. Do not restate a decision. | `docs/CURRENT-STATE.md:3` | partial: `CodebaseReviewGateTests` counts the headings | objective 12 | none | kept |
| L2 | An entry is permanent. | `docs/working-agreement.md:56-57` | none | objective 12 | none | merged into `CLAUDE.md` (W7) |
| A1 | The status legend, six values. | `docs/adr/README.md:5-12` | `AdrGateTests` | objective 17 | none | kept |
| A2 | The index agrees with each front matter. | `:14-38` | `AdrGateTests` | objective 17 | A person edits it. The gate catches drift. | kept |
| A3 | Adding an ADR: copy the last, number, Proposed, update the index. | `:51-56` | partial: `AdrGateTests` for the number and the index | objective 17 | Step 1 conflicts with the form in templates section 2. | rewritten: use the form |
| K1 | Thirteen tasks have no front matter. The budget is 13. | `tasks/TASK-0001` to `TASK-0013` | `WriteSetGateTests` | objective 12 | none | kept |
| K2 | The task status set is `Ready`, `Active`, `Deferred`, `Done`. | `CLAUDE.md:35`, `docs/INDEX.md:42` | partial: `WriteSetGateTests` reads `Done` only | objective 12 | No task uses `Active`. | `Active` removed (question Q7) |

### 2.8 `.github/` and `eng/`

| Id | Rule | Source | Gate | Serves | Cost | Proposal |
|---|---|---|---|---|---|---|
| P1 | The gate runs on three operating systems. | `.github/workflows/ci.yml:25-93` | the job itself | objective 8 | none | kept |
| P2 | A change to `Engine.Contracts` needs an ADR. Pull request only. | `ci.yml:95-124` | the job itself | objective 17 | It ran on no merge into `main` (finding F13). | changed to run on a push (question Q4) |
| P3 | Each commit stays inside the write set of a task that it touches. | `ci.yml:126-190` | the job itself | objective 12 | R-0026: each touched task gives its permits. | changed: the commit trailer names the task (question Q11) |
| P4 | The pull request template: four boundary boxes, four verification boxes. | `.github/PULL_REQUEST_TEMPLATE.md:1-23` | none | none | Stale: `Vortice.Vulkan.*` (`:15`), a separate close commit (`:7-9`), "Pending to Shipped" (`:23`). | rewritten to five lines |
| P5 | Do not move the cut-off commit. | `eng/write-set-cutoff.txt:9-10` | none | objective 12 | none | kept |
| P6 | CODEOWNERS documents ownership and enforces nothing. | `.github/CODEOWNERS:3-5` | none | none | none | kept, free text |

## 3 Findings

### 3.1 Duplicates

**F1 [Observed]** `CLAUDE.md` repeats four other files. The navigation list (`CLAUDE.md:25-43`)
repeats `docs/INDEX.md:25-43`. The dependency rules (`CLAUDE.md:104-120`) repeat the twelve tests of
`DependencyDirectionGateTests.cs` and ADR-0009, ADR-0014 and ADR-0017. The triad
(`CLAUDE.md:128-137`)
repeats `docs/glossary.md:9-11`. The geometry rules (`CLAUDE.md:171-180`) repeat anti-objectives 5,
13 and 14 (`docs/CHARTER.md:211-213`, `:236-243`). The read order (`CLAUDE.md:47-52`) repeats
`docs/CHARTER.md:279-281`. The anti-patterns C23, C24, C25 and C29 each repeat one anti-objective.

**F2 [Observed]** `docs/working-agreement.md` holds 27 rules. Fourteen repeat a rule in another file
or a gate: W2, W4, W5, W8, W9, W11, W12, W13, W15, W18, W19, W23, W25 and the second half of W6.

**F3 [Observed]** `docs/templates.md` sections 1 and the close and extend forms (`:17-56`) repeat
`docs/register.md:36-72` and `:236-246`. The checklist in section 5 (`:196-213`) repeats
`CLAUDE.md:224-230`, and line 198 says so.

**F4 [Observed]** `docs/conventions.md` repeats the diagnostic grammar of `docs/diagnostics.md:5-9`,
the schema rule of ADR-0013, and the ADR citation rule of `docs/working-agreement.md:95-96`.

**F5 [Inferred]** The duplicates are the largest cost. Each duplicate must change in two places when
a
fact changes. Register entry R-0019 records six stale statements, and each one sits in a duplicate
or in a pointer to a duplicate.

### 3.2 Contradictions

**F6 [Observed]** `CLAUDE.md:201-202` says "Do not run the gate on your computer." Each ledger entry
from v0.19 to v0.33 reports a full local test run, for example "The test list holds 206"
(`docs/CURRENT-STATE.md:803`). `docs/templates.md:174` tells a person to run the write-set check
locally with `WRITE_SET_FILES`. The rule and the practice disagree. The practice is correct: a local
run finds the error before a push, and continuous integration stays the authority.

**F7 [Observed]** `CLAUDE.md:196` says: "The documentation and the code disagree. Report both. Do
not
correct either one." Ledger entry v0.21 (`docs/CURRENT-STATE.md:343-389`) shows the practice: the
agent stopped, the owner decided, and TASK-0021 corrected both. The rule works as a stop rule. Its
last sentence forbids the correction that follows the decision. **[Recommended]** Keep the stop.
Replace the last sentence with "The owner decides which one is correct."

**F8 [Observed]** `CLAUDE.md:184-185` says of `docs/diagnostics.md`: "Add codes only."
`docs/diagnostics.md:38`
says "Mark obsolete in this file if no longer raised", which is a change to a row. TASK-0036
(`tasks/TASK-0036-honest-geometry-backend.md:92-93`) must move `E-GEOM-BACKEND-INIT` from the
reserved
table to the raised table. **[Recommended]** The rule becomes: do not remove a code, and do not
change the meaning of a code. A row may move between the two tables.

**F9 [Observed]** Two documents give a different set of ADR fields that may change after acceptance.
`docs/working-agreement.md:51-53` permits "a status field". `docs/templates.md:107-108` permits
`status`, `superseded-by` and `enforced-by` only.

**F10 [Observed]** The practice changes more fields than either rule permits. Commit `74b5305`
(v0.32)
changed `amended-by` and `notes` of ADR-0006. Commit `3c7ced9` (v0.33) changed `status` and `notes`
of
ADR-0003. No gate reads the rule, therefore nothing failed. **[Observed]** The same rule keeps stale
metadata: ADR-0007 lists `BlazorApp/**` in `affects`, and no tracked file exists under `BlazorApp`
(`git ls-files BlazorApp` gives zero files). **[Recommended]** `docs/templates.md:11-13` already
says
that front matter exists so that a gate can read it. Make the rule: after acceptance the text is
permanent, and each front-matter field except `id`, `title` and `date` may change. Question Q3.

**F11 [Observed]** `docs/register.md:43-46` lists seven marker words. `MarkerGateTests.cs:25` reads
nine: it adds `FIXME` and `XXX`. The gate is stricter than the rule. **[Recommended]** The rule
lists
the nine words of the gate.

**F12 [Observed]** `docs/adr/README.md:53` says "Copy the most recent ADR as a template."
`docs/templates.md:60-95` gives a form. ADR-0021 has a field `topic` that the form does not have,
and
the form has no `notes` field that eleven ADRs use. **[Recommended]** The README says "use the form
in `docs/templates.md`, section 2", and the form gains `topic` and `notes` as optional fields.

**F13 [Observed]** `docs/working-agreement.md:135-138` says that each part of architecture work
"must merge to the main branch". The owner merges. Commit `0b88334` merged twelve milestones (v0.16
to v0.27) at once. Commit `91540fc` merged three. The branch `decisions-2026-09-25` holds two more.
The contract gate (`ci.yml:99`) runs on a pull request only, and each of the last three merges was
local, therefore the gate never checked a merge into `main` (finding G1 of the codebase review,
`docs/reviews/2026-09-23-codebase-review.md:512-517`). **[Inferred]** The rule makes the owner the
bottleneck: the rule asks for a merge after each part, and only the owner can merge.
**[Recommended]** The rule becomes "each push is green". The contract gate runs on a push, against
the previous tip, as the write-set gate already does with `PUSH_BEFORE` (`ci.yml:152-166`). Question
Q4.

**F14 [Observed]** Two status vocabularies exist. Tasks use `Ready`, `Done` and `Deferred`
(`grep -h -m1 '^status:' tasks/*.md`: 6, 18 and 1), and `CLAUDE.md:35` and `docs/INDEX.md:42` add
`Active`, which no task uses. The roadmap uses `Pending` and `Shipped` (`docs/roadmap.md:9-10`,
`:48-121`), and the pull request template repeats that pair (`.github/PULL_REQUEST_TEMPLATE.md:23`).
`docs/roadmap.md:5-7` says that the task status is the authority. **[Inferred]** The second
vocabulary is a second place to update, and the ledger already carries the version of each shipped
phase. **[Recommended]** Remove the "Status" column from the phase tables. Keep the list "Shipped",
because it gives the version and the task in one line. Remove `Active`. Question Q7.

**F15 [Observed]** Anti-objective 10 (`docs/CHARTER.md:229-231`) says that a command records the
backend that performed the operation. The architecture challenge says that this record "was more
than needed" and that a kernel stamp in the document header covers the risk
(`docs/reviews/2026-09-23-architecture-challenge.md:498`, `:281`). Register entry R-0025
(`docs/register.md:186-197`) holds the related question about ADR-0015. No code records a backend
today. **[Recommended]** Keep the anti-objective as it is until the owner decides R-0025. A change
to
an anti-objective is the owner's decision. Question Q6.

### 3.3 Rules with no mechanism

**F16 [Observed]** `CLAUDE.md:232-240` gives six conditions to examine the plan again. No check
reads
them. Condition 2, "a session read more than five files to find its position", trips on each Ready
task (section 1). Condition 1, "three milestones shipped", is weaker than the codebase review gate,
which counts six. **[Recommended]** Delete five conditions. Keep the sixth, because it tells the
agent
to report to the owner and not to change an objective.

**F17 [Observed]** `docs/working-agreement.md:114-116` and `docs/templates.md:105` say that
`enforced-by` names a test. `AdrGateTests.cs:167` reads only the prefix `UNENFORCED`. ADR-0018,
ADR-0019, ADR-0020 and ADR-0021 have the status `Accepted` and each names a test that does not
exist,
with the note "(TASK-nnnn creates it)". The budget of unenforced ADRs (`AdrGateTests.cs:40`) is 1
and
does not see them. Four of nineteen in-force ADRs are therefore unenforced in fact.
**[Recommended]**
A gate reads the path before the parenthesis and fails when the file is absent, unless the named
task
has the status `Ready`. When that task closes, the file must exist. Question Q10.

**F18 [Observed]** `docs/templates.md:173` says that `governed-by` must equal the intersection of
`affects` and `writes`, and "a gate can verify this". No gate does. The script `governed-by.js`
(this session) computed the rule for each task with front matter. Each of the six Ready tasks agrees
with the rule. Nine of the nineteen closed tasks differ, because ADR-0018 to ADR-0021 arrived after
those tasks closed. **[Inferred]** A gate must read Ready and Active tasks only, because a closed
task
is a record. **[Observed]** The lead about ADR-0008 is half correct. TASK-0034 implements ADR-0008
§6
(`tasks/TASK-0034-document-session.md:49`) and does not list ADR-0008. The rule gives that result,
because ADR-0008 affects `Engine.Contracts/**` only and TASK-0034 forbids that path. The task
records
this at its line 122. The weak point is the `affects` field, not the absence of a gate. Question Q3
permits a correction of `affects`.

**F19 [Observed]** `docs/templates.md:172` says that a tool reads `depends-on`. No gate and no tool
reads it. `docs/roadmap.md:125-132` repeats the same order in prose. **[Recommended]** A gate fails
when
`depends-on` names a task that does not exist. `CLAUDE.md:219` says "Advance the next task that has
the
status Ready", and the next task becomes "the first Ready task, in roadmap order, whose `depends-on`
tasks are Done". That sentence removes the recurring question "which task is next?".

**F20 [Observed]** No gate scans for the calls that determinism rules 1, 3 and 4 forbid
(`CLAUDE.md:157-166`; finding T2 of the codebase review). No gate reads a runtime identifier for
rule
5. **[Recommended]** One test scans each `Engine.*` project and `3DEngine.Core` for the forbidden
names, and reads each project file for a runtime identifier that starts with `win-x86`. The list of
names is the rule. This gate makes four of the six determinism rules mechanical. Question Q10.

**F21 [Observed]** `docs/templates.md:184-190` gives a commit trailer `TASK-0014 · ADR-0013`.
Nothing
reads it. Register entry R-0026 records that a commit gets the permits of each task file that it
touches. **[Recommended]** The write-set gate reads the trailer of the commit, and only the named
task
governs the commit. The form already exists, therefore the rule costs one line. A commit with no
trailer fails with a message that names the form. Question Q11.

### 3.4 Gates with a short list, and gates with no rule

**F22 [Observed]** Five gates read a fixed list that a new file does not enter.

- `DispatchSurfaceGateTests.cs:16-23` reads five host files. A new file in `Engine.Cli` or
  `Engine.Api.Http` is not read.
- `DiagnosticsScanner.cs:17-22` reads `Engine.Contracts`, `Engine.Core` and `Engine.Cli`.
  `Engine.Api.Http` raises `E-API-BAD-REQUEST`, `E-API-WS-INVALID-SUBSCRIBE` and `W-API-WS-LAGGED`
  (`docs/diagnostics.md:19-21`), and the scanner does not read it.
- `SchemaEndpointGateTests.cs:112` and `:129` hold the command names and the query name as literals.
  A new command is not checked. The codebase review did not report this one.
- `DependencyDirectionGateTests.cs:123-127` names the clients and the hosts. This list is correct: a
  new project must be classified, and the test `Each_Project_Name_Appears_Exactly_Once` does not
  classify it. **[Recommended]** A test fails when a project in the solution is in no list.
- `MarkerGateTests.cs:69-74` names nine projects. A tenth project is not read.

**[Recommended]** Each list becomes a scan of the solution file, or of `HandlerCatalog`, or of the
directory. The gate then sees a new file with no change to the gate.

**F23 [Observed]** `RepositoryFiles.TrackedTextFiles()` (`RepositoryFiles.cs:19-52`) has no caller
in
the source. It is dead code in the gates.

**F24 [Observed]** The register gate, the marker gate, the native package gate, the objective gate
and
the codebase review gate each have a rule in a document. The dead helper above has none. No gate
exists with no rule. The reverse condition is the problem: seventy rules have no gate.

**F25 [Observed]** The write-set gate exempts thirteen task files with no front matter and bounds
the
count at 13 (`WriteSetGateTests.cs:31`). These files use a different form for the status
(`## Status` followed by `Done — shipped in commit`). **[Inferred]** The exemption is correct. A
closed
task is a record. The budget stops a new task without front matter.

### 3.5 Stale statements

**F26 [Observed]** `docs/templates.md:237` says "Two gates exist today." Thirteen gate classes
exist.
`docs/templates.md:228` names "the generated index", and no tool generates the ADR index
(`AdrGateTests.cs:182-184`). The table holds fourteen rows and the sentence says twelve.

**F27 [Observed]** `docs/open-questions.md:3-5` says that open questions live there per `CLAUDE.md`.
`CLAUDE.md:230` puts each open question in `docs/register.md`. The two entries OQ-0001 and OQ-0002
are the closed register entries R-0014 and R-0015 (`docs/register.md:387`, `:473`).
`docs/INDEX.md:33`
says that the register "Replaces `open-questions.md`", and the file still exists.

**F28 [Observed]** `docs/INDEX.md:38` names `docs/architecture/`, which does not exist.
`docs/INDEX.md:43`
lists three jobs for `ci.yml` and omits `write-set-gate`. `docs/conventions.md:61` links to
`architecture/engine-runtime-boundaries.md` and cites a `CLAUDE.md` section "When unsure — stop and
ask", which is named "Stop and ask" (`CLAUDE.md:187`). Register entry R-0019 holds these.

**F29 [Observed]** `CLAUDE.md:205-211` lists five checks for the gate. `ci.yml` runs three jobs, two
smoke tests and 206 tests, of which thirteen classes are gates.

**F30 [Observed]** `.github/PULL_REQUEST_TEMPLATE.md:15` names `Vortice.Vulkan.*`, which v0.28
removed (`docs/CURRENT-STATE.md:655-656`). Lines 7 to 9 describe a separate close commit that the
form of `docs/templates.md:151-166` replaced. Line 23 uses the roadmap vocabulary.

**F31 [Observed]** `docs/working-agreement.md:118-120` cites register entry R-0004 as an open gap.
R-0004
closed on 2026-09-20 (`docs/register.md:273-282`).

**F32 [Observed]** `docs/CHARTER.md:118` says "V1 and V1.x are complete." ADR-0001 to ADR-0015 use
the labels `V1`, `V1.x` and `V2` in 128 places. The ledger defines them by use: V1 ended at v0.6
(`docs/CURRENT-STATE.md:38`) and V1.x ended at v0.14 (`:78`). No current document defines either
label. `docs/roadmap.md:119-120` uses `V1` and `V2` for two phases of track V, which is a different
meaning. **[Recommended]** The glossary defines both labels in one line each. Question Q9.

**F33 [Observed]** ADR-0001 to ADR-0015 are not in Simplified Technical English. Six ADRs hold a
contraction. Only ADR-0016 to ADR-0021 carry the language marker. **[Recommended]** Leave the older
records as they are. A record is permanent, and a rewrite gives a second version of one decision.
Question Q9.

**F34 [Observed]** `docs/glossary.md:29` defines the version as the last emitted `Seq`. ADR-0020,
accepted on 2026-09-25, defines it as the count of applied commands. The code holds the old meaning
until TASK-0035 ships. This is not an error today. The glossary describes the code.

### 3.6 Rules that cost more than they protect

**F35 [Observed]** The reading cost of the position rules is eighteen files for TASK-0034 (section
1).
Twelve of them are ADRs that `governed-by` names. **[Inferred]** The `affects` fields are too wide.
ADR-0004 affects `Engine.Core/**` and ADR-0011 affects `Engine.Api.Http/**` and `Engine.Cli/**`,
therefore each task that touches a host or the core must read both. **[Recommended]** Question Q3
permits a narrower `affects` on an accepted ADR. The gate of F18 then gives a shorter list. This
review does not narrow one, because the owner decides the reading list.

**F36 [Observed]** The codebase review gate counts each ledger entry
(`CodebaseReviewGateTests.cs:163-168`).
The last review examined v0.28. Five entries follow it. The ledger entry of this work becomes the
sixth, and the limit is six. The entry after it, v0.35, must be a codebase review or the build
fails. **[Inferred]** The five entries after v0.28 changed no engine code, and this work changes
none. The
gate measures the ledger, not the code. **[Recommended]** Keep the gate. Schedule the review as the
next milestone
after this work. Question Q5.

**F37 [Observed]** Rule W27, "each agent has its own worktree", and rule W25, "compare the
write-sets
before you start two agents", have no mechanism. The write-set gate reads one commit and cannot see
two agents. **[Inferred]** The rules describe a practice for parallel agents that the repository
does not use today. The ledger records one agent for each milestone. **[Recommended]** Delete W27.
Keep W25 as one sentence in the task form, because a person reads it at the moment it matters.

**F38 [Observed]** `CLAUDE.md:139-147` is a section named "Scope clamps" whose content is "No clamp
is active". The section exists to hold a rule that does not exist. **[Recommended]** Delete it. If a
clamp returns, it returns with an ADR.

### 3.7 The leads, verified

| Lead | Result |
|---|---|
| 1 Write-set permits | **Correct.** `WriteSetGateTests.cs:143-153`. R-0026 records it. Finding F21. |
| 2 `enforced-by` names an absent test | **Correct.** Four ADRs. `AdrGateTests.cs:167` reads the prefix only. Finding F17. |
| 3 `governed-by` has no gate | **Correct for the gate. Half correct for ADR-0008.** Each Ready task agrees with the rule. Finding F18. |
| 4 The review period | **Correct.** The sixth entry is this work. Finding F36. |
| 5 ADR field rule and practice | **Correct.** Commits `74b5305` and `3c7ced9`. ADR-0007 lists `BlazorApp/**`. Finding F10. |
| 6 Stale statements | **Correct.** Findings F26 to F31. |
| 7 Duplication | **Correct.** Finding F1. |
| 8 No mechanism | **Correct.** Findings F16 and F19. |
| 9 Rules against practice | **Correct.** Findings F6, F7, F8. |
| 10 Short lists | **Correct, and one more.** `SchemaEndpointGateTests.cs:112`, `:129`. Finding F22. |
| 11 Contract gate | **Correct.** Finding F13. |
| 12 Anti-objective 10 | **Correct.** Finding F15. |
| 13 Older ADRs | **Correct.** 128 label uses, six contractions, no language marker. Findings F32, F33. |
| 14 Two vocabularies | **Correct.** Finding F14. |

## 4 Proposal

### 4.0 The organization after the change

One home for each topic. A file holds one kind of content. A second file points at the home in one
line; it does not repeat the content. The tree gives the target.

```
CLAUDE.md                        Position. What each session must know. About 120 lines.
docs/
  CHARTER.md                     Why. Mission, objectives, anti-objectives, non-goals, the scope test.
                                 The owner's file.
  INDEX.md                       Where. One row for each path, with its purpose.
  glossary.md                    Terms. One line each: the triad, the state, the geometry, V1, V1.x.
  templates.md                   Forms. Register entry, ADR, task, commit, codebase review, the code
                                 conventions, the gate table, the rule for rules.
  register.md                    Deferred items, with their rules and their exits.
  diagnostics.md                 The code registry.
  roadmap.md                     The menu. Tracks and phases. No status column.
  CURRENT-STATE.md               The ledger. One entry for each milestone.
  adr/README.md                  The index of decisions, with one line for each archived number.
  adr/00nn-*.md                  Decisions in force. Each text agrees with the code (section 4.7).
  adr/archive/                   Withdrawn and rejected records. Out of the index, out of the reading list.
  archive/                       Records that are not rules: the old boundary document, the rationale
                                 of the working agreement.
  reviews/                       Dated reviews. A record, not the authority.
tasks/                           Work units. The write set of a task is the permit.
Engine.Tests/Governance/         The gates. One class for each rule with a mechanism.
.github/workflows/ci.yml         The authority for "green". Three operating systems, the contract
                                 gate and the write-set gate, each on a push.
.github/PULL_REQUEST_TEMPLATE.md Five lines.
eng/write-set-cutoff.txt         One commit.
```

The table gives each topic that lives in two or more places today, and its one home after the
change. Each row of section 2 marked "merged" or "deleted" lands in one of these homes.

| Topic | Today | One home after the change |
|---|---|---|
| The purpose of each file | `CLAUDE.md:25-43`, `docs/INDEX.md:25-43` | `docs/INDEX.md` |
| The read order | `CLAUDE.md:47-52`, `docs/CHARTER.md:279-281` | `CLAUDE.md`. The charter line is the owner's. |
| The dependency rules | `CLAUDE.md:104-120`, ADR-0009, ADR-0014, ADR-0017, `DependencyDirectionGateTests` | The gate. `CLAUDE.md` keeps the diagram and names the test. |
| The triad | `CLAUDE.md:128-137`, `docs/glossary.md:9-11`, ADR-0008 | `docs/glossary.md` |
| The geometry rules | `CLAUDE.md:171-180`, anti-objectives 5, 13 and 14 | `docs/CHARTER.md` |
| The determinism rules | `CLAUDE.md:149-169` | `CLAUDE.md`, unchanged, with a gate |
| The topology | `CLAUDE.md:81-102`, ADR-0011, ADR-0019 | ADR-0019, which supersedes ADR-0011 (section 4.7). `CLAUDE.md` keeps one paragraph. |
| The session end | `CLAUDE.md:224-230`, `docs/templates.md:196-213` | `CLAUDE.md` |
| The register form and the exits | `docs/register.md:28-72`, `docs/templates.md:17-56` | `docs/register.md` |
| The Method block | `docs/working-agreement.md:76-83`, `docs/templates.md:159-166` | `docs/templates.md`, section 3 |
| One topic in one commit | `docs/working-agreement.md:42`, `docs/templates.md:192` | `docs/templates.md`, section 4 |
| The write set | `docs/working-agreement.md:34-36`, `CLAUDE.md:254`, `docs/templates.md:174` | The gate. `CLAUDE.md` has one line. |
| No dispatch switch | `docs/working-agreement.md:44-45`, `CLAUDE.md:258-260` | The gate. `CLAUDE.md` has one line. |
| ADR enforcement | `docs/working-agreement.md:114-116`, `docs/templates.md:105` | `docs/templates.md`, section 2, and the gate |
| The ADR citation in code | `docs/working-agreement.md:95-96`, `docs/conventions.md:70-79` | `docs/templates.md`, section "Code conventions" |
| A contract change needs an ADR | `docs/conventions.md:59-68`, `docs/INDEX.md:11`, `docs/glossary.md:3`, `CLAUDE.md:191-192` | `docs/templates.md`, section 2, and the contract gate. Each other place points there in one cell. |
| The diagnostic grammar | `docs/conventions.md:34-38`, `docs/diagnostics.md:5-9` | `docs/diagnostics.md` |
| Stop and ask | `CLAUDE.md:187-196`, `docs/working-agreement.md:62-64`, `docs/CHARTER.md:272-274` | `CLAUDE.md`. The charter step is the owner's. |
| The status of a task | `CLAUDE.md:35`, `docs/INDEX.md:42`, `docs/templates.md:118`, `docs/roadmap.md:9-10` | `docs/templates.md`, section 3 |
| Open questions | `docs/open-questions.md`, `docs/register.md` | `docs/register.md` |
| The gate list | `CLAUDE.md:205-211`, `docs/templates.md:217-237`, `docs/INDEX.md:43` | `docs/templates.md`, section 6, one row for each gate class |
| Parallel agents | `docs/working-agreement.md:149-158`, `docs/templates.md:177-178` | `docs/templates.md`, section 3, one sentence |

### 4.1 The target set of rule files

Ten files hold rules after the change. Three files go. The ledger, the CI workflow and the cut-off
file stay as they are, with one ledger entry for this work.

| File | Purpose after the change | Lines, estimate |
|---|---|---|
| `CLAUDE.md` | Position. The language rule, the read order, the session start and end, the stop conditions, the determinism rules, and eight one-line rules that have no other home. | 120 |
| `docs/CHARTER.md` | Why. Unchanged, except the answers to questions Q6 and Q9. | 281 |
| `docs/templates.md` | The forms and the gate table. Gains the code conventions and the ADR rules of the working agreement. Loses section 1, section 5 and the stale section 6. | 240 |
| `docs/register.md` | The entries and their rules. Unchanged, except rule 5 lists nine words. | 482 |
| `docs/diagnostics.md` | The registry. Line 3 and line 38 use one sentence: do not remove, do not change the meaning. | 58 |
| `docs/roadmap.md` | The menu. Loses the "Status" column. | 125 |
| `docs/INDEX.md` | The map. Gains the navigation purposes of `CLAUDE.md`. Loses three stale statements. | 55 |
| `docs/glossary.md` | The terms. Gains the triad sentence, `V1` and `V1.x`. | 56 |
| `docs/adr/README.md` | The index. Step 1 of "Adding an ADR" points at the form. | 57 |
| `.github/PULL_REQUEST_TEMPLATE.md` | Five lines: the task, the ADR, the ledger entry, "the gate is green". | 12 |

Deleted:

| File | What it protected | What protects it after the change |
|---|---|---|
| `docs/working-agreement.md` | Twenty-seven behaviour rules. | Fourteen are duplicates of a gate or a rule that stays (section 2.3). Nine move to `CLAUDE.md` as one line each: W1, W3, W7, W10, W14, W16, W20, W21, W24. Four move to `docs/templates.md`: W4, W15 with V7, W17, W26. W22 changes (question Q4). W27 goes with nothing behind it (finding F37). The rationale paragraphs, which cite Blender, FreeCAD and Ondsel, move to `docs/archive/working-agreement.md` with a header that says the file is a record and not a rule. |
| `docs/conventions.md` | Seven conventions. | V1, V2, V4, V7 move to a section "Code conventions" in `docs/templates.md`. V3 is in `docs/diagnostics.md`. V5 is ADR-0013 and its gate. V6 is `docs/templates.md`, section 2, and the contract gate. |
| `docs/open-questions.md` | Two questions. | Both are closed register entries R-0014 and R-0015. Nothing is lost. |

### 4.2 `CLAUDE.md` after the change

The sections, in order:

1. **Language.** C1 as it is. One sentence from W24: write for a reader who is not you.
2. **Where to start.** The read order (C4), the session start (C19 with the roadmap order and
   `depends-on`), the read rule for ADRs (W16), and C3. The navigation purposes go to
   `docs/INDEX.md`, and one line points there.
3. **The authority diagram.** C5 with C8, unchanged. One sentence names
   `DependencyDirectionGateTests` as the dependency rule (C9). One paragraph gives the topology and
   points at ADR-0019 (C6).
4. **Determinism rules.** C13, unchanged and in force. One sentence names the new gate when it
   exists.
5. **Diagnostic codes.** C15 with the corrected wording.
6. **Stop and ask.** C16 with the corrected condition 5.
7. **Tests.** C17 rewritten: run the tests for the file that you change while you work; before a
   commit, run `dotnet test` and the write-set check with `WRITE_SET_FILES`; continuous integration
   is the authority, and `ci.yml` lists its jobs.
8. **Session end.** C21, unchanged. One line from W7: an entry in the ledger is permanent.
9. **Rules with no other home.** Eight one-line rules: W1, W3, W10, W14, W20, W21, C27, C28. The
   owner-report condition of C22. C30 in one line with the gate name.

Deleted from `CLAUDE.md`: C2, C7, C10, C11, C12, C14, C18, C22 (five of six), C23, C24, C25, C26,
C29. Each has a row in section 2 that names the file or the gate that holds it.

### 4.3 The gates

Five new gates, each under eighty lines, in `Engine.Tests/Governance/`. Each one converts a text
rule to a test. The owner accepts each one in question Q10.

| Gate | Rule that it replaces | It fails when |
|---|---|---|
| `AdrEnforcementExistsGateTests` | T5, F17 | An in-force ADR names an `enforced-by` file that is absent, and the task in the parenthesis is not `Ready`. |
| `TaskGovernanceGateTests` | T9, F18, F19 | `governed-by` of a Ready or Active task differs from the intersection rule, or `depends-on` names an absent task. |
| `DeterminismCallGateTests` | C13 rules 1, 3, 4, 5; F20 | A source file in `Engine.*` or `3DEngine.Core` calls a listed function, or a project file names a 32-bit runtime identifier. |
| `IndexPathGateTests` | I4, R-0019 exit | A path in `docs/INDEX.md` does not exist. |
| Write-set trailer rule in `WriteSetGateTests` | P3, T10, R-0026 | The commit trailer names no task, or a changed file is outside the write set of the named task. |

Five gate corrections, each a change to a list (finding F22): the dispatch gate scans each host
project; the diagnostics scanner reads each `Engine.*` project; the schema gate reads
`HandlerCatalog`;
the dependency gate fails on an unclassified project; the marker gate reads each project in the
solution. Each correction is verified by injection before and after, as the working method requires.

One deletion: `RepositoryFiles.TrackedTextFiles()` (finding F23).

### 4.4 Register entries that this work closes

- R-0019 closes when each stale statement is corrected and the path gate exists.
- R-0026 closes when the trailer rule exists.

### 4.5 What the work does not do

- It does not change the text of an accepted ADR.
- It does not change an objective, an anti-objective or the acceptance of an ADR.
- It does not change `Engine.Contracts`.
- It does not change engine code. The gates are tests.
- It does not start TASK-0028 or TASK-0034 to TASK-0038.
- It does not rewrite ADR-0001 to ADR-0015.

### 4.6 The five rules that contradict the practice, made again

The owner asked on 2026-09-25 to examine how each of these rules was made and for what purpose, and
to change the rule when the purpose asks for a different rule. For each rule: where it came from,
whether its purpose still holds, and the rule after the change.

**Rule 1. "Do not run the gate on your computer" (C17, `CLAUDE.md:200-203`).**

- **[Observed]** Made from register entry R-0002 (`docs/register.md:400-416`): continuous integration
  never ran on Windows or macOS, and a local run on one system proved nothing about the other two.
  The purpose: the report of three runners is the proof, and a local pass does not mean "complete".
- **[Inferred]** The purpose holds. The words do not. A local run finds an error before the push, it
  costs one minute, and each session does it (finding F6).
- **[Recommended]** "Run the tests before each commit. The gate on three operating systems is the
  proof. Report a local result as local."

**Rule 2. "Report both. Do not correct either one" (C16 condition 5, `CLAUDE.md:196`; W9,
`docs/working-agreement.md:62-64`).**

- **[Observed]** Made in TASK-0015 with the working agreement. The purpose
  (`docs/working-agreement.md:51-54`, `:62-64`): an agent cannot tell which side holds the decision,
  and an agent told to "update the documentation" rewrites the record to agree with the code.
- **[Inferred]** The purpose holds for a decision. It does not hold for a fact about the repository:
  a path that does not exist, a count, a job list. Fourteen such facts are false today (section
  3.5), and the rule stopped each session from correcting one. The owner said on 2026-09-25 that a
  statement that does not need to exist must be cleaned.
- **[Recommended]** "A false statement about the repository is an error. Correct it in the task that
  finds it, and say so in the ledger entry. A decision that the code contradicts is a question. Stop,
  report both, and the owner says which one is correct. Then the same task corrects the other." This
  rule removes the recurring question "may I correct this line?".

**Rule 3. "Add codes only" (C15, `CLAUDE.md:184-185`; `docs/diagnostics.md:3`).**

- **[Observed]** Made in TASK-0003 from ADR-0008 §4: a code is stable because a client may hold it.
- **[Inferred]** The purpose holds for the existence and the meaning of a code. It does not hold for
  the position of a row, and it blocks TASK-0036 (finding F8).
- **[Recommended]** "Do not remove a code. Do not change the meaning of a code. A row may move
  between the tables, and its text may say where the code is raised now."

**Rule 4. "After acceptance the text of an ADR is permanent" (T6, `docs/templates.md:107-108`; W6,
`docs/working-agreement.md:51-54`).**

- **[Observed]** Made in TASK-0015. The purpose: the same fear as rule 2, applied to a decision
  record. The mechanism: no field but three may change, and the text never.
- **[Inferred]** The fear holds. The mechanism does not: the practice changes other fields (finding
  F10), stale metadata cannot be corrected, and a reader of an amended ADR reads two records for one
  decision. The owner said on 2026-09-25 that an ADR that evolved is updated with the code, and the
  code with the ADR.
- **[Recommended]** The rule of section 4.7. It keeps the value of the record with a History section
  and with the acceptance of the owner for each change to a Decision.

**Rule 5. "Each part must merge to the main branch" (W22, `docs/working-agreement.md:135-138`).**

- **[Observed]** Made in TASK-0015 from the FreeCAD lesson: a long architecture branch never merges.
  The purpose: small parts, always green, no divergence.
- **[Inferred]** The purpose holds. The mechanism asks the owner to merge after each part, and only
  the owner merges. The owner merged twelve milestones at once (finding F13). The rule makes the
  owner the bottleneck, and the contract gate never saw a merge.
- **[Recommended]** "Each milestone ends in one session. Each push is green. The contract gate and the
  write-set gate run on each push. A branch holds one track. The owner merges when it is convenient."

### 4.7 Living ADRs

**The direction of the owner, 2026-09-25.** An ADR that evolved or changed is updated with the code,
and the code with the ADR. An ADR that nobody needs any more is cleaned.

**[Observed]** Seven ADRs have the status `Amended`: 0004, 0006, 0008, 0010, 0011, 0012 and 0013. The
legend says that both records apply (`docs/adr/README.md:8`), therefore a reader reads two files for
one decision, and three files for ADR-0006. Two precedents for an update inside the record exist:
ADR-0012 holds "Amendment 1" in its own text (`docs/adr/0012-geometry-backend-wiring.md:210`), and
ADR-0008 holds a section "Amendment to ADR 0006" (`docs/adr/0008-command-query-event-triad.md:201`).

**[Recommended]** The rule:

1. An accepted ADR describes the decision in force. Its text agrees with the code. An accepted ADR
   that a Ready task implements is the one exception, until that task closes.
2. When a task changes a decision, the same task changes the ADR text. It adds one line to a section
   "History" in the ADR: the date, the task, and the change in one sentence. Git holds each old text.
3. The owner accepts each change to a section "Decision", as the owner accepts an ADR. A change to
   "Context", "Consequences", "History" or the front matter needs no acceptance.
4. A separate ADR is written when a decision is new, or when the change needs its own context. It
   then supersedes the old record, or it amends the old record and the old text changes in the same
   commit. "Amended" then means: the amending ADR holds the why, and the base ADR holds the current
   what.
5. An ADR that no decision needs gets the status `Withdrawn` or `Superseded`. Its file moves to
   `docs/adr/archive/`. `docs/adr/README.md` lists the number in one line under "Archived", so that
   the number is never used again.
6. `AdrGateTests` reads `docs/adr/0*.md` only, therefore the archive needs no gate. The index test
   accepts a row under "Archived" for a number with no file in `docs/adr/`.

**[Recommended]** The work list. Each fold changes a "Decision" section, therefore the owner accepts
each one. This work is a second task, after the rules task, so that each fold is one commit that the
owner can read.

| ADR | Today | Change |
|---|---|---|
| 0003 | `Withdrawn` | Move to `docs/adr/archive/`. |
| 0004 | `Amended` by 0011 | Fold the deployment default into the text. Add History. |
| 0006 | `Amended` by 0008 and 0020 | Fold the `CommandResult` shape from ADR-0008 (`:201`) and the version meaning from ADR-0020. Add History. The fold of ADR-0020 waits for TASK-0035. |
| 0007 | `affects: BlazorApp/**` | Correct the front matter. |
| 0008 | `Amended` by 0020 | Fold the version meaning. Move its section "Amendment to ADR 0006" into ADR-0006. Waits for TASK-0035. |
| 0010 | `Amended` by 0020 | Fold `seq` in the reset snapshot. Waits for TASK-0035. |
| 0011 | `Amended` by 0019 | ADR-0019 rewrites the default, therefore ADR-0019 supersedes ADR-0011, and ADR-0011 moves to the archive. `CLAUDE.md` then points at ADR-0019 only. |
| 0012 | `Amended` by 0018 and 0021, with "Amendment 1" in the text | Fold "Amendment 1" into sections 1 and 6. Fold ADR-0018 after TASK-0028 and ADR-0021 after TASK-0037. |
| 0013 | `Amended` by 0016 | Fold the construction step. Add History. |
| 0015 | `Proposed` | Register entry R-0025 decides. No change until then. |
| 0018 to 0021 | `Accepted`, code not yet changed | The "vice versa": TASK-0028, 0035, 0037 and 0038 make the code agree with each record. The gate of finding F17 holds each one to its task. |

### 4.8 How a rule is made

**[Observed]** Each rule came from an incident. Register entry R-0004 gave the dependency gate (v0.19).
R-0011 gave the reserve gate (v0.26). R-0017 gave the write-set gate (v0.22). Ledger entry v0.24 gave
"read the file before you cite it" (`docs/CURRENT-STATE.md:518-520`). R-0002 gave "do not run the
gate on your computer". Each incident added a rule, and no incident removed one. Nineteen rules in
the inventory serve no objective and no anti-objective (section 2, column "Serves"): H7, H9, W18,
W20, W21, V2, V4, T12, I1, I4, G2, Q1, D2, D3, D4, M2, M4, P4, P6. Twelve of them go with the
proposal. Seven stay because they serve a decision or the register: H9, I1, G2, D3, D4, M4, P6.

**[Inferred]** A rule with no gate protects nothing after the session that wrote it, because the
next session reads four files. A rule with no objective behind it has no test for its own removal.

**[Recommended]** The rule for rules, in `docs/templates.md`:

1. A rule is born from one of two sources: an objective or an anti-objective in the charter, or a
   register entry that closed with the exit "decided". The rule names its source.
2. A rule has one of three forms: a gate, a form field that a gate reads, or one line of text.
3. A text rule that an agent broke two times becomes a gate, or the rule is deleted. This is rule 6.3
   of the working agreement, kept.
4. Each rule has one home. A second file points at the home in one line.
5. The rules review repeats with each codebase review. It repeats the measures of section 1, so that
   growth is visible.

## 5 Measures, after the proposal

Each value is an estimate for the state after each question in section 6 receives "yes". The method
is the method of section 1, therefore the next review can repeat each count.

| Measure | Today | After | Method |
|---|---|---|---|
| Rule files | 13 | 10 | Section 4.1. |
| Lines in the rule files and the CI files | 2,971 | about 2,550 | The estimates in section 4.1, plus `ci.yml` at about 200 and the ledger at about 830. |
| Sentences with "must" | 52 | about 38 | The deleted duplicates hold fourteen. |
| Rules in the inventory | 124 | 93 | Section 2: 124 rows, less 31 rows marked deleted. |
| Rules that a gate enforces in full | 30 | 30 | Ten deleted rows had a gate: W2, W5, W11, W19, W23, T1, T3, C10, C18, C26. Ten rows gain a gate from section 4.3: C9, C13, C30, D1, D3, I4, M4, T5, T9, T10. |
| Rules that a gate enforces in part | 24 | 15 | The `partial` rows that are not deleted, less the six that section 4.3 converts. |
| Rules in text only | 70 | 48 | The `none` rows that are not deleted and not converted. Twelve of the 48 are in the charter. The rest are judgement rules, one line each in `CLAUDE.md` or `docs/templates.md`. |
| Files that a session reads to find its position | 18 | 17 | `docs/working-agreement.md` leaves the list. The twelve ADRs stay until an `affects` field narrows (question Q3). With a narrower `affects` on ADR-0004 and ADR-0011, the count for TASK-0034 falls to 15. |
| Open register entries | 10 of 15 | 8 of 15 | R-0019 and R-0026 close. |
| Gate classes | 13 | 17 | Four new classes. The trailer rule is a test in the existing class. |
| ADRs in force whose `enforced-by` names an absent file | 4 | 4, gated | The gate permits each one while its task is `Ready`. |

## 6 Questions for the owner

Each question has a recommendation. An answer of "yes" accepts the recommendation.

The owner gave four directions on 2026-09-25, after the first summary of this review:

- Show the organization that removes each duplicate. Section 4.0 gives it.
- The rules that contradict the practice: examine how each one was made and for what purpose, and
  change the rule when the purpose asks for it. Section 4.6 gives each rule again.
- A statement that does not need to exist is cleaned. Findings F26 to F34 list each one, and the
  rules task corrects each one.
- An ADR that evolved is updated with the code, and the code with the ADR. An ADR that nobody needs
  is cleaned. Section 4.7 gives the rule and the work list.

Questions Q3 and Q8 changed with these directions. Question Q13 is new.

**Q1. Delete `docs/working-agreement.md` and `docs/conventions.md`, and move each live rule as
section
4.1 says?** Recommendation: yes. The rationale text moves to `docs/archive/`. Alternative: keep both
files and remove the fourteen duplicates from the working agreement, which leaves two more files to
read.

**Q2. Delete `docs/open-questions.md`?** Recommendation: yes. Both entries are closed register
entries.

**Q3. Accept the living-ADR rule of section 4.7, and the work list as a second task after the rules
task?** Recommendation: yes. Each fold is one commit that changes one "Decision" section, and the
owner accepts each one. ADR-0019 supersedes ADR-0011.

**Q4. Run the contract gate on each push, against the previous tip, and change rule W22 to "each
push
is green"?** Recommendation: yes. The rule then asks nothing of the owner, and the gate sees each
merge into `main`.

**Q5. Accept that the ledger entry of this work is the sixth after v0.28, and that the next
milestone
is the codebase review?** Recommendation: yes. The alternative is a gate that counts code milestones
only, and that needs a field in each ledger entry.

**Q6. Keep anti-objective 10 as it is until R-0025 is decided?** Recommendation: yes. The kernel
stamp
of the architecture challenge is a decision about ADR-0015, and R-0025 holds it with a limit.

**Q7. Use one status vocabulary: tasks use `Ready`, `Done` and `Deferred`; the roadmap loses its
"Status" column and keeps the "Shipped" list?** Recommendation: yes.

**Q8. Accept the five rules as section 4.6 writes them?** Recommendation: yes. Each one keeps its
purpose and drops the mechanism that the practice contradicts.

**Q9. Define `V1` as v0.1 to v0.6 and `V1.x` as v0.7 to v0.15 in the glossary, and leave ADR-0001 to
ADR-0015 as they are?** Recommendation: yes. `docs/CHARTER.md:118` then has a definition behind it.

**Q10. Add the five gates and the five gate corrections of section 4.3?** Recommendation: yes. Each
gate is verified by injection before its commit. Answer with the numbers of the gates that you
refuse,
or "yes" for all.

**Q11. Change the write-set gate so that only the task in the commit trailer governs the commit?**
Recommendation: yes. This closes R-0026 with a form that exists. A commit with no trailer fails.

**Q12. Accept the target of 120 lines for `CLAUDE.md` and the section order of 4.2?**
Recommendation:
yes. Each deleted section has a row in section 2 that names its new home.

**Q13. Accept the rule for rules of section 4.8, in `docs/templates.md`?** Recommendation: yes. It
stops the growth that this review measures, and it gives each later review the same counts.
