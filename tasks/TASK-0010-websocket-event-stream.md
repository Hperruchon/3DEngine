# TASK-0010 — WebSocket event stream + reconnect (P6.3)

## Status

Ready

## Context

P6.3 is the last sub-phase of P6 (HTTP/WebSocket transport). ADR-0005 fixes the event-stream contract: `Seq` identity, total order per Document, in-memory ring retention, replay-from-cursor with Resume / Reconstruct / Unknown branches, the `subscribe` → `subscription.resume|reset` handshake, per-subscriber bounded outbound queue with `subscriber.lagged` disconnect, and 30 s idle heartbeat. ADR-0010 (this set) fixes the `subscription.reset` snapshot shape.

ADR-0005's three CI validation rules are explicit:

- "subscriber lag" test — slow consumer is disconnected; fast consumers continue without gaps.
- "reconnect resume" test — client with a recent cursor receives buffered events without a Reset.
- "reconnect reset" test — client with a stale cursor receives a Reset and a snapshot.

This TASK ships the WebSocket endpoint that implements that contract.

`Engine.Core/InMemoryEventSink` is the in-memory ring buffer today; it has no notification surface. To broadcast events to connected subscribers without changing `Engine.Core`, a decorating sink lives in `Engine.Api.Http` and wraps the `InMemoryEventSink` — the bus still writes to one `IEventSink`, but the decorator forwards each `Append` to a broadcaster that fans out to subscribers.

## Goal

Add `GET /events` (WebSocket upgrade) to `Engine.Api.Http`. Behaviour:

1. Client connects via WebSocket and sends a single JSON `subscribe` message.
2. Engine responds with either `subscription.resume { fromSeq }` (cursor within ring) or `subscription.reset { documentId, snapshot }` (cursor missing, snapshot per ADR-0010).
3. Engine streams Document events to the connection as they're emitted by `CommandBus`, in `Seq` order, with no gaps for that connection.
4. If the connection's outbound queue overflows (V1 cap: 1024), the engine closes the WebSocket with reason `subscriber.lagged` and stops sending.
5. If 30 s passes with no event sent, the engine sends `heartbeat`.

Three CI validation tests land alongside the implementation.

## Scope (in)

1. **Project plumbing**
   - Add `app.UseWebSockets()` in `Engine.Api.Http/Program.cs`.
   - Map `app.Map("/events", EventsEndpoint.Handle)`. Non-WebSocket requests to `/events` return `400` with the `E-API-BAD-REQUEST` envelope.

2. **Broadcaster + decorating sink**
   - `Engine.Api.Http/WebSockets/EventBroadcaster.cs` — singleton service. Holds a list of active `Subscriber`s; `OnEvent(EventRecord)` pushes to each subscriber's bounded queue. Methods `Attach(Subscriber)` / `Detach(Subscriber)`.
   - `Engine.Api.Http/WebSockets/BroadcastingEventSink.cs` — `IEventSink` decorator. On `Append`, delegates to the inner `InMemoryEventSink` first (preserves ring semantics, current `Snapshot()` contract), then calls `EventBroadcaster.OnEvent(record)`.
   - `EngineHost` constructs both: `InMemoryEventSink` stays as the storage; `BroadcastingEventSink` wraps it; the wrapper is what `CommandBus` writes to. `Engine.Core` is untouched.

