# Diagnostic code registry

All `E-`/`W-`/`I-` codes used in `Engine.*` code MUST appear here. A code is never removed and never changes meaning. A row may move between the two tables, and its text may change to say where the code is raised. Per ADR-0008 §4.

## Conventions

- **Severity prefix:** `E-` error · `W-` warning · `I-` info
- **Subsystem token:** `CMD` (command bus), `QRY` (query bus), `API` (HTTP transport), `GEOM` (geometry), `IO` (persistence/transport), `VAL` (validation). Add new tokens as subsystems land.
- **Format:** `<severity>-<subsystem>-<short-tag>` — short, kebab-style, stable.

## Active codes

| Code | Severity | Meaning | Where raised |
|---|---|---|---|
| `E-CMD-UNKNOWN` | Error | Command name + schema version is not registered in `CommandRegistry`. | `CommandBus.Apply` when no handler matches. |
| `E-CMD-VERSION-STALE` | Error | Submitted command's `ExpectedDocumentVersion` does not match the Document's current `Version`. | `CommandBus.Apply` optimistic check. |
| `E-CMD-BUS-BUSY` | Error | Inbound command queue is full; submission rejected without enqueueing. | Reserved for the transport task; not raised in P0 (no inbound queue yet). |
| `E-QRY-UNKNOWN` | Error | Query name + schema version is not registered in `QueryRegistry`. | `QueryBus.Query` when no handler matches. |
| `E-API-BAD-REQUEST` | Error | HTTP request body was malformed, missing a required field, or wrong content-type. Never reaches the bus. | `Engine.Api.Http` endpoints, returned with HTTP `400` or `415`. |
| `E-API-WS-INVALID-SUBSCRIBE` | Error | WebSocket subscribe frame is malformed, missing, or wrong shape. The connection is closed before any subscription is established. | `Engine.Api.Http` `/events`, WebSocket close status `1003` with this reason. |
| `W-API-WS-LAGGED` | Warning | A WebSocket subscriber's outbound queue overflowed (per ADR-0005 §6). The engine disconnects that subscriber; other subscribers are unaffected. | `Engine.Api.Http` `/events`, WebSocket close status `1008` with reason `subscriber.lagged`. |
| `E-GEOM-CAP-MISSING` | Error | The active geometry backend does not implement a capability interface (`IMeshOps`, `IGeometryQuery`, etc.) that the handler requested. No silent fallbacks (ADR-0001 §4). | `Engine.Core` command/query handlers when `backend.TryGet<T>()` returns null. |
| `E-GEOM-INVALID-PARAM` | Error | Geometry command rejected because a parameter is out of range (e.g. zero or negative box size). | `Engine.Core` geometry command handlers on parameter validation. |
| `E-GEOM-BODY-NOT-FOUND` | Error | A geometry query referenced a `bodyId` that is not present in `Document.Bodies`. | `Engine.Core` geometry query handlers (e.g. `GetBoundingBox`). |
| `E-GEOM-NATIVE-OP` | Error | A geometry backend operation failed or returned a degenerate result (e.g. a native Manifold FFI failure). | `Engine.Core` geometry command handlers, wrapping the backend call (P7b). |
| `E-GEOM-BACKEND-INIT` | Error | The native geometry backend failed to initialise (native lib not found / load / version mismatch). Reserved: the host's native-availability fallback (ADR-0014 §4) selects the managed stub instead of surfacing this, so it is not raised in V1.x. | `Engine.Geometry.Manifold` backend init (reserved, not raised). |

## Reserved namespaces

- `X-…` — plugin-defined codes (reserved, not used).

## Adding a code

1. Add the row above. Pick a stable, namespaced code.
2. Reference the code from `Engine.Core/DiagnosticCodes.cs` constants.
3. Same change as the code that raises it. `Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs` reads each `Engine.*` project and fails when a code in the source has no row here.

Removing a code is forbidden. A code that no source raises stays in the table above and gets a row in the table below, with its reason.

## Permanently reserved codes

A reserved code is a code that the registry holds and that no source raises. Register entry R-0011
recorded the risk: an unlimited quantity of reserved codes makes the registry unreliable, because a
reader cannot tell a live code from a placeholder.

`Engine.Tests/Governance/DiagnosticsReserveGateTests.cs` reads this table. The quantity of reserved
codes must not grow past two. A new reserved code needs a decision that lowers or raises that number.
When a task raises a reserved code, the task removes its row from this table and corrects the column
"Where raised" above.

| Code | The reason that no source raises it |
|---|---|
| `E-CMD-BUS-BUSY` | No inbound queue exists. ADR-0006 gives a serial bus with no queue, therefore nothing can be full. The code stays because it is shipped and codes are stable. |
| `E-GEOM-BACKEND-INIT` | ADR-0014 section 4 makes the host select the managed backend when the native library does not load. A failure to initialise therefore becomes a fallback and never a diagnostic. |

TASK-0025 decided this on 2026-09-22 and closed R-0011. Each code above is reserved permanently, and
not until a later phase. A later phase that raises one of them must state that it does so.
