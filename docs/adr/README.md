# Architectural Decision Records

This index is the entry point for ADRs. Find the relevant decision; do not read all.

## Status legend

- **Accepted** — in force.
- **Amended** — modified by a later ADR; both apply, see "Amended by".
- **Superseded** — replaced by a later ADR; do not apply.
- **Proposed** — not yet accepted; informational.

## Index

| # | Title | Status | Topic | Amends | Amended by |
|---|---|---|---|---|---|
| [0001](0001-kernel-abstraction-capabilities.md) | Geometry kernel abstraction by capabilities | Accepted | Boundary, geometry | — | — |
| [0002](0002-headless-first-cli-as-canonical-client.md) | Headless-first; CLI as canonical client | Accepted | Workflow, clients | — | — |
| [0003](0003-blazor-as-thin-viewer.md) | Blazor as thin viewer | Accepted | Clients | — | — |
| [0004](0004-engine-runtime-is-authority.md) | Engine Runtime is the authoritative controller | Accepted | Boundary, authority | — | — |
| [0005](0005-event-stream-and-replay.md) | Event stream and replay protocol | Accepted | Contracts, observability | — | — |
| [0006](0006-command-execution-model.md) | Command execution model | Accepted | Contracts, runtime | — | 0008 |
| [0007](0007-ui-ephemeral-state-boundary.md) | UI ephemeral state boundary | Accepted | Boundary, clients | — | — |
| [0008](0008-command-query-event-triad.md) | Command / Query / Event triad and structured results | Accepted | Contracts | 0006 (extended `CommandResult` shape) | — |
| [0009](0009-3dengine-core-peer-render-kernel.md) | `3DEngine.Core` is a peer render kernel | Accepted | Boundary, clients | — | — |
| [0010](0010-subscription-reset-snapshot-format.md) | `subscription.reset` snapshot format | Accepted | Contracts, observability | — | — |

## Topics

- **Contracts** — 0005, 0006, 0008. Read these before changing `Engine.Contracts/**`.
- **Boundary** — 0001, 0004, 0007. Read these before changing project references or adding clients.
- **Workflow** — 0002. Read before adding a client or skipping CLI.
- **Clients** — 0002, 0003, 0007. Read before changing UI/CLI/host code.
- **Geometry** — 0001. Read before adding a capability or backend.
- **Observability** — 0005. Read before changing event ordering, retention, or subscription.

## Adding an ADR

1. Copy the most recent ADR as a template.
2. Number sequentially. Status `Proposed` until accepted.
3. If amending an earlier ADR, add the entry in its "Amended by" column above.
4. Update this index in the same PR.

## Pending

_None._
