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
- P0.13 — One serial boundary for commands, queries and snapshots. v0.39, TASK-0034.
- P0.14 — The version counts applied commands, and a replay rebuilds it. v0.43, TASK-0035, ADR-0020.
- P0.15 — A host refuses to start without the native backend. v0.44, TASK-0036.
- P0.16 — An operation consumes its operands, and the Document holds the live bodies only. v0.49, TASK-0037, ADR-0021.
- P0.17 — The judge of the write set runs from a build of main. v0.51 and v0.52, TASK-0053.
- P0.18 — The judge and the contract gate close the holes of the fifth review. v0.54, TASK-0057.
- P0.19 — The guards close the gaps of the fifth review. v0.56, TASK-0058.
- P0.20 — The surface is a library, and `engine-api-http` is its program. v0.57, TASK-0038, ADR-0019.
- P0.21 — The desktop host owns a session and serves the surface. v0.58, TASK-0054, ADR-0019.
- P0.23 — The judge judges each change of a merge, and its tests hold each rule. v0.61, TASK-0061.
- P0.24 — The hosts refuse each bracket form, stop well, and own their session. v0.62, TASK-0062.
- P0.25 — The gates that read the repository run from a build of main. v0.63, TASK-0064.
- R2 — A mesh leaves the geometry backend through the query `GetTessellation`. v0.64, TASK-0028, ADR-0018.
- P0.26 — The checks from main take no input of the build from the judged checkout. v0.67, TASK-0066.
- R3 — The desktop draws an indexed mesh through the first pipeline. v0.68, TASK-0069, ADR-0022.
- R4 — An orbit camera, reversed depth, and a draw list over the scene graph. v0.69, TASK-0070, ADR-0023.
- R5 — The desktop draws each live body of its session, from the events and GetTessellation. v0.70, TASK-0071, ADR-0024.
- P0.28 — No file outside the gate files can empty the step "Test", and the gates see what MSBuild evaluates. v0.73, TASK-0073.
- R6 — A person observes the cut of the first demonstration, and an image test with no window holds it. v0.74, TASK-0076, ADR-0025. The first objective is complete.
- P0.29 — The subscription reports and isolates its failures, and the renderer survives a resize, an old device and a bad camera. v0.75, TASK-0074.
- P0.27 — The hosts stop within a short limit, give a code for each failure of the start, and write compact JSON. v0.76, TASK-0067.
- P0.30 — No file outside the gate files can empty a test project, and the gates read MSBuild names as MSBuild does. v0.79, TASK-0078.
- P0.22 — `/schema/events` gives the payload fields of each event kind. v0.80, TASK-0055.

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
| P0.17 | The write-set check runs outside the test assembly. TASK-0053. | 1 |
| P0.18 | The judge and the contract gate close the holes of the fifth review. TASK-0057. | 1–2 |
| P0.19 | The address guard, the session guard and the consumed list close their gaps. TASK-0058. | 1 |
| P0.20 | The surface is a library, and `engine-api-http` is its program. ADR-0019, TASK-0038. | 2–3 |
| P0.21 | The desktop host owns a session and serves the surface. ADR-0019, TASK-0054. | 1–2 |
| P0.22 | `/schema/events` gives the payload fields of each kind. ADR-0008 §9, TASK-0055. | 1 |
| P0.23 | The judge judges each change of a merge, and its tests hold each rule. TASK-0061. | 1–2 |
| P0.24 | The hosts refuse each bracket form, stop well, and own their session. TASK-0062. | 1–2 |
| P0.25 | The gates that read the repository run from a build of main. TASK-0064. | 3–4 |
| P0.26 | The checks from main take no input of the build from the judged checkout, and the judge reads each change of a merge. TASK-0066. | 2–3 |
| P0.27 | The hosts stop within a short limit, give a code for each failure of the start, and write compact JSON. TASK-0067. | 1 |
| P0.28 | The test step cannot be emptied by a file that is not a gate file, and the gates see what MSBuild evaluates. TASK-0073. | 2–3 |
| P0.29 | The subscription reports and isolates its failures, and the renderer survives a resize, an old device and a bad camera. TASK-0074. | 2–3 |
| P0.30 | No file outside the gate files can empty a test project, and the gates read MSBuild names as MSBuild does. TASK-0078. | 2–3 |
| P0.31 | A stop waits for a running command, and the present path is checked for hazards. TASK-0079. | 2 |
| P0.32 | The desktop uses a binding with SDL 3.2 or later. TASK-0081. | 1–2 |
| P0.33 | A codebase review follows the product milestones. TASK-0083. | 1 |

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
| R3 | A pipeline. A triangle, then an indexed mesh. TASK-0069. | 4–6 |
| R4 | A camera with depth. An orbit camera, reversed depth, and a draw list over the scene graph (ADR-0023). TASK-0070. | 5–7 |
| R5 | Geometry from the engine. Subscribe to events, fetch a mesh, draw it. Subtract a double-precision origin before the narrowing to float. Choose `frontFace` together with the viewport flip. TASK-0071. | 6–8 |
| R6 | Observe a cut. The live body set of ADR-0021 gives the result. A headless image test. TASK-0076. | 6–8 |
| R7 | Device-tagged pointer samples. The intent layer and the parity test. TASK-0082. | 5–7 |
| R8 | Name and visibility as commands. The selection stays a state of the client (ADR-0007; the owner, 2026-10-17). | 4–6 |
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
   are on record: P0.23 to P0.25 must be done before R2 and before P0.22, which R5 does not need. P0.13 and
   P0.15 must be done before R2, because the tests of R2 need a serial read and a native backend
   that surely ran. P0.26 must be done before R3: the owner answered question Q1 of the codebase
   review of 2026-10-12 on 2026-10-12. P0.27 holds three low findings and has no place in the order. The
   codebase review of 2026-10-13 put P0.28 before R6 (question Q1, answered by the owner on 2026-10-14). P0.29 comes
   after R6, except the parts that R6 takes. P0.30 and P0.32 come before R7 (the owner answered
   questions Q1 and Q2, D7, of the codebase review of 2026-10-17 on 2026-10-17). The order of the owner of
   2026-10-27: P0.33, P0.32, R7, TASK-0040, then P0.31, whose findings are medium or low.
2. R2 blocks each later phase in track R. No mesh leaves the geometry backend today.
3. K4 needs K0, K1 and K2. Do not start K4 before the reference model exists.

The field `depends-on` of each task file holds the same order. `TaskGovernanceGateTests` verifies
that each dependency names a task.

Phase R6 gave the first objective that the owner named: create a box, subtract a second box, and
observe the cut. P0.28 and R6 shipped it at v0.74, on 2026-10-16. The next objective has no estimate yet.

## Not on a track

`docs/CHARTER.md` holds each deferred non-goal and each anti-objective. An item there has no phase.
It needs an ADR and a task first.
