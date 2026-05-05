# ADR 0004 — Engine Runtime is the Authoritative Application Controller

## Status

Accepted — 2026-04-28

## Context

The platform has multiple clients (desktop UI, Blazor, CLI, HTTP API, AI agents). Without a clear authority, each client risks accumulating its own state, its own validation, and its own interpretation of what a "scene" is. Within months this produces divergence, ghost bugs, and irreproducible behavior.

The Document is already defined as the source of truth for *data* (commands + materialized state). This ADR fixes the *runtime* authority — the process and the components that own behavior.

## Decision

**The Engine Runtime is the single authoritative application controller. All clients are thin.**

1. **The Engine Runtime owns:**
   - The `CommandBus` and command dispatch.
   - The `Document` (command log + materialized scene + metadata).
   - The active `IGeometryBackend` and its handles.
   - Save/load and project file I/O.
   - Replay execution.
   - The event stream (and its sequence numbering, retention, replay-from-cursor).
   - Validation, debug snapshots, and error reports.
   - Schema publication (`/schema`).

2. **Clients are clients.** Desktop, Blazor, CLI, HTTP API consumers, and AI agents:
   - Read state through the API.
   - Submit commands through the bus (in-process or HTTP).
   - Subscribe to events.
   - Render or display results.

   They do not own business logic.

3. **Same command system for all clients.** A command issued from the desktop is indistinguishable to the engine from one issued by an AI agent over HTTP. There is no privileged client.

4. **Deployment shapes.** The Engine Runtime can run:
   - In-process with the desktop app (V1 default).
   - As a standalone process exposing HTTP/WS (used by Blazor and remote agents).
   - As a library inside test harnesses.

   The authority rule holds in all shapes.

5. **What is *not* business logic and may live on clients (the boundary):**
   - Camera state, viewport size, cursor position.
   - Selection sets (a projection over Document IDs; the selection itself is client UI state).
   - Hover highlights, drag previews, mid-gesture transforms before commit.
   - Layout, theme, panel arrangement.
   - Schema-driven form state in Blazor.

   These are ephemeral, per-client, and never authoritative. If a piece of state must survive a client restart or be visible to other clients, it belongs in the Document and goes through a command.

## Consequences

- The desktop app shrinks to: window, viewport rendering, input → command translation, ephemeral interaction state.
- Blazor shrinks per ADR 0003.
- The CLI is structurally equal to any other client; there is no "CLI mode" of the engine.
- AI-agent integration is free: agents are HTTP/WS clients of the same surface.
- Replay and debug data are first-class engine outputs, not client features.
- The engine can be started without any UI and remain fully functional — a property regularly exercised by CI.

## Non-goals

- Removing all state from clients. Ephemeral UI state is legitimate (see boundary above).
- Forcing every client through HTTP. In-process clients (desktop, tests) may bind directly to the same `CommandBus` instance — they just may not bypass it.
- Designing for multi-engine federation in V1. One Runtime per project.

## Validation rules

1. `Engine.Core` references no UI assembly. CI checks the dependency graph.
2. No client assembly references the geometry backend directly. Clients depend on `Engine.Contracts` (and the HTTP client where applicable). CI enforces this.
3. Every state mutation observable to other clients is traceable to a command in the log. A reviewer's smell test: "Could two clients see different versions of this?" If yes, it must go through a command.
4. The CI "headless smoke" job (ADR 0002) is also the test that the Engine Runtime stands alone.

## Open challenges

- The cancellation and back-pressure model for long-running commands needs definition (shared open item with ADR 0002).
- Event retention policy (in-memory ring? on-disk journal?) is undefined; V1 may use in-memory + on-disk command log replay as the recovery path.
- For the standalone-process deployment, lifecycle (startup, shutdown, crash recovery) is V1.5 work; V1 ships in-process.
