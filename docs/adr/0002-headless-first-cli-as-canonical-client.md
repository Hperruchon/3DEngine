# ADR 0002 — Headless-First, CLI as Canonical Client

## Status

Accepted — 2026-04-28

## Context

The platform must be operable by humans (UI), scripts (CLI), services (API), and AI agents — all through the same command system. If the desktop UI accumulates behavior the CLI cannot reproduce, headless control erodes silently and the multi-client promise collapses within months.

Command-driven architecture is only credible when enforced. Aspirational rules in documentation do not survive feature pressure.

## Decision

**Every user-visible feature must be reachable through the CLI/API. The CLI is the canonical client for tests. UI clients contain no business logic.**

1. **Command bus is the only mutator.** All state changes — without exception — flow through `CommandBus.Apply(Command)`. UI input is translated to commands; the UI does not mutate the Document directly.

2. **CLI parity is mandatory.** Every command registered in the engine must be invokable via `engine apply <command.json>`. There is no "UI-only" command class.

3. **CLI is the test client.** Integration tests target the CLI (or the in-process equivalent of the same command bus). Replay fixtures live in `tests/fixtures/` and run on every PR.

4. **PR gate.** A PR introducing user-visible behavior must include a CLI test exercising it. CI rejects PRs that add a command handler without a corresponding CLI scenario.

5. **Contract-diff guard.** A PR that changes `Engine.Contracts` without an ADR reference is rejected by CI. Contracts are the public API.

6. **"If it only works in the UI, it is not implemented."** This is a binding rule, not a slogan. Bug reports against UI-only features are closed as "not implemented."

## Consequences

- The HTTP API surface (`Engine.Api.Http`) is promoted into V1, since Blazor and external agents depend on it. The CLI may run in-process for fastest tests; the HTTP API exists for remote clients.
- Some interactions feel awkward as commands at first (e.g., drag-to-translate becomes a `Translate` command on drag end, not per-frame). This is correct — interactive previews are local UI state, not Document state. See ADR 0004.
- The desktop app and Blazor become *thin*. That is intended.
- Test investment is front-loaded. The replay fixture suite is the architecture's regression test.

## Non-goals

- Removing all client-side state. UI clients legitimately own ephemeral state (camera, selection set, drag preview, hover highlight). That is not business logic. See ADR 0004 for the boundary.
- Building a fully featured CLI UX in V1. The CLI is correct, scriptable, and complete in coverage; ergonomic flags can come later.
- Forcing the HTTP API to expose anything beyond what the CLI exposes. The CLI is the floor, not a subset.

## Validation rules

1. CI step: every command type registered in `CommandRegistry` has at least one CLI scenario test in `tests/cli/`. Missing tests fail the build.
2. CI step: any PR touching `Engine.Contracts` references an ADR id in the PR body or fails.
3. CI step: a "headless smoke" job runs the CLI against every fixture project, asserting deterministic outcomes, with no UI binaries built.
4. Code review checklist item: "Could this PR introduce behavior reachable only via the desktop or Blazor client?" If yes, request a CLI test.

## Open challenges

- Truly interactive operations (gizmo manipulation, sketch dragging) need a clear protocol for "ephemeral preview vs committed command." Defining this is part of the V1 desktop UI work; until defined, prefer commit-on-release semantics.
- Long-running commands (boolean of large meshes) need a cancellation contract. Provisional: commands return `CommandResult` with `Status ∈ { Applied, Rejected, Cancelled }` and may be cancelled via a token threaded through the bus.
