# ADR 0007 — UI Ephemeral State Boundary

## Status

Accepted — 2026-04-28

## Context

ADR 0004 establishes the Engine Runtime as the sole authority and forbids clients from owning business logic. ADR 0002 enforces this with a CI gate. Read literally, those rules forbid every UI from holding *any* state, which is wrong: a viewport must remember camera position, selections, and mid-drag previews, none of which belong in the Document.

Without a written boundary, two failure modes are likely:

- **Boundary too strict.** Every camera tilt becomes a command; the Document log fills with noise; the engine becomes a chokepoint for trivial UI updates.
- **Boundary too loose.** Clients accumulate scene state, geometry caches, "smart" features, and quietly become a second source of truth.

This ADR draws the line.

## Decision

Client-owned ephemeral state is permitted *only* if it satisfies all of:

1. **Ephemeral** — losing it on client restart is acceptable (or it is restored from a non-Document source the client owns, e.g., user settings).
2. **Local** — no other client needs to see it. If two clients open the same project, divergent values are fine.
3. **Non-derivable input** — it is *not* a function of Document state that some other client could compute differently.
4. **Never substitutes for a command** — it never causes a state change visible to other clients except by submitting a command through the bus.

State that fails any of those is **not** UI state and must be a Document concept reached via a command.

### Permitted ephemeral state (illustrative, not exhaustive)

| State | Owner | Notes |
|---|---|---|
| Camera position, orientation, FOV | Client | Saved views are different — see below. |
| Viewport size, split layout, panel arrangement | Client | UX preference. |
| Selection set (entity IDs) | Client | A list of Document IDs; the IDs come from the Document, the *selection* is per-client. |
| Hover highlight | Client | Pointer-driven. |
| Drag/gizmo preview transform | Client | A purely visual transform applied client-side until release. |
| In-progress sketch points before commit | Client | Becomes a command when committed. |
| Tool mode, snap settings, grid visibility | Client | UX preference. |
| Error toasts, transient notifications | Client | Derived from events; not stored authoritatively. |
| Theme, language, font scale | Client | User settings. |
| Connection state, last cursor seq | Client | Transport detail. |

### State that must go through a command (illustrative)

| State | Why it's not ephemeral |
|---|---|
| Object position after release of a drag | Visible to other clients; must be replayable. |
| Saved/named views ("Front", "Iso") | Document-level concept; survives restart; shared. |
| Project-level units, tolerances, settings | Affect computed geometry; replay-relevant. |
| Layer visibility *if* it affects exports or other clients | If purely a viewport filter, ephemeral; if it changes what gets saved, command. |
| Imported asset references | Affects the Document's resolution. |
| Materials, named colors, parametric values | Document content. |

### Drag/gizmo protocol (the canonical example)

1. User starts dragging an entity at `t0`.
2. Client computes a *preview* transform locally on each pointer move and re-renders the affected entity with that transform applied **on top of** the Document's authoritative transform. The Document is unchanged. No commands are issued.
3. On release, the client submits **one** `Translate` (or `Transform`) command with the final delta. The bus applies it; the engine emits `command.applied`; all clients (including this one) update from the event.
4. Until the event arrives, the client may keep showing the preview transform. On the event, it discards the preview and renders authoritative state.

This collapses N preview frames into 1 command. It is the model every interactive operation should follow.

### Multi-client coordination in V1

- Two clients viewing the same Document have **independent** cameras, selections, and previews. There is no shared "presence."
- A drag preview in one client is invisible to the other; only the eventual command is.
- Live cursors, shared selection, presence indicators — all post-V2.

### Settings vs ephemeral

User-settings persistence (theme, last-opened project, recent files, panel layout) is the **client's** responsibility, stored in the client's local config. It is not in the Document. Different clients on the same machine may have different settings.

## Consequences

- The desktop UI legitimately holds non-trivial state (selection, gizmo state, snap config) without violating ADR 0004.
- The CI rule "no business logic in clients" is enforced via the dependency graph (ADR 0002), not by counting state.
- Reviewers have a clear test for any new client-side field: run it through the four conditions above. If it fails any, push it into a command.
- "Saved views" must be modeled as Document concepts (commands like `CreateNamedView`, `UpdateNamedView`), not as client settings. This is a small but important call-out for V1.5.

## Non-goals

- Defining the exhaustive list of ephemeral state. The four conditions are the test; the tables are illustrative.
- Solving multi-client presence/collab in V1.
- Persisting ephemeral state across client restarts (clients may, at their discretion, save camera/layout/settings locally; the engine takes no position).

## Validation rules

1. Code review checklist item: "For each new client-side field, does it satisfy all four conditions? If not, where is the corresponding command?"
2. CI dependency check (already in ADR 0004): client assemblies must not reference `Engine.Core` or `Engine.Kernel.*`. This catches the worst violations structurally.
3. A client may **read** Document state via the API, but may **not** maintain a parallel mutable copy that drifts from event-driven updates. Local caches must be invalidated on `command.applied` for affected entities.
4. No client emits side effects to the Document except by `Apply(Command)`. Reviewers should reject "client computes X, then sends a command with X" patterns where X could be computed by the engine — push the computation into the command handler.

## Open challenges

- **Selection that is a query result.** "Select all bolts touching face F" is a query against the Document; the *result* is ephemeral selection state, but the query itself must run somewhere. V1: the engine exposes selection queries as read APIs; clients call them and store the result. Future: the queries themselves may need command form for replayability of test scenarios.
- **Named views as Document content.** The first feature that crosses the boundary "feels like UI but is shared" — V1 ships without saved views. When added, they are a clean test of the boundary.
- **Settings sync across machines.** Out of scope. If ever in scope, settings become a separate document, not part of the project Document.
