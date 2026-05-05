# ADR 0001 — Geometry Kernel Abstraction by Capabilities

## Status

Accepted — 2026-04-28

## Context

The platform must support multiple geometry backends over its lifetime:

- A mesh backend (Manifold) in V1.
- A B-Rep backend (OpenCascade) in a later phase.
- Targeted custom implementations where justified.

A single fat `IGeometryKernel` interface across mesh and B-Rep paradigms leaks. The two paradigms differ in primitive identity, persistence of features (face/edge IDs), exactness guarantees, and operation cost. If the abstraction reduces both to the lowest common denominator (a mesh), B-Rep loses the very properties that make it worth integrating.

The Document is the source of truth (an ordered, replayable command log). Geometry data is a derivable cache. The kernel must therefore not become a competing source of state.

## Decision

The kernel layer is structured as **capability-typed backends**, not paradigm-typed interfaces.

1. **`IGeometryBackend`** is the only top-level kernel type clients see internally. It exposes a capability set and a typed accessor:

   - `Capabilities` — flags including `Mesh`, `BRep`, `ExactBooleans`, `FaceIds`, `Offset`, `Fillet`, `Query`.
   - `TryGet<TCapability>()` — returns the capability interface or `null`.

2. **Capability interfaces** are small, single-responsibility, and independent:

   - `IMeshOps` — primitive creation, transform, mesh boolean, validate, tessellate-export. **V1.**
   - `IGeometryQuery` — bbox, volume, area, mass properties, raycast. **V1.**
   - `IBRepOps` — primitive creation, transform, boolean with face map, fillet, chamfer. **Reserved, not implemented in V1.**
   - `IFeatureIdMap` — stable face/edge IDs across operations. **Reserved, not implemented in V1.**

3. **`BodyHandle`** is opaque (`record BodyHandle(Guid Id)`). The Document holds handles and commands; **the backend owns the geometry data** behind those handles. Switching backends is performed by replaying the command log against the new backend.

4. **Commands depend on capabilities, not backends.** A command handler requests `IMeshOps` (or `IBRepOps + IFeatureIdMap`) from the active backend. If the capability is missing, the command fails fast with a structured error. No silent fallbacks.

5. **The kernel API is internal** to command handlers. Clients (UI, CLI, API, agents) only see commands. Refactoring the kernel layer must not require client changes.

## Consequences

- Adding OCCT later is additive: implement `IBRepOps`/`IFeatureIdMap` on a new `OcctBackend`, register new B-Rep commands. No existing command, contract, or stored project changes.
- Mesh and B-Rep cannot be transparently mixed in the same operation. Cross-paradigm exchange happens only via explicit tessellation (B-Rep → mesh). The reverse is not promised.
- Per-project backend selection is **not** the model; capability negotiation is per command.
- Backends are caches. Losing the cache is recoverable by replay. This is a feature, not a limitation.

## Non-goals

- Universal `Body` type unifying mesh and B-Rep. Rejected.
- Automatic mesh → B-Rep reverse conversion. Rejected.
- Geometry POCOs (`Mesh`, `Solid`) in `Engine.Contracts` exposed to clients. Rejected — clients see commands and tessellated previews only.
- A custom geometry kernel in V1.

## Validation rules

A change to the kernel layer must pass all of:

1. No client-facing contract (command schema, project file format, event stream) is modified.
2. Existing replay fixtures replay identically.
3. New commands declare the capabilities they require; CI fails the PR if a command bypasses the capability check.
4. `Engine.Contracts` contains no concrete geometry data types — only handles, capability interfaces, and command/event records.
5. `IBRepOps` and `IFeatureIdMap` remain reserved (declared, unimplemented) until a backend implements them; their shape may evolve until then.

## Open challenges

- The exact shape of `IFeatureIdMap` cannot be finalized without an OCCT prototype. The interface is reserved, not frozen — expect one breaking revision when the first B-Rep command lands.
- Tessellation parameters (chord tolerance, angular deviation) will need a home. Provisionally on `IBRepOps.Tessellate`, not on `BodyHandle`.
