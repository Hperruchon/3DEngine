# Architectural Decision Records

This index is the entry point for ADRs. Read the ones that the field `governed-by` of your task
names. Do not read all of them.

## Status legend

- **Accepted** — in force. The text agrees with the code.
- **Amended** — a later ADR gives the reason for a change, see "Amended by". This record holds the current decision.
- **Superseded** — replaced by a later ADR; do not apply. The record is in the archive.
- **Proposed** — not yet accepted; informational.
- **Withdrawn** — the author took a proposal back, or the owner took back a decision that no later ADR replaces; do not apply. The record is in the archive.
- **Rejected** — the project examined the proposal and refused it; do not apply.

## Index

| # | Title | Status | Topic | Amends | Amended by |
|---|---|---|---|---|---|
| [0001](0001-kernel-abstraction-capabilities.md) | Geometry kernel abstraction by capabilities | Accepted | Boundary, geometry | — | — |
| [0002](0002-headless-first-cli-as-canonical-client.md) | Headless-first; CLI as canonical client | Accepted | Workflow, clients | — | — |
| [0004](0004-engine-runtime-is-authority.md) | Engine Runtime is the authoritative controller | Accepted | Boundary, authority | — | — |
| [0005](0005-event-stream-and-replay.md) | Event stream and replay protocol | Accepted | Contracts, observability | — | — |
| [0006](0006-command-execution-model.md) | Command execution model | Amended | Contracts, runtime | — | 0008, 0020, 0031 |
| [0007](0007-ui-ephemeral-state-boundary.md) | UI ephemeral state boundary | Accepted | Boundary, clients | — | — |
| [0008](0008-command-query-event-triad.md) | Command, query and event triad with structured results | Amended | Contracts | 0006 | 0020 |
| [0009](0009-3dengine-core-peer-render-kernel.md) | 3DEngine.Core is a peer render kernel | Amended | Boundary, clients | — | 0022, 0025 |
| [0010](0010-subscription-reset-snapshot-format.md) | subscription.reset snapshot format | Amended | Contracts, observability | — | 0020, 0015 |
| [0012](0012-geometry-backend-wiring.md) | Geometry backend wiring | Amended | Boundary, geometry, contracts | — | 0018, 0021, 0028, 0015, 0033, 0037 |
| [0013](0013-command-query-schema-declaration.md) | Command and query schema declaration | Accepted | Contracts, observability | — | — |
| [0014](0014-manifold-native-interop.md) | Manifold backend native-interop posture | Accepted | Boundary, geometry, native-interop | — | — |
| [0015](0015-command-log-persistence.md) | The document file | Accepted | Persistence, contracts | 0010, 0012 | — |
| [0016](0016-handler-declared-construction.md) | Handler-declared command and query construction | Accepted | Contracts, clients | — | — |
| [0017](0017-first-party-vulkan-layer.md) | The first-party Vulkan layer | Amended | Boundary, rendering | — | 0022, 0025 |
| [0018](0018-tessellation-capability.md) | The tessellation capability | Accepted | Geometry, contracts, rendering | 0012 | — |
| [0019](0019-hybrid-deployment-topology.md) | The hybrid deployment topology | Amended | Clients, deployment | — | 0024 |
| [0020](0020-document-version-meaning.md) | The meaning of the document version | Accepted | Contracts, runtime, observability | 0006, 0008, 0010 | — |
| [0021](0021-operand-consumption.md) | Operand consumption and the live body set | Amended | Contracts, geometry, observability | 0012 | 0028, 0031, 0033, 0037 |
| [0022](0022-first-render-pipeline.md) | The first render pipeline | Amended | Rendering, boundary | 0009, 0017 | 0025 |
| [0023](0023-camera-depth-and-draw-list.md) | The camera, the depth and the draw list of the renderer | Amended | Rendering | — | 0024, 0026 |
| [0024](0024-in-process-events-and-body-origin.md) | The events of a session in its host, and the origin of a body in double precision | Accepted | Rendering, clients | 0019, 0023 | — |
| [0025](0025-observe-a-cut.md) | The first demonstration, a device with no window, and an image test | Amended | Rendering, boundary | 0009, 0017, 0022 | 0029 |
| [0026](0026-intent-layer.md) | Device-tagged pointer samples, an intent layer, and a parity test of touch and mouse | Amended | Rendering, clients | 0023 | 0027, 0029, 0032, 0034, 0038 |
| [0027](0027-text-and-panels.md) | Text and panels through Dear ImGui, drawn by a first-party renderer | Amended | Rendering, clients | 0026 | 0029 |
| [0028](0028-body-name-and-visibility.md) | The name and the visibility of a body | Amended | Contracts, geometry | 0012, 0021 | 0037 |
| [0029](0029-selection-and-hide-gestures.md) | The selection, the hide gestures and the frame of the camera in the desktop | Amended | Rendering, clients | 0025, 0026, 0027 | 0034 |
| [0030](0030-document-file-in-the-desktop.md) | The document file in the desktop and for an agent | Accepted | Persistence, deployment | — | — |
| [0031](0031-undo-and-redo.md) | Undo and redo as commands of the log | Amended | Contracts, runtime | 0006, 0021 | 0032, 0033, 0037 |
| [0032](0032-numeric-pad-and-evaluator.md) | The numeric pad, the expression evaluator and the keys of undo and redo | Amended | Clients, rendering | 0026, 0031 | 0034 |
| [0033](0033-sketch-model.md) | The sketch model in the engine | Amended | Contracts, sketch | 0012, 0021, 0031 | 0035 |
| [0034](0034-sketch-tools.md) | The sketch tools of the desktop | Amended | Clients, sketch | 0026, 0029, 0032 | 0041 |
| [0035](0035-sketch-solver.md) | The constraint solver of a sketch | Accepted | Sketch, determinism | 0033 | — |
| [0036](0036-extrude-of-a-sketch-profile.md) | The extrude of a closed profile of a sketch | Amended | Contracts, geometry, sketch | — | 0037, 0040 |
| [0037](0037-rebuild-of-an-extrude.md) | The rebuild of an extrude after an edit of its sketch | Amended | Contracts, geometry, sketch | 0012, 0021, 0028, 0031, 0036 | 0039 |
| [0038](0038-translate-gizmo.md) | The translate gizmo of the desktop | Accepted | Rendering, clients | 0026 | — |
| [0039](0039-chain-of-a-consumed-extrude.md) | The rebuild of the chain of a consumed extrude | Accepted | Geometry, sketch | 0037 | — |
| [0040](0040-extrude-of-curves.md) | The extrude of a profile with arcs and circles | Accepted | Geometry, sketch | 0036 | — |
| [0041](0041-snap-of-a-sketch-tap.md) | The snap of a tap of the sketch | Accepted | Clients, sketch | 0034 | — |

