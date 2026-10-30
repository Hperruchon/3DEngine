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
- P0.33 — A codebase review follows the product milestones. v0.81, TASK-0083.
- P0.32 — The desktop uses a binding with SDL 3.2 or later. v0.82, TASK-0081.
- R7 — Touch, mouse and keyboard give the same intents, and a parity test holds it. v0.83, TASK-0082, ADR-0026.
- P0.31 — A stop waits for a running command, and the present path is checked for hazards. v0.85, TASK-0079.
- R7b — Text and panels: Dear ImGui through ImGui.NET, drawn by a first-party renderer. v0.86, TASK-0084, ADR-0027.
- R8a — A body has a name and a visibility that commands change. v0.87, TASK-0085, ADR-0028.
- R8b — The desktop selects, hides, shows and renames a body, and the camera stays where it is. v0.88, TASK-0086, ADR-0029.
- P0.34 — No file outside the gate files can replace the test framework of a test project. v0.90, TASK-0088.
- P8a — The document file in the engine and the server. v0.91, TASK-0091, ADR-0015.
- P8f — The document file in the desktop and for an agent. v0.92, TASK-0092, ADR-0030.
- P0.36 — A name is a JSON string of well-formed text, the server disposes at each stop, and the desktop ends a text field at a click on the scene. v0.93, TASK-0090.
- P0.35 — The gates bind the names that the compiler and MSBuild bind, and the archive and the review counter check their links. v0.94, TASK-0089.
- P8c — Undo and redo as commands of the log, with one history for the document. v0.95, TASK-0093, ADR-0031.
- R10 — A numeric pad and an expression evaluator for a new box and a move, and the keys of undo and redo. v0.96, TASK-0094, ADR-0032.
- P0.37 — An open applies each entry of the document file one time. v0.98, TASK-0096.
- P0.38 — The test framework gate reads what MSBuild compiles into a test project. v0.99, TASK-0097.
- K2a — The engine holds a sketch of points, lines and five constraints, and the desktop draws it. v0.100, TASK-0100, ADR-0033.
- K2b — The desktop draws a sketch of lines and adds its constraints, by touch and by mouse. v0.101, TASK-0101, ADR-0034.
- P0.40 — The document file keeps each complete line and each held file, and the pad applies to the current selection. v0.102, TASK-0099.
- K3a — The engine solves the five constraints of a sketch with `+ - * / sqrt` only, and the desktop shows if a sketch is solved. v0.103, TASK-0102, ADR-0035.
- P0.39 — The gates read the names, the packages and the assemblies that the build binds, and the review counter reads each heading. v0.104, TASK-0098.
- K0a — The engine extrudes the closed profile of a solved sketch over Manifold, and the desktop extrudes the active sketch. v0.105, TASK-0103, ADR-0036.
- P0.41 — The test framework gate reads the output path and each MSBuild file of a test project. v0.107, TASK-0105.
- K0b — An edit of a sketch rebuilds each live extrude of it under its handle and a new geometry key, and a broken profile gives a fault. v0.108, TASK-0108, ADR-0037.
- K0c — The desktop draws the rebuilt solid of an extrude and shows the fault of a body. The milestone "CAD on one part" is complete. v0.109, TASK-0109.
- P0.42 — The sketch refuses an identifier that it holds, and the pads of the sketch follow the state that they act on. v0.110, TASK-0106.
- P0.43 — The gates read the target name, the root folders and each text file, and a host test waits for its file. v0.111, TASK-0107.
- K2c — The engine solves pointOn, midpoint, parallel, perpendicular, equal, symmetric and angle. v0.112, TASK-0110.
- P0.44 — No MSBuild element gives a client the assembly name of another project. v0.114, TASK-0112.
- P0.46 — The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says. v0.115, TASK-0115.
- K0d — An edit of a sketch rebuilds each translate and subtract that descends from its extrude. v0.116, TASK-0116.
- K2d — The engine holds the circle and the arc of a sketch, and solves tangent, radius, pointOn and equal on them. v0.117, TASK-0117.
- P0.45 — The sketch keeps read-only lists and refuses each identifier that the log holds, and the gates read the forms of the thirteenth review. v0.118, TASK-0114.
- R9 — The desktop moves the selected body with a translate gizmo. v0.119, TASK-0113.
- P0.47 — The pads of the sketch follow their values, and the desktop meshes each body one time for each frame. v0.120, TASK-0118.
- P8e — The numeric pad shows the length unit of the document. v0.121, TASK-0119.
- P0.48 — A solve keeps each radius of a circle positive. v0.123, TASK-0121.
- K2e — The desktop draws and adds circles and arcs, and the panel offers the constraints of K2d on a curve. v0.124, TASK-0122.
- P0.49 — The engine, the surface and the gates close the forms of the fourteenth review. v0.125, TASK-0123.
- P0.50 — The gizmo stays in front of the eye, and the overlay keeps each visible part of a line. v0.126, TASK-0124.
- K2f — The window of the sketch offers the constraints of K2c on lines. v0.127, TASK-0125.
- K2g — The selection holds three items, and the window of the sketch offers Symmetric. v0.128, TASK-0127.
- K3 — The constraint solver holds the fourteen kinds of ADR-0033 item 3. Closed at v0.129, from TASK-0102, TASK-0110 and TASK-0117.

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
| P0.34 | No file outside the gate files can replace the test framework of a test project. TASK-0088. | 1 |
| P0.35 | The gates bind the names that the compiler and MSBuild bind, and the archive and the review counter check their links. TASK-0089. | 2–3 |
| P0.36 | A name is a JSON string of well-formed text, the server disposes at each stop, and the desktop ends a text field at a click on the scene. TASK-0090. | 2–3 |
| P0.37 | An open applies each entry of the document file and gives its version, or refuses the file. TASK-0096. | 1 |
| P0.38 | The test framework gate reads each source and each alias that MSBuild compiles into a test project. TASK-0097. | 1 |
| P0.39 | The gates read the names, the packages and the assemblies that the build binds, and the review counter reads each heading. TASK-0098. | 2–3 |
| P0.40 | The document file keeps each complete line and each held file, and the pad applies to the current selection. TASK-0099. | 3–4 |
| P0.41 | The test framework gate reads the output path and each MSBuild file of a test project. TASK-0105. | 1 |
| P0.42 | The sketch refuses an identifier that it holds, and the pads of the sketch follow the state that they act on. TASK-0106. | 3–4 |
| P0.43 | The gates read the target name, the root folders and each text file, and a host test waits for its file. TASK-0107. | 2–3 |
| P0.44 | No MSBuild element gives a client the assembly name of another project (T88 of the codebase review of 2026-10-30). TASK-0112. | 1 |
| P0.45 | The sketch keeps read-only lists and refuses each identifier that the log holds, and the gates read the forms of the thirteenth review (E94 to E96, T89 to T95). TASK-0114. | 2–3 |
| P0.47 | The pads of the sketch follow their values, and the desktop meshes each body one time for each frame (V46 to V50, R-0043; after R9). TASK-0118. | 2–3 |
| P0.46 | The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says (E11, E14, E16, R-0043; the plan of 2026-10-30). TASK-0115. | 2–3 |
| P0.48 | A solve keeps each radius of a circle positive (E97 of the codebase review of 2026-10-30 (b); before K2e). TASK-0121. | 1–2 |
| P0.49 | The engine, the surface and the gates close the forms of the fourteenth review (E98 to E101, T96 to T102; after K2e; E99 and E100 after Q1). TASK-0123. | 3–4 |
| P0.50 | The gizmo stays in front of the eye, and the overlay keeps each visible part of a line (V51 to V55; after K2e; V53 after Q2). TASK-0124. | 2–3 |

