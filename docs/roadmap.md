# Roadmap

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This document holds each track and each phase. A phase here is a candidate. When you select a phase,
it becomes a task in `tasks/`. The `status` field in the task file is the authority for the question
"did this phase ship". This document is the menu. It is not the ledger.

When a phase ships, add its line to "Shipped". Give the version and the task identifier. A phase
that is not in "Shipped" is pending.

The scope test in `docs/CHARTER.md` uses this document. The absence of a track is a stop signal.
Therefore each approved objective must have a track here.

## Shipped

- P0 — Engine Runtime spine. v0.1, TASK-0001.
- P1 — Engine.Cli scaffold. v0.2, TASK-0002.
- P2 — Diagnostics registry CI gate. v0.3, TASK-0003.
- P3 — `3DEngine.Core` peer render kernel (ADR-0009). v0.4, TASK-0004.
- P4 — Replay determinism CI gate. v0.5, TASK-0005.
- P5 — Workflow gates. v0.6, TASK-0006.
- P6.1 — Engine.Api.Http scaffold. v0.7, TASK-0007.
- P6.2 — Idempotency cache. v0.8, TASK-0008.
- P6.4 — Schema endpoints. v0.9, TASK-0009.
- P6.3 — WebSocket event stream and reconnect (ADRs 0010 and 0011). v0.10, TASK-0010.
- P7a — First geometry slice, `CreateBox` (ADRs 0012 and 0013). v0.11, TASK-0011.
- P7b — Manifold backend (ADR-0014). v0.14, TASK-0012.
- P7c — Translate and Subtract (ADR-0012 Amendment 1). v0.15, TASK-0013.
- P0.1 — A released SDK pin. v0.16, TASK-0014.
- P0.2 — The charter and each operational rule. v0.17, TASK-0015.
- P0.3 — Handler-declared construction. One handler catalog. v0.18, TASK-0016, ADR-0016.
- P0.4 — Each governance rule becomes mechanical. Four gates. v0.19, TASK-0017.
- P0.5 — The unmerged branch is salvaged. Each identifier names one thing. v0.20, TASK-0018.
- P0.8 — Three governance corrections. One status set, one limit, one false statement. v0.21, TASK-0021.
- P0.7 — The write set of a task becomes mechanical. v0.22, TASK-0022.
- P0.9 — The repository is clean. The gate runs on each push. v0.23, TASK-0023.
- P0.6 — The gate passes on Windows, Linux and macOS. v0.24, TASK-0020.
- P0.10 — A licence notice and a verified checksum for the native payload. v0.25, TASK-0024.
- P0.11 — Each open question is decided. Two entries remain open. v0.26, TASK-0025.
- P0.12 — The write-set gate runs in the pipeline and its forbid rule is correct. v0.27, TASK-0026. Track 0 is complete.
- R1 — The Vulkan code is first-party and uses Vortice.Vulkan 3.2.3. v0.28, TASK-0027, ADR-0017.

## Track 0 — Foundations

This track removes each condition that blocks other work.

| Phase | Content | Evenings |
|---|---|---|
| P0.1 | A released SDK pin | 1–2 |
| P0.2 | The charter and each operational rule | 3–4 |
| P0.3 | Remove both dispatch switch statements. One handler catalog. | 3–5 |
| P0.4 | Make each governance rule mechanical. ADR front matter, the register gate, the marker gate, the dependency direction gate. | 4–6 |
| P0.5 | Salvage the unmerged branch. Renumber. Merge the hosting factory. | 2–3 |
| P0.6 | A gate on Windows, Linux and macOS. | 2–3 |
| P0.7 | The write-set check. Compare each changed file against the write set of the active task. | 1–2 |
| P0.8 | Correct the status vocabulary, the register limit and one false statement. | 1 |
| P0.9 | Clean each branch, worktree and stash. Run the gate on each push. | 1 |
| P0.10 | A licence notice and a verified checksum for the native payload. | 1 |
| P0.11 | Decide each open question that does not need new work. | 1 |
| P0.12 | Correct the forbid rule. Run the write-set gate on each commit. | 1 |
| P0.13 | One serial boundary for commands, queries and snapshots. TASK-0034. | 2–3 |
| P0.14 | The version counts applied commands. ADR-0020, TASK-0035. | 1–2 |
| P0.15 | A host refuses to start without the native backend. TASK-0036. | 1–2 |
| P0.16 | An operation consumes its operands. ADR-0021, TASK-0037. | 2–3 |
| P0.17 | The desktop host owns a session and serves the surface. ADR-0019, TASK-0038. | 3–5 |