## Topics

- **Contracts** — 0005, 0006, 0008, 0012, 0013, 0015, 0018, 0020, 0021, 0028, 0033. Read these before changing `Engine.Contracts/**`.
- **Boundary** — 0001, 0004, 0007, 0012, 0014, 0017, 0022, 0025. Read these before changing project references or adding clients.
- **Workflow** — 0002. Read before adding a client or skipping CLI.
- **Clients** — 0002, 0007, 0019, 0024, 0026, 0027, 0029, 0030, 0032, 0034. Read before changing UI/CLI/host code.
- **Deployment** — 0004, 0019, 0030. Read these before changing where a session runs or which transport a host uses.
- **Geometry** — 0001, 0012, 0014, 0018, 0021, 0028. Read before adding a capability or backend.
- **Observability** — 0005, 0010, 0013, 0020, 0021. Read before changing event ordering, retention, subscription, or schema endpoints.
- **Rendering** — 0009, 0017, 0018, 0022, 0023, 0024, 0025, 0026, 0027, 0029. Read these before changing `3DEngine.Core`, `3DEngine.Vulkan` or `3DEngine`.

## Adding an ADR

1. Use the form in `docs/templates.md`, section 1.
2. Number sequentially. The status is `Proposed` until the owner accepts the record.
3. If the record amends an earlier ADR, add the entry in its "Amended by" column above, and change
   the text of the earlier ADR in the same commit (`docs/templates.md`, section 1, "An ADR agrees
   with the code").
4. Update this index in the same commit. `AdrGateTests` fails when a row and a record disagree.

## Archived

A withdrawn or superseded record moves to `docs/adr/archive/`, and its number stays in this list so
that the number is never used again. Do not apply an archived record. `AdrGateTests` fails when a
number in this list still has a file in `docs/adr/`, and when a record in `docs/adr/` is withdrawn or
superseded.

| # | Title | Status | Replaced by | Archived |
|---|---|---|---|---|
| [0003](archive/0003-blazor-as-thin-viewer.md) | Blazor as thin viewer | Withdrawn | — | 2026-10-27, TASK-0040 |
| [0011](archive/0011-server-default-deployment-topology.md) | Server-default deployment, embed for offline | Superseded | 0019 | 2026-10-27, TASK-0040 |