## Track P — The platform

| Phase | Content | Evenings |
|---|---|---|
| P8a | The document file in the engine and the server. The command log in JSON Lines, written by `Engine.Core/Persistence/` (Q7). Each applied command goes into the file before its result, with fsync. The header records the length unit of the document, `Document.LengthUnit`, with the millimetre as the only unit for now (KD4, Q3). Each entry records the name and the version of the backend from `IGeometryBackend`, and the bus writes them (anti-objective 10, KD3, Q2). An open gives a new document identifier (Q1). A document with no name writes a temporary file from its first command, and a save moves it (Q4). The server opens the file that its command line names. ADR-0015 is written again (register entry R-0025). TASK-0091. | 3–4 |
| P8b | The container model. One document holds one part. An assembly holds references at a fixed version. | 5–8 |
| P8c | Undo and redo. `Undo` and `Redo` are commands of the log, and the bus reverses the recorded body changes of a step (Q1 B to Q4 A). The keys wait in R-0041. ADR-0031, TASK-0093. | 3–5 |
| P8d | The three layers. Log, ordered feature list, regeneration cache. | 5–8 |
| P8e | The unit in the numeric pad. The header of P8a holds the length unit of the document (KD4). TASK-0119. | 1 |
| P8f | The document file in the desktop and for an agent. The desktop opens the file that its command line names, and an open in the window starts the program again (Q5). "Save" moves the file to the name typed in the panel. HTTP save and open on the loopback address, in one folder (Q6, ADR-0002). ADR-0030, TASK-0092. | 2–3 |

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
| R7b | Text and panels. Dear ImGui through ImGui.NET, a first-party 2D renderer, and the input of the intent layer (ADR-0027). TASK-0084. | 4–6 |
| R8a | Name and visibility in the engine. The commands `RenameBody` and `SetBodyVisibility`, the query `GetBodyInfo`, the event kind `body.changed`, and ADR-0028. TASK-0085. | 3–4 |
| R8b | Name and visibility in the desktop. The pick of one body, a highlight, the gestures, and the text field of R7b. The selection stays a state of the client (ADR-0007). The camera frames the bodies only when the set goes from empty to not empty (KD6). TASK-0086. | 4–5 |
| R9 | A transform gizmo at a constant screen size. After the milestone "CAD on one part" (KD2). Translate only, from the defaults R9-Q1 to Q8 of 2026-10-27. ADR-0038, TASK-0113. | 5–8 |
| R10 | A numeric pad and an expression evaluator. The pad gives the sizes of a new box and the offsets of a move; the drags of the camera have no numeric equivalent. The keys of undo and redo (R-0041). ADR-0032, TASK-0094. | 3–5 |
| R11 | Escalated selection and a snap engine. | 4–6 |

