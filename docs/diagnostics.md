# Diagnostic code registry

All `E-`/`W-`/`I-` codes used in `Engine.*` code MUST appear here. Append-only. Codes are stable: once shipped, a code does not change meaning. Per ADR-0008 §4.

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

- `X-…` — plugin-defined codes (V2, not used yet).

## Adding a code

1. Add the row above. Pick a stable, namespaced code.
2. Reference the code from `Engine.Core/DiagnosticCodes.cs` constants.
3. Same PR as the code that raises it.

Removing a code is forbidden. Mark obsolete in this file if no longer raised.
