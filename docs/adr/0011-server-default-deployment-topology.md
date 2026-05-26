# ADR 0011 — Server-default deployment, embed for offline

## Status

Accepted — 2026-05-12

## Context

ADR-0002 fixes the *testability* surface (CLI as canonical client; every feature reachable headless). ADR-0004 fixes *authority* (the engine is the only mutator). ADR-0009 fixes *kernels* (design truth in `Engine.*`, render state in `3DEngine.Core`). None of them fix *deployment topology*: where the engine process lives, how many of them exist, and how clients reach the one they share.

The codebase reflects the ambiguity. As of v0.9:

- `Engine.Cli` is **embedded**: every `engine apply` builds a fresh `Document` + `CommandBus`, runs one command, exits. No state survives between invocations.
- `Engine.Api.Http` is **server**: a singleton engine per host process, multiple HTTP clients possible, WebSocket events in flight (P6.3).
- `3DEngine/` and `BlazorApp/` are placeholders. They reference neither `Engine.Core` nor `Engine.Api.Http` today; their fate is undecided.

The two modes have started to drift: P6's work assumes server (one engine, many clients, WebSocket fan-out); the CLI assumes embedded (one client, no fan-out, no persistence between invocations). Both compile, both pass tests, and the project has not stated which is the default deployment story. That ambiguity makes downstream calls harder: P6.3 is hard to justify without a consumer; persistence is hard to scope without knowing whether restarts are user-visible or operator-visible; auth is hard to scope without knowing whether the engine is on a network.

This ADR fixes the topology so everything downstream has a stake to drive against.

## Decision

### 1. `engine-api-http` is the canonical deployment

The engine runs as its own process — `engine-api-http` — independent of any client. Its lifecycle is operator-managed (locally: a process the user launches; eventually: a hosted service). Clients reach it through:

- `POST /commands`, `POST /queries` (TASK-0007) — request/response.
- `GET /schema/*` (TASK-0009) — discovery.
- `GET /events` over WebSocket (P6.3) — live event stream with cursor-based reconnect.

"Reach it" is identical for every client: UIs, CLIs, AI agents, remote validators. There is no privileged in-process path that bypasses the API surface.

### 2. Embedded mode is a strict subset

Embedded mode is the case where exactly one client hosts `Engine.Core` types in-process instead of going through HTTP. The engine code is unchanged; the host wires it locally. `Engine.Cli` today is the canonical embedded host.

Embedded is valid when **all** of these hold:

- There is exactly one client of the engine at any moment.
- No other process (AI agent, remote UI, validator) needs to observe what the engine does.
- No persistence between client invocations is required (V1 clamp; revisits when persistence lands).

Anything that violates one of these — collaboration, agent-watching, restart-survivable state — requires server mode by definition.

### 3. Engine code is topology-agnostic

`Engine.Contracts` and `Engine.Core` know nothing about HTTP, processes, or hosts. The same `CommandBus` runs in `Engine.Cli` (one client, one CLI invocation) and in `engine-api-http` (many clients, long-lived). The host chooses the topology; the kernel does not.

There is no "mode" flag, no compile-time switch, no feature toggle in `Engine.*` to distinguish embedded from server.

### 4. New rich clients target server-default

`3DEngine/` (Vulkan desktop host) and `BlazorApp.Client/` (browser) target server mode by default when they grow up. They may *additionally* support an embedded path for solo-offline use, but the rich-features story — collaboration, agents, multi-client viewing — is server-mode only. That choice is per-client and out of this ADR's scope.

A future client that connects to a *remote* server is the same client as one connecting to a *localhost* server; the only difference is the URL.

### 5. The CLI stays embedded for now

`Engine.Cli` continues to be the canonical embedded host: ephemeral engine per invocation, no persistence, no event stream. That makes it usable as a scripting and test surface without operator overhead.

A future remote CLI variant — call it `engine-cli-remote` — that talks to a running `engine-api-http` is allowed but not in V1.x scope. When it ships, the existing embedded CLI remains for tests.

### 6. Topology diagram

