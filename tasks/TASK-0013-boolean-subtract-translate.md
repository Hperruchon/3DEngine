# TASK-0013 — First real Manifold operations: Translate + Subtract (P7c)

## Status

Done — shipped in v0.15 (P7c). See `docs/CURRENT-STATE.md` v0.15.

## Context

v0.11 (P7a) shipped the geometry posture (capability surface, `Document.Bodies`,
deterministic handles, `body.created`) behind a managed stub. v0.14 (P7b) swapped in a
real native Manifold backend behind that same surface. But the only command is still
`CreateBox`, which builds an origin-centered box — something the managed stub imitates by
storing three numbers. This phase adds the first operations that only a real kernel can do.

`Subtract` (box A minus box B) is the headline. `Translate` is included because it is
required to *observe* the subtraction: `CreateBox` only produces origin-centered boxes, and
for two centered boxes a non-empty `A − B` always has the same bounding box as A (the cut is
interior or a through-slot; the outer extent never shrinks). Moving B off-center first makes
the difference trim a face, so `GetBoundingBox` becomes a real witness — no new query needed.

Gated by **ADR-0012 Amendment 1** (accepted 2026-07-05). ADR-0014 (native-interop posture)
and ADR-0001 (kernel posture) remain in force; the `manifoldc` binding authored in P7b is
extended, not redesigned.

## Goal

Ship `Translate` and `Subtract` end to end: new capability interfaces `ITransformOps` /
`IBooleanOps` (Manifold-only), the two commands + handlers, the native ops wired via
`manifold_translate` / `manifold_difference`, host wiring in CLI + HTTP, and tests. After
this, `POST /commands` can create two boxes, move one, subtract, and `POST /queries`
`GetBoundingBox` shows the trimmed solid.

## Scope (in)

1. **Contracts (ADR-0012 Amendment 1 §A1.1).** `ITransformOps`, `IBooleanOps` in
   `Engine.Contracts/Geometry/`; `BackendCapabilities.Transform`, `.Booleans`. No other
   Contracts change.
2. **Commands + handlers (Amendment §A1.3, §A1.5).** `TranslateCommand`/`Handler`,
   `SubtractCommand`/`Handler` in `Engine.Core/Commands/`, mirroring `CreateBox`. Check
   order: operand-in-`Document.Bodies` → capability → backend op. Result body `Kind = "Solid"`,
   handle `= CommandId`, operands left intact, reuses `body.created`.
3. **Manifold backend (Amendment §A1.2).** Add `manifold_translate` / `manifold_difference`
   / `manifold_is_empty` P/Invoke; implement `ITransformOps` + `IBooleanOps`; add the two
   capability flags. Reject an empty difference (`E-GEOM-NATIVE-OP`). `InProcessMeshBackend`
   is **not** modified.
4. **Host wiring.** CLI `apply` switch + handler registration + `Usage.cs`; HTTP
   `CommandsEndpoint` builders + switch; `EngineHost` registration.
5. **Tests (Amendment §A1.7).** Native-gated backend tests (translate shifts AABB; subtract
   trims AABB; empty difference throws; unknown operand throws); bus-level handler tests
   (cap-missing on stub, body-not-found); CLI parse/reject; HTTP native-gated end-to-end +
   unknown-body rejection; native replay round-trip; stub-honesty tests; extend the
   `SchemaEndpointGateTests` hard-coded command-name list.
6. **Docs.** ADR-0012 Amendment 1; this TASK; `CURRENT-STATE.md` v0.15; roadmap P7c Shipped.

## Scope (out)

- **Union / intersection**, and boolean on non-box operands beyond what falls out for free.
  Future ops, each additive.
- **Rotate / scale / general transforms**; only axis translation lands here.
- **In-place body mutation / body deletion.** Ops are non-destructive and additive; a
  `body.deleted`/`body.modified` event is out of scope (would be a new event kind).
