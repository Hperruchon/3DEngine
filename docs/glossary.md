# Glossary

Canonical vocabulary. One line per term + a pointer to where it is **defined** (file) and **decided** (ADR). Definitions here are nominal only — the *why* and the *rules* live in the linked ADR; the *current shape* lives in the code. Names listed are locked to what exists in `Engine.Contracts/` and `Engine.Core/` today; do not rename without an ADR (see [templates.md](templates.md), section 1).

## Triad

Each change to persistent state must use a command. A query must not change state. An event is an
observation of a change, and it appears only in the event stream.

| Term | Meaning | Defined / decided |
|---|---|---|
| **Command** | The only mutator. Serializable, versioned (`SchemaVersion`), replayable; carries `CommandId` and optional `ExpectedDocumentVersion`. The engine records it in the log and can replay it. | `Engine.Contracts/Command.cs` · ADR-0006, ADR-0008 |
| **Query** | A read. Never logged, replayed, or streamed. Carries `QueryId` + `SchemaVersion`. | `Engine.Contracts/Query.cs` · ADR-0008 |
| **Event** | An observation of what happened, derived from commands; surfaces only via the event stream. The record type is `EventRecord`. | concept · ADR-0005 |

## Result envelopes

| Term | Meaning | Defined / decided |
|---|---|---|
| **CommandResult** | Structured outcome of an `Apply` — `Status` (`CommandStatus`: Applied/Rejected/Cancelled), `AppliedAtSeq`, `DocumentVersion`, `Outputs`, diagnostics, optional `Error`. | `Engine.Contracts/CommandResult.cs` · ADR-0008 (extends ADR-0006) |
| **QueryResult\<T\>** | Structured outcome of a query — `AsOfDocumentVersion`, typed `Result`, diagnostics, optional `Error`. | `Engine.Contracts/QueryResult.cs` · ADR-0008 |
| **Outputs** | Typed-bag of named result values (`IReadOnlyDictionary<string, object?>`) with `TryGet<T>`. Returned by command handlers. | `Engine.Contracts/Outputs.cs` · ADR-0008 |
| **Diagnostic** | A `Severity` + stable `Code` + message attached to a result (non-fatal channel). Codes are registered in [diagnostics.md](diagnostics.md). | `Engine.Contracts/Diagnostic.cs` · ADR-0008 |
| **ErrorDetail** | The fatal-error channel on a result — `Code`, message, optional `Cause`, `Retriable`. Present iff the command/query failed. | `Engine.Contracts/ErrorDetail.cs` · ADR-0008 |
| **EventRecord** | The event wire type — `Seq`, `Timestamp`, `DocumentId`, `CauseCommandId`, `Kind`, `Payload`. | `Engine.Contracts/EventRecord.cs` · ADR-0005 |

## State

| Term | Meaning | Defined / decided |
|---|---|---|
| **Document** | The design-truth aggregate: ordered command `Log` + materialized projections (`Bodies`) + metadata. Mutated only inside `CommandBus`'s commit section. | `Engine.Contracts/Document.cs` · ADR-0004 |
| **document version** | `Document.Version`: the count of applied commands, which is the count of entries in `Document.Log`. A rejected or cancelled command does not change it, so a replay of the log rebuilds it. A command states it in `ExpectedDocumentVersion`. It is not `Seq`: no code computes one from the other. | `Engine.Contracts/Document.cs` · ADR-0020, TASK-0035 |
| **Seq** | Monotonic per-Document event sequence number on `EventRecord`; the cursor for replay/reconnect. Each event takes one, a rejection too. The reset snapshot gives the highest one as `seq`, and a client continues from `seq + 1`. | `Engine.Contracts/EventRecord.cs` · ADR-0005, ADR-0020 |
| **document session** | The one serial boundary of one `Document`. It owns the Document, its backend, its command bus, its query bus and its event sink. A command, a query and a read of the Document enter one serial section, so a reader never sees a commit in the middle. Each host uses it. | `Engine.Core/DocumentSession.cs` · ADR-0008 §6, ADR-0014 §3 · TASK-0034 |
| **surface library** | `Engine.Api.Http`: the HTTP and WebSocket surface that a host mounts on a document session that the host owns, with `AddEngineSurface` and `MapEngineSurface`. Each host gets the same endpoints and the same address checks. The program `engine-api-http` and the desktop host (TASK-0054) are hosts of it. | `Engine.Api.Http/EngineSurface.cs` · ADR-0019 item 3 · TASK-0038 |

## Geometry

| Term | Meaning | Defined / decided |
|---|---|---|
| **BodyHandle** | Opaque identity for a geometry body (`Guid Id`). Deterministic from `CommandId` for single-body creates, so replay is byte-stable. | `Engine.Contracts/BodyHandle.cs` · ADR-0001, ADR-0012 |
| **Body** | Document-side body projection (`BodyRecord` = `Handle` + `Kind`): handles + minimum metadata only; the backend owns the geometry data. | `Engine.Contracts/BodyRecord.cs` · ADR-0012 §3 |
| **live body** | A body in `Document.Bodies`. A body is live from the command that creates it to the command that consumes it, and a consumed body never becomes live again. `Translate` consumes its source, and `Subtract` consumes both operands. A command on a body that is not live gets `E-GEOM-BODY-NOT-FOUND`. The order of `Document.Bodies` is not the order of creation after a body is consumed; it is the same for the same commands, so a replay agrees, but a client must not read it as history. | `Engine.Contracts/Document.cs` · ADR-0021 · TASK-0037, TASK-0058 |
| **IGeometryBackend** | The only kernel type a handler sees. Exposes `Capabilities` flags + `TryGet<T>()`; backends are caches, swapped by replay. | `Engine.Contracts/Geometry/IGeometryBackend.cs` · ADR-0001, ADR-0012 |
| **capability (`TryGet<T>`)** | A typed geometry interface negotiated at runtime via `IGeometryBackend.TryGet<T>()` (e.g. `IMeshOps`, `IGeometryQuery`, `ITransformOps`, `IBooleanOps`, `ITessellationOps`; reserved `IBRepOps`/`IFeatureIdMap`); declared by `BackendCapabilities`. Returns null, never silently falls back. | `Engine.Contracts/Geometry/` · ADR-0001 §1, ADR-0012 §1 (Amendment 1) |
| **tessellation** | The triangles of a body: `Positions` (x, y and z of each vertex, double precision) and `Indices` (three for each triangle, counter-clockwise when seen from outside). A presentation output of the query `GetTessellation`; it never enters a command, an event, the Document or the log. Only the native backend gives it. | `Engine.Contracts/Geometry/Tessellation.cs` · ADR-0018 · TASK-0028 |