```
           ┌──────────────────────────────┐
           │       engine-api-http        │  ← canonical deployment
           │  ┌────────────────────────┐  │
           │  │ Engine.Core            │  │  ← same types as embedded
           │  │  CommandBus, Document  │  │
           │  │  IdempotencyCache      │  │
           │  │  InMemoryEventSink     │  │
           │  └────────────────────────┘  │
           │  HTTP + WebSocket surface    │
           └──────▲──────────────────────┬┘
                  │                      │
   ┌──────────────┼──────────┬───────────┘
   │              │          │
┌──┴───────┐ ┌────┴─────┐ ┌──┴─────┐ ┌──────────────────────────────┐
│ 3DEngine │ │ Blazor   │ │ Agent  │ │ Engine.Cli (offline mode)    │
│ (desktop)│ │ (web)    │ │ (AI)   │ │ embeds Engine.Core in-process│
└──────────┘ └──────────┘ └────────┘ │ same code, no HTTP           │
                                     └──────────────────────────────┘

Server: one engine, many clients, lifecycle independent of clients.
Embedded: one client, in-process engine, lifecycle bounded by the client.
```

## Consequences

- **P6.3 (WebSocket) is justified, not premature.** Any rich client (UI, agent) requires it. Without it, the server can take commands but cannot push state changes — the headless-server story is incomplete.
- **Persistence ADR scope clarifies.** "After an `engine-api-http` restart, can clients reconnect to the same Document?" is a real operator question; the persistence ADR answers it. For `Engine.Cli` the question is moot (each invocation is its own session).
- **Auth becomes a real ADR.** Any non-localhost deployment of `engine-api-http` needs authentication and authorization. V1.x is localhost-only; that is a clamp, not a permanent design.
- **3DEngine and BlazorApp.Client target HTTP/WS clients.** When those projects start, they consume `Engine.Api.Http` and `3DEngine.Core` (the render kernel), not `Engine.Core` directly.
- **No duplicate code paths in `Engine.*`.** The kernel doesn't grow a "if-running-as-server" branch. The host wires things; the kernel doesn't care.
- **CLI vs HTTP behavioural parity** is reaffirmed: both surfaces produce the same `CommandResult`/`QueryResult` shapes (ADR-0002, ADR-0008). The wire is JSON in both cases.

## Non-goals

- Specifying the deployment platform (systemd, Docker, Kubernetes, native binary). Out of scope; an operator-doc concern.
- Logging, metrics, tracing infrastructure for the server. Its own concern.
- Federated / multi-server topology. V2 at earliest.
- Per-tenant or per-Document instances of `engine-api-http`. Today one process hosts one Document; multi-document is V2.
- Authentication / authorization design. Separate ADR before any non-localhost shipping.
- Mandating that solo-offline use cases ship with a bundled server. Each client picks how it deploys; some may bundle, some may require a running server.
- Forcing the CLI to talk over HTTP. Stays embedded.

## Validation rules

1. `Engine.Contracts/**` and `Engine.Core/**` contain no reference to HTTP, WebSocket, hosting, or process lifecycle types. CI-checkable via dependency direction (the diagnostic-codes/contracts gates already catch most of this; explicit dependency gate is a future TASK).
2. Every feature in `Engine.Api.Http` exercises only the surface in `Engine.Contracts` / `Engine.Core`. No `internal` reach-through into kernel state.
3. `Engine.Cli` continues to work without any `engine-api-http` process running. Tests that confirm CLI behaviour run in-process via `Cli.Run`.

## Open challenges

- **CLI ↔ HTTP wire parity test.** A test that runs the same logical command through both surfaces and asserts the resulting `CommandResult` is structurally identical would be a strong guard against drift. Not sized here; a follow-up TASK when both surfaces have a richer command set.
- **Engine.Cli embedded vs. remote, eventually.** The current embedded CLI is fine as a test/scripting surface for a developer's own machine. When users want to script against a *running* server, a remote CLI is the answer. Scoping that arrives when the use case does.
- **Authentication.** Mentioned above. Pre-production, before any non-localhost deployment.
- **Persistence pairs with this ADR.** A separate persistence ADR is still open. Its design assumes server-default (the engine process owns the durable state); embedded mode either gets stripped-down persistence or stays ephemeral.