- **A `GetVolume` or other new query.** The trimmed AABB (via Translate) is the witness.
- **New diagnostic codes.** Existing `GEOM` codes cover every path (Amendment §A1.4).
- **Stub support for the new ops.** Deliberately native-only (Amendment §A1.2).
- **Changes to the canonical replay-determinism gate.** Stays stub-backed (ADR-0014).

## Inputs

- ADR-0012 Amendment 1 — the capability additions this implements.
- ADR-0012 §§2–7 — reused wiring (handler access, handles, `Document.Bodies`, `body.created`).
- ADR-0014 — native-interop posture; the `manifoldc` binding extended here.
- TASK-0011 / TASK-0012 — the CreateBox slice and Manifold backend this builds on.

## Outputs

- `engine apply Translate --param bodyId=<guid> --param dx=5 --param dy=0 --param dz=0` and
  `engine apply Subtract --param minuendBodyId=<guid> --param subtrahendBodyId=<guid>` parse
  and dispatch via the CLI (structured rejection on the one-shot engine).
- Over HTTP: CreateBox A, CreateBox B, Translate B, Subtract → GetBoundingBox on the result
  returns the trimmed AABB (parity across surfaces per ADR-0011).
- `/schema/commands` lists `Translate` and `Subtract` (auto-projected from the handlers).
- `Engine.Contracts` change is confined to the two interfaces + two flags, gated by the ADR
  amendment (contract-gate green).
- Canonical replay gate unchanged; a native replay round-trip over translate+subtract passes.
- `dotnet build` + `dotnet test` green on a runner with the pinned `manifoldc` artifact.

## Files

**Created:** `Engine.Contracts/Geometry/ITransformOps.cs`, `IBooleanOps.cs`;
`Engine.Core/Commands/TranslateCommand.cs`, `TranslateCommandHandler.cs`,
`SubtractCommand.cs`, `SubtractCommandHandler.cs`;
`Engine.Tests/Commands/BooleanTransformHandlerTests.cs`,
`Engine.Tests/Cli/CliBooleanTransformScenarioTests.cs`,
`Engine.Tests/Http/HttpBooleanTransformScenarioTests.cs`; this TASK file.

**Modified:** `Engine.Contracts/Geometry/BackendCapabilities.cs`;
`Engine.Geometry.Manifold/Native/ManifoldNative.cs`, `ManifoldGeometryBackend.cs`;
`Engine.Cli/Cli.cs`, `Usage.cs`; `Engine.Api.Http/Endpoints/CommandsEndpoint.cs`,
`EngineHost.cs`; `Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs`,
`InProcessMeshBackendTests.cs`, `Engine.Tests/ReplayDeterminism/ManifoldReplayRoundTripTests.cs`,
`Engine.Tests/Http/SchemaEndpointGateTests.cs`; `docs/adr/0012-geometry-backend-wiring.md`,
`docs/CURRENT-STATE.md`, `docs/roadmap.md`.

**Do not touch:** `Engine.Core/Geometry/InProcessMeshBackend.cs`; the canonical
replay-determinism gate; `docs/diagnostics.md` (no new codes); the do-not-touch projects.

## Acceptance criteria

1. `dotnet build` + `dotnet test` green (native tests run on the dev machine, skip on CI).
2. `ITransformOps`/`IBooleanOps` implemented only by `ManifoldGeometryBackend`; the stub
   returns null from `TryGet<T>()` for both.
3. Translate/Subtract produce a new `"Solid"` body with handle `== CommandId`; operands stay.
4. Subtract that trims a face shrinks the result's AABB; a fully-consumed difference rejects
   with `E-GEOM-NATIVE-OP`; unknown operands reject with `E-GEOM-BODY-NOT-FOUND`; the stub
   rejects with `E-GEOM-CAP-MISSING`.
5. `Engine.Contracts` change is only the two interfaces + two flags (contract-gate green);
   no new diagnostic code; no RID in any csproj.
6. Canonical replay-determinism gate unchanged; native replay round-trip passes.
7. `CURRENT-STATE.md` v0.15 entry exists; roadmap shows P7c Shipped.
