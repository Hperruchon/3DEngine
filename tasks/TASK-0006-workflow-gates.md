# TASK-0006 — Workflow gates (P5)

## Status

Ready

## Context

CLAUDE.md describes "the gate" as `dotnet build`, `dotnet test`, dependency direction check, diagnostic codes registered, replay determinism fixture. TASK-0003 shipped the diagnostics gate (in-tree); TASK-0005 shipped the replay-determinism gate (in-tree). The first two gates (build + test) run today on developer machines but have no automated trigger on PRs.

The repository has a GitHub remote (`https://github.com/Hperruchon/3DEngine`) but no `.github/` directory. PRs against `main` are reviewed by hand; nothing fails automatically. This task adds the workflow-level discipline that turns the existing gates into PR-time checks and adds two new gates that only make sense at PR time:

- A **contract-touched-needs-ADR** check that fails the PR when `Engine.Contracts/**` is modified without a new or updated ADR in the same PR. Mirrors CLAUDE.md §When unsure — stop and ask.
- A **headless-smoke** job that spawns the built CLI (`dotnet run --project Engine.Cli -- apply NoOp --param echo=hello`) and asserts the JSON output, catching process-boundary regressions that the in-process `Cli.Run` tests miss.

The other two workflow surfaces — PR template and CODEOWNERS — are passive: they document what good PRs look like and who owns each surface.

This is P5 from `docs/roadmap.md`: process discipline only; no `Engine.*` code changes.

## Goal

Add `.github/PULL_REQUEST_TEMPLATE.md`, `.github/CODEOWNERS`, and `.github/workflows/ci.yml`. The workflow runs the existing build/test gates on push and PR, plus the contract-gate and headless-smoke checks.

## Scope (in)

1. **`.github/PULL_REQUEST_TEMPLATE.md`** — sections: Summary, TASK reference, Boundaries checkboxes (Engine.Contracts / diagnostics / do-not-touch), Verification checkboxes (build / test / CURRENT-STATE / roadmap). Mirrors CLAUDE.md §Workflow close criteria.

2. **`.github/CODEOWNERS`** — `@Hperruchon` as catch-all reviewer, with explicit (redundant but documentary) entries for `/Engine.Contracts/`, `/docs/adr/`, and `/CLAUDE.md` to surface that touching them is structurally significant.

3. **`.github/workflows/ci.yml`** — single workflow, three jobs:
   - **`build-and-test`** — `actions/setup-dotnet@v4` with `dotnet-version: 10.x` and `include-prerelease: true`; runs `dotnet build` then `dotnet test --no-build`. Trigger: push to `main`, pull_request.
   - **`headless-smoke`** — same setup; runs `dotnet build`, then `dotnet run --no-build --project Engine.Cli -- apply NoOp --param echo=hello`; greps stdout for `"status": "Applied"` and `"echo": "hello"`. Trigger: push to `main`, pull_request.
   - **`contract-gate`** — fetches the PR base, runs `git diff --name-only origin/$base...HEAD` to detect changes under `Engine.Contracts/**` and `docs/adr/**`; fails the job if contracts changed without an ADR change. Trigger: pull_request only.

4. **Docs**
   - `docs/CURRENT-STATE.md` — v0.6 entry referencing this task.
   - `docs/roadmap.md` — move P5 from Pending V1 to Shipped (V1 Pending becomes empty).

## Scope (out)

