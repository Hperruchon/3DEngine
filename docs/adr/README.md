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
| [0004](0004-engine-runtime-is-authority.md) | Engine Runtime is the authoritative controller | Amended | Boundary, authority | — | 0011 |
| [0005](0005-event-stream-and-replay.md) | Event stream and replay protocol | Accepted | Contracts, observability | — | — |
| [0006](0006-command-execution-model.md) | Command execution model | Amended | Contracts, runtime | — | 0008 |
| [0007](0007-ui-ephemeral-state-boundary.md) | UI ephemeral state boundary | Accepted | Boundary, clients | — | — |
| [0008](0008-command-query-event-triad.md) | Command, query and event triad with structured results | Accepted | Contracts | 0006 | — |
| [0009](0009-3dengine-core-peer-render-kernel.md) | 3DEngine.Core is a peer render kernel | Accepted | Boundary, clients | — | — |
| [0010](0010-subscription-reset-snapshot-format.md) | subscription.reset snapshot format | Accepted | Contracts, observability | — | — |
| [0011](0011-server-default-deployment-topology.md) | Server-default deployment, embed for offline | Accepted | Clients, deployment | 0004 | — |
| [0012](0012-geometry-backend-wiring.md) | Geometry backend wiring | Accepted | Boundary, geometry, contracts | — | — |
| [0013](0013-command-query-schema-declaration.md) | Command and query schema declaration | Amended | Contracts, observability | — | 0016 |
| [0014](0014-manifold-native-interop.md) | Manifold backend native-interop posture | Accepted | Boundary, geometry, native-interop | — | — |
| [0016](0016-handler-declared-construction.md) | Handler-declared command and query construction | Accepted | Contracts, clients | 0013 | — |

## Topics

- **Contracts** — 0005, 0006, 0008, 0012, 0013. Read these before changing `Engine.Contracts/**`.
- **Boundary** — 0001, 0004, 0007, 0012, 0014. Read these before changing project references or adding clients.
- **Workflow** — 0002. Read before adding a client or skipping CLI.
- **Clients** — 0002, 0003, 0007. Read before changing UI/CLI/host code.
- **Geometry** — 0001, 0012, 0014. Read before adding a capability or backend.
- **Observability** — 0005, 0010, 0013. Read before changing event ordering, retention, subscription, or schema endpoints.

## Adding an ADR

1. Copy the most recent ADR as a template.
2. Number sequentially. Status `Proposed` until accepted.
3. If amending an earlier ADR, add the entry in its "Amended by" column above.
4. Update this index in the same PR.

## Pending

- **0015** is reserved for command-log persistence. That document exists on the branch
  `claude/happy-booth-1cef3f`. Milestone P0.5 merges it. The gap in the numbers is intentional.