## Rendering

| Term | Meaning | Defined / decided |
|---|---|---|
| **draw list** | The draws of one frame: one record for each entity that has a mesh, with the mesh and its model-view matrix relative to the eye. The host fills it from the scene graph, and the Vulkan layer executes it with one direct draw for each record. | `3DEngine.Core/Rendering/DrawList.cs` · ADR-0023 · TASK-0070 |
| **reversed depth** | The near plane gets depth 1 and the far plane depth 0, the depth buffer is cleared to 0, and a fragment with a greater depth wins. A float holds the most precision near 0, which this gives to the far range. | `3DEngine.Core/Models/Camera.cs` · ADR-0023 |
| **body origin** | The `Origin` of an `Entity` in double precision: the centre of the box of the mesh of a body, around which the host narrows the mesh to float (ADR-0024 item 2). | `3DEngine.Core/Models/Entity.cs` · ADR-0024 · TASK-0071 |
| **offscreen target** | A colour image and a depth image with their own render pass, and a copy of the colour to memory that the host reads. A device with no window draws into it. | `3DEngine.Vulkan/OffscreenTarget.cs` · ADR-0025 · TASK-0076 |
| **presenter** | The presentation of a device to a window: the swapchain, the resources of each frame, the acquire and the present. A device with no window has none. | `3DEngine.Vulkan/Presenter.cs` · ADR-0025 · TASK-0076 |
| **image test** | A test that draws a scene into an offscreen target and compares the picture by structure, with no reference picture. | `3DEngine.ImageTests/` · ADR-0025 · TASK-0076 |
| **intent** | What the person asks for, with no kind of device: orbit, pan, zoom, select or cancel. The intent layer gives it from the samples of touch, pen, mouse and keyboard. | `3DEngine.Core/Input/Intents.cs` · ADR-0026 · TASK-0082 |
| **sample** | One record of input with its kind of device, its device, its pointer, its position, its time and its phase; a wheel and a key have their own samples. The host collects the samples of one frame. | `3DEngine.Core/Input/InputSamples.cs` · ADR-0026 |
| **map of gestures** | The setting of the client that gives each gesture (a drag, a pinch, a tap, a key) its action. | `3DEngine.Core/Input/Intents.cs` · ADR-0026 |
| **relative to the eye** | Each position is moved by minus the eye of the camera in double precision before it is narrowed to float, so a body far from the origin of the engine keeps its detail. | `3DEngine.Core/Rendering/SceneWalker.cs` · ADR-0023 |

## Concepts

| Term | Meaning | Defined / decided |
|---|---|---|
| **Replay** | Reconstructing an equivalent `Document` (+ backend state) by re-applying a log on a fresh backend. Equivalence holds modulo `Timestamp`/`DocumentId`. | `Engine.Core/Replay.cs` · ADR-0005, ADR-0012 §2 |
| **projection** | Any state regenerable from the log — `Document.Bodies`, the event stream, render scene, snapshots. Losing a projection is recovery, not data loss. | concept · [CHARTER.md](CHARTER.md) · ADR-0012 §3 |
| **design truth** | The authoritative, ordered, replayable command log (the `Document`) that every other piece of state is downstream of. Owned by `Engine.*`. | concept · [CHARTER.md](CHARTER.md) · ADR-0004 |
| **objective** | A result that the project must achieve. There are twenty, and each one has a number. Only the owner changes one. | [CHARTER.md](CHARTER.md) "The twenty objectives" |
| **anti-objective** | A thing that the project refuses at each version. Each one protects an objective. Example: anti-objective 1, "no second source of truth", protects objective 17. | [CHARTER.md](CHARTER.md) "Anti-objectives" |
| **the two kernels** | `Engine.*` (design truth) and `3DEngine.Core` (render state) — peers that never reference each other; render hosts own the projection from events. | [CLAUDE.md](../CLAUDE.md) "Authority diagram" · ADR-0009 |
| **Vulkan layer** | `3DEngine.Vulkan` — the first-party GPU code. It draws render state and never sees design truth; only a host that draws references it. | `3DEngine.Vulkan/` · ADR-0017 |
| **V1** | The label of the first six milestones, v0.1 to v0.6: the engine spine, the command-line host and the first gates. ADR-0001 to ADR-0015 use it. | [CURRENT-STATE.md](CURRENT-STATE.md) v0.6 |
| **V1.x** | The label of the milestones v0.7 to v0.15: the HTTP surface, the first geometry slice and the Manifold backend. | [CURRENT-STATE.md](CURRENT-STATE.md) v0.14 |