- Any change to `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`, `Engine.Tests/**`. The phase is workflow surface only.
- Any new ADR.
- Any new diagnostic code.
- Loosening `global.json`'s `rollForward: disable`. CI uses `dotnet-version: 10.x` (latest available) instead of the pinned SDK, accepting that CI may run a slightly different patch from local. If the public feeds do not yet carry .NET 10 preview SDKs at the moment, that surfaces as a follow-up open question, not a phase blocker.
- Branch protection / required-status-check configuration. That is a GitHub settings change owned by the repo administrator; this PR defines the checks, branch protection consumes them.
- Wiring the dependency-direction gate (the second of CLAUDE.md's five gates that is still verbal). Separate task.
- A GitHub Actions matrix across OSes / SDK versions. One job, one OS (Ubuntu) is enough for V1.

## Inputs

- CLAUDE.md — Workflow section, gate list, "When unsure — stop and ask" rules, dependency rules.
- `docs/adr/README.md` — ADR conventions and the (now empty) Pending list.
- `docs/roadmap.md` — P5 entry.
- `Engine.Tests/Cli/CliApplyTests.cs` — expected stdout shape for the NoOp command.

## Outputs

- `.github/PULL_REQUEST_TEMPLATE.md` appears as the default PR body on new PRs.
- `.github/CODEOWNERS` exists with `@Hperruchon` as the catch-all reviewer.
- `.github/workflows/ci.yml` runs on push to `main` and on pull_request. The first push after this task lands triggers the first CI run.
- A PR that modifies a file under `Engine.Contracts/` without modifying anything under `docs/adr/` fails the contract-gate job.
- A PR that breaks the CLI's NoOp output fails the headless-smoke job.
- `docs/CURRENT-STATE.md` v0.6 entry.
- `docs/roadmap.md` updated: P5 moved Pending → Shipped. V1 Pending becomes empty.

## Files

**Created:**
- `.github/PULL_REQUEST_TEMPLATE.md`
- `.github/CODEOWNERS`
- `.github/workflows/ci.yml`
- `tasks/TASK-0006-workflow-gates.md` (this file)

**Modified:**
- `docs/CURRENT-STATE.md` — add v0.6 entry.
- `docs/roadmap.md` — move P5 to Shipped.
- `tasks/TASK-0006-workflow-gates.md` — flip Status to Done in close commit.

**Do not touch:**
- `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`, `Engine.Tests/**`.
- `docs/diagnostics.md` (no new codes).
- ADRs.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*`.

## Tests

No new tests. CI itself is the verification. `dotnet test` is still run locally before the impl commit to confirm doc/.github edits did not touch code by accident. The headless-smoke step doubles as a CLI scenario test at process scope on every PR.

## Acceptance criteria

1. `.github/` exists with `PULL_REQUEST_TEMPLATE.md`, `CODEOWNERS`, and `workflows/ci.yml`.
2. `dotnet build` succeeds locally (unchanged).
3. `dotnet test` passes 33 tests locally (unchanged).
4. The CI workflow YAML parses as valid (the user pushes the branch and observes the first run).
5. No file under `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`, or `Engine.Tests/**` is modified.
6. No new diagnostic code.
7. `docs/CURRENT-STATE.md` lists v0.6 with this task id.
8. `docs/roadmap.md` lists P5 under Shipped; V1 Pending is empty (advance to V1.x next).

## Notes for the implementer

- **SDK in CI.** The local `global.json` pins `10.0.300-preview.0.26177.108`, an internal preview that may not appear on the public `setup-dotnet` feed. The workflow uses `dotnet-version: 10.x` with `include-prerelease: true`, accepting that CI may install a different patch. If `setup-dotnet` cannot find any .NET 10 preview, CI fails and the user has a real signal — either bump global.json to a public SDK or wait for one. Documented as an explicit risk rather than worked around.
- **CODEOWNERS in a solo repo.** A single owner doesn't gate review (GitHub doesn't require a separate reviewer when the only owner is the PR author). The file is documentation of architectural-surface ownership, not enforcement.
- **`headless-smoke` greps stdout.** `jq` would be cleaner but adds a tool dependency on the runner; two `grep -q` substrings against `"status": "Applied"` and `"echo": "hello"` are sufficient to catch a broken CLI without false positives.
- **`contract-gate` uses three-dot diff.** `git diff origin/$base...HEAD` (three dots) shows changes on the PR branch *since* it diverged from base; that is the right semantic. Two-dot diff would also flag changes pulled in from base.
- **PR template path.** Uppercase `.github/PULL_REQUEST_TEMPLATE.md` is the more visible convention.
- **Workflow runs on the branch this PR creates.** That means the first CI run for this very PR exercises the new workflow. If something's wrong with the YAML, the user sees it immediately.