## Track K — The kernel

The owned kernel gives identity. Manifold gives geometry. `docs/CHARTER.md` holds the closed feature
boundary and each refused rung.

| Phase | Content | Evenings |
|---|---|---|
| K0a | Extrude a closed profile of line segments over Manifold. The command records the identifier of its sketch and a distance, not coordinates, and the handler reads the current sketch (Q7 of the second review of 2026-10-28). No arc, because an arc needs a sine and a cosine (determinism rule 1). The first commit checks that the native package exports the extrude function (KD5). ADR-0036, TASK-0103. | 3–5 |
| K0b | The engine and HTTP: an edit of a sketch rebuilds each live extrude of the sketch under its handle and a new geometry key, and a profile that breaks gives a fault on the body and keeps its last solid (Q1 to Q5 A of the codebase review of 2026-10-29). An ADR resolves the conflict with ADR-0021 (KD5). TASK-0108. | 4–6 |
| K0c | The desktop: a body that a rebuild changes gets its new mesh, and the panel shows the fault of a body (Q6 A of the codebase review of 2026-10-29). TASK-0109. | 2–4 |
| K0d | An edit of a sketch rebuilds each translate and subtract that descends from its extrude, as a projection of the log (KQ1 to KQ4 A of 2026-10-30, and Q1 to Q3 A of the codebase review of 2026-10-30). ADR-0039, TASK-0116. | 6–9 |
| K0e | An extrude takes a profile of lines and arcs, or one circle, as a polygon of + - * / sqrt (the answer "1 3 4" of 2026-10-31, item 1). ADR-0040, TASK-0126. | 4–6 |
| K0 | A feature graph over Manifold. No B-Rep. Deferred-query references. The rest after K0d. | 12–20 |
| K1 | Exact predicates. A deterministic arithmetic layer. | 4–8 |
| K2a | The sketch model in the engine. The point, the line segment that references two points (Q3), the fixed frame of a sketch, and the five constraints of K3a. A sketch records the identifier of a datum; the three origin planes have fixed identifiers (Q4). One command for each item, with scalar fields; the Document holds one sketch record for each sketch, and an undo restores the record before; the events `sketch.created`, `sketch.changed` and `sketch.removed`, and a query `GetSketch` (Q2). The positions are a projection of the record, and the log holds the inputs only (Q6). An angle records the pair (cos θ, sin θ) (Q5). The ADR names the five entities and the fourteen constraints now (Q1). The desktop draws a sketch as lines of the UI layer, read only (Q9). ADR-0033, TASK-0100. | 4–6 |
| K2b | The sketch tools of the desktop. A sketch mode in the intent layer, a ray to the plane of the sketch, a pad for a line, and a panel of constraints (Q8). ADR-0034, TASK-0101. | 4–6 |
| K2c | The seven constraints of ADR-0033 item 3 on points and lines: pointOn, midpoint, parallel, perpendicular, equal, symmetric and angle, with their equations in the solver (the plan of 2026-10-30). TASK-0110. | 3–5 |
| K2d | The circle and the arc in the engine: two commands, the radius as a variable of the solver, the equation of an arc, and tangent, radius, pointOn and equal on a curve (the plan of 2026-10-30). The desktop waits for K2e. TASK-0117. | 4–6 |
| K2e | The desktop draws and adds circles and arcs, and the panel offers the constraints of K2d on a curve (the codebase review of 2026-10-30 (b), sections 5 and 6). TASK-0122. | 4–6 |
| K2f | The window of the sketch offers the constraints of K2c on lines: On line, Midpoint, Equal, Parallel, Perpendicular and Angle (the Scope (out) of K2e). TASK-0125. | 1–2 |
| K2g | The selection holds three items, and the window of the sketch offers Symmetric (the answer "1 3 4" of 2026-10-31). TASK-0127. | 1 |
| K3a | A constraint solver for five constraints: coincident, horizontal, vertical, distance and fixed. It uses only `+ - * / sqrt`, and its output stays outside the log (KD5). ADR-0035, TASK-0102. | 8–12 |
| K3 | The rest of the constraint solver, after K3a. Done: the fourteen kinds of ADR-0033 item 3 are in K3a (TASK-0102), K2c (TASK-0110) and K2d (TASK-0117); the owner closed the row on 2026-10-31. | 4–8 |
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
   2026-10-27: P0.33, P0.32, R7, TASK-0040, then P0.31, whose findings are medium or low. The codebase review of
   2026-10-28 puts P0.34, which holds a high finding, before P8a, and P0.35 and P0.36 after P8a, by the rule of
   `docs/templates.md`, section 7. On 2026-10-28, after P8f, the owner put P0.36 and then P0.35 before P8c, because
   a defect of P0.36 can put a wrong rename into the document file.
   The second codebase review of 2026-10-28 puts P0.37 and P0.38, which hold high findings, before K2a, and P0.39
   and P0.40 after K2a, by the rule of `docs/templates.md`, section 7. On 2026-10-28, after K2b, the owner put P0.40
   before K3a and before P0.39, because findings E76 and V32 of P0.40 are risks to the data of the work after K2b.
   On 2026-10-29, after P0.40, the owner put K3a before P0.39.
   The codebase review of 2026-10-29 puts P0.41, which holds a high finding, before K0b, and P0.42 and P0.43 after
   K0b, by the rule of `docs/templates.md`, section 7.