## Track P — The platform

| Phase | Content | Evenings |
|---|---|---|
| P8a | Persistence. A command log on disk. ADR-0015. Register entry R-0025 asks if the scope becomes a document file. | 3–5 |
| P8b | The container model. One document holds one part. An assembly holds references at a fixed version. | 5–8 |
| P8c | Undo and redo. Add the inverse command. | 3–5 |
| P8d | The three layers. Log, ordered feature list, regeneration cache. | 5–8 |
| P8e | Units and dimension on each number. | 2–3 |

## Track R — The renderer

The first objective. Each phase ends with something that a person can observe.

| Phase | Content | Evenings |
|---|---|---|
| R1 | Upgrade the Vulkan binding. Move the Vulkan code into a first-party project. | 2–4 |
| R2 | A tessellation capability. A mesh leaves the geometry backend. TASK-0028. | 4–6 |
| R3 | A pipeline. A triangle, then an indexed mesh. | 4–6 |
| R4 | A camera with depth. Flat scene arrays. Each draw through an indirect interface. | 5–7 |
| R5 | Geometry from the engine. Subscribe to events, fetch a mesh, draw it. Subtract a double-precision origin before the narrowing to float. Choose `frontFace` together with the viewport flip. | 6–8 |
| R6 | Observe a cut. The live body set of ADR-0021 gives the result. A headless image test. | 4–6 |
| R7 | Device-tagged pointer samples. The intent layer and the parity test. | 4–7 |
| R8 | Selection, name and visibility as commands. | 4–6 |
| R9 | A transform gizmo at a constant screen size. | 5–8 |
| R10 | A numeric pad and an expression evaluator. Each drag has a numeric equivalent. | 3–5 |
| R11 | Escalated selection and a snap engine. | 4–6 |

## Track K — The kernel

The owned kernel gives identity. Manifold gives geometry. `docs/CHARTER.md` holds the closed feature
boundary and each refused rung.

| Phase | Content | Evenings |
|---|---|---|
| K0 | A feature graph over Manifold. No B-Rep. Deferred-query references. | 12–20 |
| K1 | Exact predicates. A deterministic arithmetic layer. | 4–8 |
| K2 | A 2D sketch model. Five entities and fourteen constraints. A sketch attaches to a datum. | 6–10 |
| K3 | A constraint solver. Write it. | 12–20 |
| K4 | Planar faces only, with a boolean. Provenance inside the kernel. | 35–55 |
| K5 | Add the cylinder, the cone, the sphere and the torus. | 55–85 |
| K6 | STEP export for the analytic subset. | 6–12 |
| K7 | General extrude and revolve of a simple curve. **Optional.** Year two. | 40–65 |

## Track V — Machine vision

The first domain extension. Objective 15 puts it before materials, chemistry and biological data.

| Phase | Content | Evenings |
|---|---|---|
| V0 | A verification spike. The vision library on three platforms. | 1 |
| V1 | A content-addressed asset store. A textured quad. | 2–3 |
| V2 | Place an image on a datum. Calibrate it to real units. | 2–3 |
| V3 | A contour proposal becomes an editable sketch. Propose and accept. | 3–5 |

## Order

The tracks are not strictly sequential, but three rules hold:

1. Track 0 comes first. Each phase in it removes a condition that blocks other work. Two exceptions
   are on record: P0.16 and P0.17 must be done before R5, and R2 can start before them. P0.13 and
   P0.15 must be done before R2, because the tests of R2 need a serial read and a native backend
   that surely ran.
2. R2 blocks each later phase in track R. No mesh leaves the geometry backend today.
3. K4 needs K0, K1 and K2. Do not start K4 before the reference model exists.

The field `depends-on` of each task file holds the same order. `TaskGovernanceGateTests` verifies
that each dependency names a task.

Phase R6 gives the first objective that the owner named: create a box, subtract a second box, and
observe the cut. Track 0 and phases R1 to R6 give it in 25 to 35 evenings.

## Not on a track

`docs/CHARTER.md` holds each deferred non-goal and each anti-objective. An item there has no phase.
It needs an ADR and a task first.