3. **Subscriber + connection lifecycle**
   - `Engine.Api.Http/WebSockets/Subscriber.cs`:
     - Owns the `WebSocket` plus a bounded `Channel<WireMessage>` (capacity 1024 per ADR-0005 §6, configurable for tests).
     - Reader task pumps the channel to `WebSocket.SendAsync` as JSON text frames.
     - A `Timer` (or `PeriodicTimer`) sends `heartbeat` after 30 s of no other send. Reset on every send.
     - `Enqueue(WireMessage)`: try-write to the channel. If full, mark for lag-disconnect (don't block, don't drop, don't coalesce — per ADR-0005 §6).
     - Lag detection closes the WebSocket with status `1008` (Policy Violation) + reason `subscriber.lagged`. The diagnostic code `W-API-WS-LAGGED` is also surfaced (registry entry).
     - On disconnect (lag, normal close, network error): detach from broadcaster, dispose the channel and timer.

4. **Subscription handshake (`/events`)**
   - `Engine.Api.Http/WebSockets/EventsEndpoint.cs`:
     - Accept WebSocket if `HttpContext.WebSockets.IsWebSocketRequest`; else 400 with `E-API-BAD-REQUEST`.
     - Read one client message: `{ "documentId"?: Guid, "lastSeenSeq"?: long }`. Malformed → close with `1003` + reason "E-API-WS-INVALID-SUBSCRIBE", do not attach.
     - Decide Resume vs Reset using `EngineHost.Events.Snapshot()` and `EngineHost.Document.Version`:
       - **Resume.** `documentId` matches AND `lastSeenSeq` is within the ring (i.e., the ring contains an event with `Seq == lastSeenSeq + 1` or `lastSeenSeq == _document.Version`). Send `subscription.resume { fromSeq }`, replay events from the ring strictly greater than `lastSeenSeq`, then attach to the broadcaster for live events.
       - **Reset.** `documentId` is null, or doesn't match, or `lastSeenSeq` is older than the ring's earliest. Send `subscription.reset { documentId, snapshot }` where `snapshot` follows ADR-0010 §2. Attach for live events from `current+1`.
   - Race-safety: the handshake captures a snapshot + the ring, enqueues replay messages, **then** attaches to the broadcaster — under a brief lock on the broadcaster — so live events emitted between snapshot and attach are not lost.

5. **Wire format**
   - Every wire message is a JSON object with `kind` as the discriminator. Three families:
     - **Document events** — full `EventRecord` shape: `{ kind, seq, timestamp, documentId, causeCommandId, payload }`.
     - **Protocol messages** — minimal envelopes:
       - `{ kind: "subscription.resume", fromSeq: long }`
       - `{ kind: "subscription.reset", documentId: Guid, snapshot: {...per ADR-0010} }`
       - `{ kind: "heartbeat" }`
   - Field naming, enum-as-string, etc. — reuses `ApiJson.Options` from TASK-0007.

6. **New diagnostic codes**
   - `W-API-WS-LAGGED` — warning, surfaced when a subscriber is disconnected for lagging. Subsystem: `API`.
   - `E-API-WS-INVALID-SUBSCRIBE` — error, surfaced when the client's `subscribe` message is malformed or missing. Subsystem: `API`.
   - Both land in `Engine.Core/DiagnosticCodes.cs`, `docs/diagnostics.md`, and `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` mirror.

7. **Tests in `Engine.Tests/Http/`**
   - `EventsEndpointResumeTests.Subscribe_With_Cursor_Inside_Ring_Receives_Resume_Then_Buffered_Events` — corresponds to ADR-0005 validation rule §3 "reconnect resume."
   - `EventsEndpointResetTests.Subscribe_With_Stale_Cursor_Receives_Reset_With_Snapshot` — ADR-0005 rule §4 "reconnect reset."
   - `EventsEndpointResetTests.Subscribe_With_Mismatched_DocumentId_Receives_Reset_With_Snapshot` — Unknown branch (ADR-0005 §4 case 3).
   - `EventsEndpointResetTests.Subscribe_With_Null_DocumentId_Receives_Reset_With_Snapshot` — first-time connect.
   - `EventsEndpointLagTests.Slow_Subscriber_Is_Disconnected_With_Subscriber_Lagged_While_Fast_Subscriber_Receives_All_Events` — ADR-0005 rule §2 "subscriber lag."
   - `EventsEndpointInvalidSubscribeTests.Malformed_Subscribe_Frame_Closes_With_E_API_WS_INVALID_SUBSCRIBE` — input validation.
   - `EventsEndpointInvalidSubscribeTests.Plain_HTTP_GET_On_Events_Returns_400_With_E_API_BAD_REQUEST` — non-WS requests rejected.
   - `EventsEndpointWireShapeTests.Document_Event_Frame_Matches_EventRecord_Shape` — wire-format guard.
   - `EventsEndpointWireShapeTests.Subscription_Reset_Snapshot_Matches_ADR_0010_Shape` — snapshot shape gate.

   Tests use a real WebSocket client (`System.Net.WebSockets.ClientWebSocket`) over `WebApplicationFactory<Program>`'s `TestServer`. The factory exposes `TestServer.CreateWebSocketClient()` for this.

8. **Documentation**
   - `CLAUDE.md` V1 clamps: drop the "No WebSocket transport" line (now present).
   - `docs/CURRENT-STATE.md` — v0.10 entry.
   - `docs/roadmap.md` — move P6.3 to Shipped V1.x. P6 is now complete.
   - `docs/diagnostics.md` — append the two new codes.

## Scope (out)

- **Per-Document subscription routing.** V1 has one Document per Engine Runtime (ADR-0005). When V2 adds multi-Document, `documentId` filtering on subscriptions becomes a feature.
- **Topic / Kind filtering.** Per ADR-0005 §Non-goals: "V1 streams everything; clients filter locally."
- **Binary frames.** Text JSON. ADR-0005 §1: framing is implementation choice; text aligns with the rest of the HTTP API.
- **TLS / `wss://`.** Local HTTP only in V1; production hosting is its own task.
- **Auth / per-subscriber quotas.** Out of scope.
- **Backpressure tuning** beyond the 1024 default capacity. Configurable through a non-public hook for tests; no public surface.
- **Reconnection from the server side.** The protocol is client-driven; on disconnect the client reconnects.
- **Snapshot at arbitrary historical Seq.** ADR-0010 §Non-goals.
- **Snapshot for Document state larger than V1.x.** Per ADR-0010 §3, the snapshot is metadata-only today; geometry state extends it when P7 lands.
- **Cancellation token propagation from `HttpContext.RequestAborted` to in-flight bus calls.** Out of phase.
- **A `ws://` smoke step in the CI workflow** (`/headless-smoke`). The in-process tests cover the same surfaces with deterministic timing.

## Inputs

- ADR-0005 — event stream contract; the validation rules drive the test list.
- ADR-0006 — `CommandBus` serial execution; what the broadcaster sees is what the bus committed.
- ADR-0010 — snapshot format.
- TASK-0007 — `EngineHost`, `ApiJson`, `ApiErrorEnvelope`; reused here.
- TASK-0008 — idempotency cache (no interaction; events on a duplicate command apply are not re-emitted, which keeps the stream clean).
- TASK-0009 — `/schema/events`, `/schema/diagnostics` (new diagnostics added there too).
- `Engine.Core/InMemoryEventSink.cs` — ring buffer; not modified, just wrapped.
- `Microsoft.AspNetCore.WebSockets` — built into ASP.NET Core; no new NuGet package.

## Outputs

- `GET /events` upgrades to WebSocket. Plain GET returns `400`.
- Subscribers receive `subscription.resume` or `subscription.reset` then live events.
- A slow subscriber is disconnected with `subscriber.lagged` without affecting other subscribers.
- A 30 s idle gap produces a `heartbeat` frame.
- New diagnostic codes `W-API-WS-LAGGED` and `E-API-WS-INVALID-SUBSCRIBE` registered in `docs/diagnostics.md`, mirrored in `DiagnosticCodes.cs` and `/schema/diagnostics`.
- CLAUDE.md V1 clamps list no longer mentions WebSocket transport.
- `docs/CURRENT-STATE.md` v0.10 entry.
- `docs/roadmap.md` shows P6.3 Shipped V1.x; P6 complete.
- `dotnet build` + `dotnet test` green with new tests.

## Files

**Created:**
- `Engine.Api.Http/WebSockets/EventBroadcaster.cs`
- `Engine.Api.Http/WebSockets/BroadcastingEventSink.cs`
- `Engine.Api.Http/WebSockets/Subscriber.cs`
- `Engine.Api.Http/WebSockets/WireMessage.cs` — wire-shape records + serializer helpers.
- `Engine.Api.Http/WebSockets/SnapshotProjector.cs` — `Document` → snapshot DTO per ADR-0010.
- `Engine.Api.Http/Endpoints/EventsEndpoint.cs`
- `Engine.Tests/Http/EventsEndpointResumeTests.cs`
- `Engine.Tests/Http/EventsEndpointResetTests.cs`
- `Engine.Tests/Http/EventsEndpointLagTests.cs`
- `Engine.Tests/Http/EventsEndpointInvalidSubscribeTests.cs`
- `Engine.Tests/Http/EventsEndpointWireShapeTests.cs`
- `Engine.Tests/Http/WebSocketTestClient.cs` — shared helper for opening WS to `TestServer` and reading frames.
- `tasks/TASK-0010-websocket-event-stream.md` (this file)

**Modified:**
- `Engine.Api.Http/Program.cs` — `UseWebSockets`, map `/events`, register broadcaster.
- `Engine.Api.Http/EngineHost.cs` — construct broadcaster + decorating sink; bus writes through the decorator.
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror new codes.
- `Engine.Core/DiagnosticCodes.cs` — two new constants.
- `CLAUDE.md` — drop the "No WebSocket transport" V1 clamp.
- `docs/diagnostics.md` — append rows for the two new codes.
- `docs/CURRENT-STATE.md` — v0.10 entry.
- `docs/roadmap.md` — P6.3 → Shipped V1.x.
- `tasks/TASK-0010-websocket-event-stream.md` — Status flip in close commit.

**Do not touch:**
- `Engine.Contracts/**`. No new contract types.
- `Engine.Core/CommandBus.cs`, `Engine.Core/InMemoryEventSink.cs`, etc. The decorator-in-Api pattern keeps `Engine.Core` untouched.
- `Engine.Cli/**`. No CLI changes.
- ADRs.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*`.

## Tests

- `EventsEndpointResumeTests.Subscribe_With_Cursor_Inside_Ring_Receives_Resume_Then_Buffered_Events`
- `EventsEndpointResetTests.Subscribe_With_Stale_Cursor_Receives_Reset_With_Snapshot`
- `EventsEndpointResetTests.Subscribe_With_Mismatched_DocumentId_Receives_Reset_With_Snapshot`
- `EventsEndpointResetTests.Subscribe_With_Null_DocumentId_Receives_Reset_With_Snapshot`
- `EventsEndpointLagTests.Slow_Subscriber_Is_Disconnected_With_Subscriber_Lagged_While_Fast_Subscriber_Receives_All_Events`
- `EventsEndpointInvalidSubscribeTests.Malformed_Subscribe_Frame_Closes_With_E_API_WS_INVALID_SUBSCRIBE`
- `EventsEndpointInvalidSubscribeTests.Plain_HTTP_GET_On_Events_Returns_400_With_E_API_BAD_REQUEST`
- `EventsEndpointWireShapeTests.Document_Event_Frame_Matches_EventRecord_Shape`
- `EventsEndpointWireShapeTests.Subscription_Reset_Snapshot_Matches_ADR_0010_Shape`

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — existing 61 plus the 9 new tests (70 total).
3. `Engine.Core/**` is unchanged. The decorator + broadcaster live entirely in `Engine.Api.Http`.
4. No `Engine.Contracts/**` change.
5. Two new diagnostic codes `W-API-WS-LAGGED` and `E-API-WS-INVALID-SUBSCRIBE` registered in `docs/diagnostics.md`, mirrored in `DiagnosticCodes.cs` and `/schema/diagnostics`.
6. CLAUDE.md V1 clamps no longer mention WebSocket transport.
7. `docs/CURRENT-STATE.md` v0.10 entry exists.
8. `docs/roadmap.md` lists P6.3 under Shipped V1.x; V1.x Pending is empty.

## Notes for the implementer

- **Race window at attach.** The handshake's risk: live events emitted between "snapshot the ring" and "attach to the broadcaster" can be lost or duplicated. Mitigation: hold a short lock on the broadcaster during the attach so `OnEvent` queues for the new subscriber only after the replay events are in its channel.
- **Heartbeat timer.** `PeriodicTimer` (every 30 s) plus a "last send timestamp" check. On tick, if nothing sent in the last 30 s, send a `heartbeat`. Cancel on disconnect.
- **Bounded channel.** `Channel.CreateBounded<WireMessage>(new BoundedChannelOptions(1024) { FullMode = BoundedChannelFullMode.DropWrite })` would silently drop — which violates ADR-0005 §6. Use `FullMode = Wait` and try-write with `TryWrite`; on failure, mark for lag-disconnect. Do not block.
- **Lag-disconnect path.** Schedule a close on a separate task so the publishing path doesn't await the WebSocket close. The publishing path enqueues to all subscribers in turn; one slow subscriber must not slow the others.
- **`Snapshot()` returns the ring under a lock.** Use it for the replay decision; copy the relevant events out under the same lock so live events don't slip past. Implementation note: `InMemoryEventSink.Snapshot()` already returns an array copy.
- **Wire shape for `EventRecord.Payload`.** The bus emits `Dictionary<string, object?>` payloads. `System.Text.Json` renders this as a JSON object; the existing `ApiJson.Options` configuration handles it.
- **Test client.** `WebApplicationFactory<Program>.Server.CreateWebSocketClient()` returns a client that connects via the in-process pipeline — no real ports. Faster and deterministic.
- **Why diagnostic codes, not just close-frame reason strings.** Close-frame reasons are human-readable transport hints; the diagnostic registry is the authoritative catalog of failure conditions. Both surfaces should agree, and the P2 gate + the gate test from TASK-0009 catch mismatches.
- **No event-registry refactor.** TASK-0009 hand-encoded the event kinds in `/schema/events`. If a new Kind ships here, append it to that list. The list currently includes `subscription.resume`, `subscription.reset`, `heartbeat`; no new Kinds are introduced by this task.