2. R2 blocks each later phase in track R. No mesh leaves the geometry backend today.
3. K4 needs K0, K1 and K2. Do not start K4 before the reference model exists.
4. The path to the milestone "CAD on one part" (the owner, 2026-10-27, KD2): R7b, R8a, R8b, the codebase
   review when its gate makes it due, P8a, P8f, P8c, R10, K2a, K2b, K3a, K0a, K0b, then K0c. R9 comes after the
   milestone. The owner answered the questions of the codebase review of 2026-10-29 on 2026-10-29, A to each: K0b
   splits into K0b, the engine and HTTP, and K0c, the desktop.
   The owner answered the questions of the second codebase review of 2026-10-28 on 2026-10-28, A to each: K2
   splits into K2a and K2b on the path, and the rest of K2 comes after the milestone with the rest of K3.
   `docs/proposals/2026-10-27-path-to-cad-decision-sheet.md` gives the analysis. A slice of a phase has a
   letter after its number (K0a, K0b, K3a), so that the review gate reads it as a phase of its track.

The field `depends-on` of each task file holds the same order. `TaskGovernanceGateTests` verifies
that each dependency names a task.

Phase R6 gave the first objective that the owner named: create a box, subtract a second box, and
observe the cut. P0.28 and R6 shipped it at v0.74, on 2026-10-16. The next milestone is "CAD on one part": a
sketch with constraints, an extrude, and a solid that follows an edit of the sketch. The decision sheet of
2026-10-27 estimates 43 to 67 evenings and two codebase reviews.

## Not on a track

`docs/CHARTER.md` holds each deferred non-goal and each anti-objective. An item there has no phase.
It needs an ADR and a task first.
