# ADR 0003 — Blazor as Thin Viewer

## Status

Accepted — 2026-04-28

## Context

Blazor is in scope for V1 as a secondary client. Its roles are:

- Cross-platform access without installing the desktop app.
- Debugging and observability of a running engine.
- Remote inspection.
- Command/event visualization.
- A surface for AI interaction.

The desktop Vulkan application remains the primary editor in V1. Blazor must add value without becoming a second source of truth or a parallel UI track that doubles work.

## Decision

**Blazor is a thin viewer over the engine's HTTP/WS API. It is Blazor WebAssembly only. Its V1 scope is two pages.**

1. **Blazor WASM only.** Blazor Server is rejected for V1: it couples Blazor's deployment to the engine's process and conflates client and engine state. The existing `BlazorApp` (Server-rendered) shell is paused; `BlazorApp.Client` (WASM) is the V1 client.

2. **Transport.** Blazor connects to the engine via:
   - `HTTP` for state reads, schema discovery (`/schema`), and command submission.
   - `WebSocket` for the event stream, with sequence numbers and replay-from-cursor.

3. **V1 scope — two pages.**
   - **Inspector page.** Connect to engine URL, display project header, live event log (virtualized), recent commands, current document summary.
   - **Command submitter page.** JSON editor backed by `/schema`, send button, response panel, error display.

4. **Permitted Blazor responsibilities.**
   - Inspect engine state via API reads.
   - Display events from the WebSocket stream.
   - Submit commands through the HTTP API.
   - Visualize debug data (validation reports, replay status, command history).
   - Render tessellated previews *if* the engine pushes them. The engine performs tessellation; Blazor draws.

5. **Forbidden Blazor responsibilities.**
   - Owning Document state.
   - Applying business logic.
   - Rebuilding geometry, replaying commands client-side, or computing scene state from events.
   - Acting as a second source of truth for any shared concept.
   - Direct kernel access.

6. **Schema-driven.** All command and event DTOs are generated from the engine's `/schema` endpoint. Blazor does not hand-maintain DTOs.

7. **Auth & binding.** The engine HTTP API binds to localhost by default. Any non-loopback bind requires a token. This is a security gate, not a polish item — it is a V1 acceptance criterion.

## Consequences

- `Engine.Api.Http` is V1 (driven by this ADR and ADR 0002).
- Blazor work in V1 is small and bounded; it does not compete with desktop UI for engineering time.
- AI agents reach the engine through the same HTTP/WS surface Blazor uses. Blazor is partly an existence proof for that surface.
- The dual-render-mode setup currently in `BlazorApp` is deferred. No work invested there in V1.

## Non-goals

- Blazor as the primary editor in V1.
- Authoritative 3D viewport in Blazor. Previews are tessellated snapshots from the engine, not interactively edited.
- Live multi-user editing.
- Blazor Server interactivity in V1.
- Per-command parametric forms beyond the generic JSON editor.

## Validation rules

1. The Blazor project does not reference `Engine.Core` or `Engine.Kernel.*`. It depends only on `Engine.Contracts` (DTOs/schemas) and an HTTP/WS client.
2. No Blazor code path mutates a model representing Document state outside of receiving an event from the stream.
3. CI: a "Blazor build without engine" job confirms Blazor compiles and runs against a mocked HTTP/WS surface.
4. The two V1 pages are the only routes shipped. New pages require an ADR amendment.

## Challenge — can Blazor become the main UI later?

Asked explicitly. Honest answer: **technically yes, with two unresolved obstacles, and only if the API stays complete.**

**Obstacles, in order of severity:**

1. **Authoritative 3D viewport in the browser.** A real editor needs sub-100 ms input-to-pixel feedback for picking, gizmo drag, and camera manipulation. Options:
   - Tessellated preview pushed from engine + WebGL/Three.js for camera-only interactivity. Works for inspection, struggles for editing.
   - Pixel streaming from a remote engine. Bandwidth and latency tax; viable for cloud deployments only.
   - WebGPU rendering of a client-side scene cache. Reintroduces a second source of truth — directly violates this ADR.
   None of these give parity with the desktop Vulkan path on a local machine.

2. **Input model.** Browser input (touch, pointer events, keyboard focus management) differs from desktop. A "main UI" must specify gesture semantics anew.

**Conditions under which it could become the main UI:**

- The HTTP/WS API remains the only path to the engine, with no desktop-only command class accumulating.
- A tessellated-preview protocol is defined (engine pushes mesh deltas + transforms; client renders, never authors).
- Selection, picking, and gizmo state are modeled as queries/commands, not as client-side computation.
- Latency budget is met for the target deployment (local engine: easy; remote engine: harder).

**Conditions under which it cannot:**

- The browser client begins to compute scene state independently.
- Tessellation is moved client-side for "performance."
- A WebGL renderer accumulates its own materials, lighting, or geometry pipeline divergent from the engine's.

The decision to revisit this is post-V2 and gated on an explicit ADR. Until then, Blazor stays a viewer.

## Open challenges

- Event volume bounding. If the engine emits >1 kHz events, Blazor will struggle. Event stream design must support server-side batching/sampling.
- Reconnect protocol details (cursor format, retention window of past events on the engine) are not yet specified — V1 task.
- Tessellated-preview protocol does not exist in V1. Inspector page renders a placeholder (scene tree, bbox, counts) until it does.
