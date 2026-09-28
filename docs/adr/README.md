# Architectural Decision Records

This index is the entry point for ADRs. Read the ones that the field `governed-by` of your task
names. Do not read all of them.

## Status legend

- **Accepted** — in force. The text agrees with the code.
- **Amended** — a later ADR gives the reason for a change, see "Amended by". This record holds the current decision.
- **Superseded** — replaced by a later ADR; do not apply.
- **Proposed** — not yet accepted; informational.
- **Withdrawn** — the author took a proposal back, or the owner took back a decision that no later ADR replaces; do not apply.
- **Rejected** — the project examined the proposal and refused it; do not apply.

## Index

| # | Title | Status | Topic | Amends | Amended by |
|---|---|---|---|---|---|
| [0001](0001-kernel-abstraction-capabilities.md) | Geometry kernel abstraction by capabilities | Accepted | Boundary, geometry | — | — |
| [0002](0002-headless-first-cli-as-canonical-client.md) | Headless-first; CLI as canonical client | Accepted | Workflow, clients | — | — |
| [0003](0003-blazor-as-thin-viewer.md) | Blazor as thin viewer | Withdrawn | Clients | — | — |
| [0004](0004-engine-runtime-is-authority.md) | Engine Runtime is the authoritative controller | Amended | Boundary, authority | — | 0011 |
| [0005](0005-event-stream-and-replay.md) | Event stream and replay protocol | Accepted | Contracts, observability | — | — |
| [0006](0006-command-execution-model.md) | Command execution model | Amended | Contracts, runtime | — | 0008, 0020 |
| [0007](0007-ui-ephemeral-state-boundary.md) | UI ephemeral state boundary | Accepted | Boundary, clients | — | — |
| [0008](0008-command-query-event-triad.md) | Command, query and event triad with structured results | Amended | Contracts | 0006 | 0020 |
| [0009](0009-3dengine-core-peer-render-kernel.md) | 3DEngine.Core is a peer render kernel | Accepted | Boundary, clients | — | — |
| [0010](0010-subscription-reset-snapshot-format.md) | subscription.reset snapshot format | Amended | Contracts, observability | — | 0020 |
| [0011](0011-server-default-deployment-topology.md) | Server-default deployment, embed for offline | Amended | Clients, deployment | 0004 | 0019 |
| [0012](0012-geometry-backend-wiring.md) | Geometry backend wiring | Amended | Boundary, geometry, contracts | — | 0018, 0021 |
| [0013](0013-command-query-schema-declaration.md) | Command and query schema declaration | Amended | Contracts, observability | — | 0016 |
| [0014](0014-manifold-native-interop.md) | Manifold backend native-interop posture | Accepted | Boundary, geometry, native-interop | — | — |
| [0015](0015-command-log-persistence.md) | Command-log persistence for engine-api-http | Proposed | Persistence, runtime, deployment | — | — |
| [0016](0016-handler-declared-construction.md) | Handler-declared command and query construction | Accepted | Contracts, clients | 0013 | — |
| [0017](0017-first-party-vulkan-layer.md) | The first-party Vulkan layer | Accepted | Boundary, rendering | — | — |
| [0018](0018-tessellation-capability.md) | The tessellation capability | Accepted | Geometry, contracts, rendering | 0012 | — |
| [0019](0019-hybrid-deployment-topology.md) | The hybrid deployment topology | Accepted | Clients, deployment | 0011 | — |
| [0020](0020-document-version-meaning.md) | The meaning of the document version | Accepted | Contracts, runtime, observability | 0006, 0008, 0010 | — |
| [0021](0021-operand-consumption.md) | Operand consumption and the live body set | Accepted | Contracts, geometry, observability | 0012 | — |

## Topics

- **Contracts** — 0005, 0006, 0008, 0012, 0013, 0018, 0020, 0021. Read these before changing `Engine.Contracts/**`.
- **Boundary** — 0001, 0004, 0007, 0012, 0014, 0017. Read these before changing project references or adding clients.
- **Workflow** — 0002. Read before adding a client or skipping CLI.
- **Clients** — 0002, 0007, 0011, 0019. Read before changing UI/CLI/host code.
- **Deployment** — 0011, 0019. Read these before changing where a session runs or which transport a host uses.
- **Geometry** — 0001, 0012, 0014, 0018, 0021. Read before adding a capability or backend.
- **Observability** — 0005, 0010, 0013, 0020, 0021. Read before changing event ordering, retention, subscription, or schema endpoints.
- **Rendering** — 0009, 0017, 0018. Read these before changing `3DEngine.Core`, `3DEngine.Vulkan` or `3DEngine`.

## Adding an ADR

1. Use the form in `docs/templates.md`, section 1.
2. Number sequentially. The status is `Proposed` until the owner accepts the record.
3. If the record amends an earlier ADR, add the entry in its "Amended by" column above, and change
   the text of the earlier ADR in the same commit (`docs/templates.md`, section 1, "An ADR agrees
   with the code").
4. Update this index in the same commit. `AdrGateTests` fails when a row and a record disagree.

## Archived

No record is archived yet. A withdrawn or superseded record moves to `docs/adr/archive/`, and its
number stays in this list so that the number is never used again.
