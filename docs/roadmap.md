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
- K0e — An extrude takes a profile of lines and arcs, or one circle. v0.130, TASK-0126, ADR-0040.
- P0.51 — A radius at the noise level does not solve, and the check of the tasks binds the real gate files. v0.132, TASK-0130.
- R11a — A tap of the sketch snaps to a midpoint, a line, a curve or the grid. v0.133, TASK-0131, ADR-0041.
- P0.52 — The engine, the surface and the gates close the forms of the fifteenth review. v0.134, TASK-0132.
- P0.53 — The pad of an angle takes degrees, and the tools and the window follow the review. v0.135, TASK-0133.
- P0.54 — The log of the Document is read-only, a cancelled replay is a cancellation, and a query of a lost body gives an error. v0.136, TASK-0135.
- P0.55 — An exception of a handler in a replay is a divergence, and a replay needs a backend. v0.137, TASK-0136.
- P0.56 — The command line checks a name before the backend, and the comments of the replay are true. v0.138, TASK-0137.
- P0.57 — The reason of a native library that does not load separates a missing file from a platform with no payload. v0.139, TASK-0138.
- P0.58 — The schema of the backend is in the list of the surface, and it gives the identity of the backend. v0.140, TASK-0139.
- P0.59 — A sink of events does not throw, and a test holds each sink to it. v0.141, TASK-0140.
- R11b — The overlay marks the point that a tap under the mouse or a finger would take. v0.143, TASK-0134, ADR-0041.
- P0.60 — The open follows the replay, the adds follow the solver, and the comments of the cache are true. v0.144, TASK-0142.
- P0.61 — The gates read disabled text, the routes of the test host, each sink, and the next forms of the fifteenth review. v0.145, TASK-0143.
- P0.62 — An arc ends within a pixel of its start, and Escape cancels a press on a handle before the slop. v0.146, TASK-0144.
- V0 — The vision library loads and finds a contour on the three platforms of the pipeline. v0.147, TASK-0145, ADR-0042.
- V1b — The engine keeps each asset under the SHA-256 of its bytes, in one store of the account. v0.148, TASK-0147, ADR-0044.
- V1a — The scene draws an image on a quad with the depth of the bodies. v0.149, TASK-0146, ADR-0043.
- V1 — A content-addressed asset store and a textured quad. Closed at v0.149, from TASK-0146 and TASK-0147.
- V2a — The Document holds an image on a datum, and a command calibrates it to real units. v0.151, TASK-0152, ADR-0045.
- V2b — The desktop places a PNG file on a datum, draws it, and calibrates it with two taps. v0.152, TASK-0153, ADR-0046.
- V2 — An image on a datum, calibrated to real units. Closed at v0.152, from TASK-0152 and TASK-0153.
- P0.63 — The store of the assets gives a code for each error and repairs a damaged file, and an arc follows the solver. v0.153, TASK-0149.
- P0.64 — The pin rules read each imported file, the sink gate reads the desktop, and the next forms of the sixteenth review fail. v0.154, TASK-0150.
- P0.65 — A cancelled press gives nothing, and the mark of the snap is where the point goes and only while a finger is down. v0.155, TASK-0151.
- P0.66 — The lag test of the events fills the queue of the subscriber with a count and no timing. v0.156, TASK-0154.
- V3 — A contour proposal of a placed image becomes an editable sketch. v0.157, TASK-0155, ADR-0047.
- P0.67 — Each runner hashes a native replay with geometry and solved sketches, and a last job compares the hashes. v0.158, TASK-0156.
- P8g — The live bodies leave the engine as one binary STL file, from the surface, the command line and the desktop. v0.159, TASK-0157, ADR-0048.
- P0.68 — The PNG reader refuses each error of the decompressor, so an image cannot stop the desktop. v0.161, TASK-0159.
- K0f — A command changes the distance of a live extrude, and the extrude and its chain rebuild. v0.162, TASK-0163, ADR-0049.
- P0.69 — The export reads the document file only, the store takes parallel puts, and each edge of the export and the calibration gives a code. v0.163, TASK-0160.
- P0.70 — The comparison refuses an empty hash, the pin rules read a global reference, and the next forms of the seventeenth review fail. v0.164, TASK-0161.
- P0.71 — A proposal gives a simple polygon, the taps of a calibration reach it, and the image actions follow their refusals. v0.165, TASK-0162.
- K1 — The profile of a sketch uses exact predicates: a rounded filter and an exact path with integers. v0.166, TASK-0164, ADR-0050.
- R11c — A tap of the sketch snaps to the intersection of two lines, of a line and a curve, or of two curves. v0.167, TASK-0165, ADR-0041.
- R11 — The snaps of the sketch. Closed at v0.167, from TASK-0131, TASK-0134 and TASK-0165; the escalated selection moves to R12.
- K0g — A datum on a face of an extrude or a box, a sketch on it, and the rebuild of each datum after an edit. v0.168, TASK-0166, ADR-0051.
- K0h — The edges and the vertices of an extrude and a box as references in a sketch. v0.170, TASK-0167, ADR-0052.
- P0.72 — A datum and a reference refuse a value that is not finite, and warn on a new fault only. v0.171, TASK-0169.
- P0.73 — The mark of the snap hides during a calibration, the snap of a large sketch fits in a frame, and the next forms of three gates fail. v0.172, TASK-0170.
- K0i — A tap on a face starts a sketch on a datum of it, and the desktop draws a sketch on a datum of the Document. v0.173, TASK-0171, ADR-0053.
- K0 — A feature graph over Manifold with deferred-query references. Closed at v0.173, from K0a to K0i in this list.
- P0.74 — A put of an asset waits for a name that another program holds, and does not replace a name that it cannot read. v0.174, TASK-0172.
- P8d1 — The ordered feature list is a fold of the log that the engine reads, and `GetFeatures` gives each feature with its inputs and its state. v0.175, TASK-0173, ADR-0054.
- P8d2 — One regeneration evaluates the features again after an edit, and each step whose inputs did not change keeps its solid. v0.176, TASK-0174, ADR-0055.
- P8d — The three layers: the log, the ordered feature list and the regeneration. Closed at v0.176, with P8d1 and P8d2 in this list.
- P0.75 — The walk of the planes repeats until no reference solves a sketch again, and an edit of a sketch takes the record of the walk. v0.178, TASK-0176.
- R13 — The desktop shows the dimensions of the overall box and of a selected body, with a toggle in the panel. v0.179, TASK-0178, ADR-0056.
- P0.77 — The far plane of the camera of the desktop follows the box of the visible bodies. v0.180, TASK-0179.
- P0.76 — A sketch on a face looks from the side of the eye, the arm of a face ends with a new sketch, a datum on a short side has unit axes, and the next forms of three gates fail. v0.181, TASK-0177.
- P0.78 — The far plane holds each visible body from the eye, and the dimension overlay keeps its labels apart and inside the window, follows a drag and reads the boxes once for each change. v0.183, TASK-0181.
- P0.79 — A datum on a diagonal subnormal side has unit axes, the walk of the planes gives its sketches in the order of the log, and the next forms of three gates fail. v0.184, TASK-0182.
- K4a — The owned kernel subtracts planar solids exactly, and each face of the result keeps its provenance. v0.185, TASK-0183, ADR-0057.
- K4b — The engine reads the owned kernel, and GetFaces gives the faces of a body with their names. v0.186, TASK-0184, ADR-0058.
- K4c — A datum on a face that a cut removed gets a fault, and a datum on the wall of a hole faces into the hole. v0.187, TASK-0185, ADR-0059.
- K4 — Planar faces with a boolean, and the provenance of each face inside the owned kernel. Closed at v0.187, with K4a to K4c in this list.
- P0.80 — A gate fails when an open task plans an ADR number that another ADR file or another open task has. v0.188, TASK-0187.
- P8h — Each number parameter of a command declares its unit kind in the schema. v0.189, TASK-0186, ADR-0060.
- P0.81 — A press and a release of the mouse take the place of the cursor, not the last place of a motion. v0.190, TASK-0188.
- P0.82 — A document whose log holds a datum on a face that a cut removed opens again. v0.192, TASK-0190.
- R14a — Android is a platform of the charter, with ADR-0061 and the rows of R14. v0.193, TASK-0193, ADR-0061.
- P0.83 — A press of a hand keeps its place, the far plane holds the images, and a drag queries no size in each frame. v0.194, TASK-0191.
- P0.84 — The kept solids belong to one Document, the faces of the kernel keep their holes, and the next forms of five gates fail. v0.195, TASK-0192.
- R14b — The host owns the document session, the surface mounts on it as an option, and an open stays in the same process. v0.196, TASK-0194, ADR-0061.
- R14c — ppy.SDL3-CS replaces Alimer.Bindings.SDL on the desktop, with no change of behaviour. v0.197, TASK-0195, ADR-0061.
- K6a — The live bodies leave the engine as one STEP file of their planar faces. v0.198, TASK-0200, ADR-0070.
- P0.87 — An export of the command line refuses a file that is its own document file. v0.200, TASK-0220.
- K6b — A void inside a body leaves the engine as a BREP_WITH_VOIDS. v0.201, TASK-0201, ADR-0070.
- P8b1 — A document is a part or an assembly of references to parts at a fixed version. v0.202, TASK-0202, ADR-0071.
- R14d — The native builds of Manifold and cimgui for android-arm64 and android-x64 run on GitHub Actions. v0.203, TASK-0196, ADR-0061.
- P0.85 — Each hole of a face has its owner, the STEP export refuses what a strict reader refuses, and the session of the desktop holds an open. v0.204, TASK-0197.
- P0.88 — An export of the command line resolves a link of each folder of its paths. v0.205, TASK-0221.
- P0.86 — The window keeps no input between sessions, the hook has the cases of one axis, and the next forms of three gates fail. v0.206, TASK-0198.
- R14e1 — The drawing code of the desktop becomes a library that an Android program can reference. v0.207, TASK-0222, ADR-0072.
- Review — The codebase review of 2026-11-06 (b). v0.208, TASK-0223.
- R14e2 — The Android program builds an APK that draws a document of its own session. v0.209, TASK-0226, ADR-0072.
- P0.89 — A void on the plane of a distant face is written, a replay and an export know an assembly, and two gates read the transitive form. v0.210, TASK-0224.
- P0.90 — The start again keeps an unsaved document, a window that does not open gives a code, and the flush holds its place. v0.211, TASK-0225.
- R14e3 — A person cuts a body in the panel, and the phone keeps its document through the background. v0.212, TASK-0227, ADR-0073.
- R14f1 — The pipeline builds the APK of the phone at each push. v0.213, TASK-0228.
- R14f2 — The APK holds its assemblies, and the pipeline starts it on the emulator up to its first frame. v0.214, TASK-0229, ADR-0072.
- Review — The codebase review of 2026-11-06 (c). v0.215, TASK-0230.
- R14f3 — The pipeline compares the determinism hash of the phone with the hashes of the desktop runners. v0.216, TASK-0233, ADR-0074.
- P0.91 — The review gate counts each product phase, and the export, the host and three checks hold their next forms. v0.217, TASK-0231.
- P0.92 — The start again sees the commands of the stop, and the panel and the phone hold their next forms. v0.218, TASK-0232, ADR-0073.
- R15 — The user interface of the desktop is one partial file for each region. v0.219, TASK-0234.
- P0.93 — The smoke test of the emulator gives the cause of a start that fails. v0.220, TASK-0235.
- R12a — A second tap on the selected body selects the face under the point. v0.221, TASK-0236, ADR-0075.
- P0.94 — The key Escape goes back from the face, and the button "Sketch on face" ends the arm of Face. v0.222, TASK-0237.
- K5a — A command CreateCylinder makes a cylinder of 64 plane facets, with three face names. v0.223, TASK-0238, ADR-0076.
- P0.95 — The smoke test of the emulator gives the exception of the program as public annotations. v0.224, TASK-0239.
- Review — The codebase review of 2026-11-06 (d). v0.225, TASK-0240.
- P0.96 — A save to a new name keeps a handle on the document at each moment. v0.226, TASK-0241.
- K5b — A list "New" of the panel of the desktop and of the phone makes a box or a cylinder. v0.227, TASK-0244, ADR-0076.
- P0.98 — A cancelled finger ends, the activity tells its starts, and the face and the texts hold their next forms. v0.228, TASK-0243.
- P0.97 — The readers of a file, the pin rule, the smoke test and the comparison hold their next forms. v0.229, TASK-0242.
- R12b — A tap near an edge of the selected face selects the edge. v0.230, TASK-0245, ADR-0077.
- P0.99 — The activity of the phone may start again in its process. v0.231, TASK-0247, ADR-0079.
- R16a — The desktop and the phone take the theme "Proposed (harmonized)" of the redesign. v0.232, TASK-0246, ADR-0078.
- R16b1 — The title of the document, Undo, Redo and the status line are fixed controls of the redesign. v0.233, TASK-0248, ADR-0080.
- Review — The codebase review of 2026-11-06 (e). v0.234, TASK-0249.
- P0.100 — A second session of the phone in its process makes its window. v0.235, TASK-0250.
- R16b2 — The tools of the selected body are the segments of a ring that follows it. v0.236, TASK-0253, ADR-0081.
- P0.101 — A failed delete, an edge of more than two faces, two gates and the smoke test hold their next forms. v0.237, TASK-0251.
- P0.103 — The session of a destroyed activity ends before its window. v0.238, TASK-0255, ADR-0079.
- P0.104 — A failure of the window of the phone and a new create of its activity name their cause. v0.239, TASK-0256.
- P0.102 — The kept face, the panel, the title, the open list, the edge and the safe area hold their next forms. v0.240, TASK-0252.
- R16c — A value that a command changes sits on the edge of the selected body that it measures. v0.241, TASK-0254, ADR-0082.
- P9a — An STL file opens as a mesh body of a new part, through one import command (the record). v0.242, TASK-0257, ADR-0083.
- P9b — The engine reads an STL file into a mesh body. v0.243, TASK-0258, ADR-0083.
- Review — The codebase review of 2026-11-07. v0.244, TASK-0261.
- P0.105 — The check of an open reads the store of the assets. v0.245, TASK-0262.
- P0.108 — A lost surface of a destroyed activity ends the session quietly. v0.246, TASK-0265, ADR-0079.
- P9c — The desktop opens an STL file from its home screen, and the row waits for the owner. v0.247, TASK-0259, ADR-0083.
- P9d — The phone opens an STL file from its home screen, and the row waits for the owner. v0.248, TASK-0260, ADR-0083.
- P9e — The test sheet of the STL demonstration, and the row waits for the owner. v0.249, TASK-0266.
- P0.106 — A failed copy, a test of the faces, the STL reader, the command line, two gates and the affects fields hold
  their next forms. v0.250, TASK-0263.
- P0.107 — The chips, the ring and the collapsed panel hold their next forms. v0.251, TASK-0264, ADR-0081.
- R16d — The data of the selected body is in rows with no panel at the upper right. v0.252, TASK-0267, ADR-0084.
- Review — The codebase review of 2026-11-07 (b). v0.253, TASK-0268.
- P0.109 — The wait of the phone checks its turn, the STL reader keeps the results of v0.243, and the test sheet says what
  the phone keeps. v0.254, TASK-0269, ADR-0079, ADR-0083.
- P0.110 — The import of a chosen STL reaches only its new part, and an open by name ends the home screen. v0.255,
  TASK-0270, ADR-0083.
- R16e — The active sketch of a live extrude moves out of its body, and the camera stays. v0.256, TASK-0273, ADR-0085.
- P0.113 — The name of the solid keeps the results of v0.250, and the test sheet says how the size comes back. v0.257,
  TASK-0274, ADR-0083.
- P0.111 — The home screen, the rows, the chips, the dialog and three comments hold their next forms. v0.258,
  TASK-0271, ADR-0084.
- P0.112 — The phone, the reader, the APK check, the gates and the roadmap hold their next forms. v0.259, TASK-0272,
  ADR-0072, ADR-0079, ADR-0083.
- R16f — While a sketch is active, the ring holds the tools of the sketch. v0.260, TASK-0275, ADR-0086.
- R16g — "Constrain" on the ring of a sketch opens the constraints that fit the selection. v0.261, TASK-0276, ADR-0087.
- P0.114 — The tests of the home state of the desktop run in one collection. v0.262, TASK-0278.
- R16h — While the ring of a sketch shows, the window of the sketch does not repeat it. v0.263, TASK-0277, ADR-0088.
- Review — The codebase review of 2026-11-07 (c). v0.264, TASK-0279.
- P0.115 — The reader gives the result of each earlier reader that accepted a file, and the ring of a sketch keeps
  each tool in reach. v0.265, TASK-0280, ADR-0083, ADR-0086, ADR-0088.
- P0.116 — The size limit, the lost surface, the taps over a window, the portrait ring and the host warnings hold
  their next forms. v0.266, TASK-0281, ADR-0083.
- P0.117 — The asset verb, the second ring, three comments and the dialog filter hold their next forms. v0.267,
  TASK-0282.
- Proposal — The cone, the sphere and the torus are closed meshes that the engine makes (Proposed, for the owner).
  v0.268, TASK-0283, ADR-0089.
- P0.118 — The ring of a sketch sees the windows that come after it, and a window at the left does not pull it under
  the panel. v0.269, TASK-0284, ADR-0086, ADR-0088.
- P0.119 — The platform step builds its probe in the phone host, and the proposal gives the volume of the sphere.
  v0.270, TASK-0285, ADR-0089.
- P0.120 — A surface lost while the phone engine starts ends the engine as the background does. v0.271, TASK-0286.
- P0.121 — Each query of a surface checks its result, and a device that fails to start frees what it made. v0.272,
  TASK-0287.
- K5c — A command CreateCone makes a cone, a closed mesh that the engine stores as a solid. v0.273, TASK-0288,
  ADR-0089.
- P9f — A start of the desktop with a file opens it and shows no home screen. v0.274, TASK-0290, ADR-0083.
- P0.122 — The HTTP answer of a save lists its warnings. v0.275, TASK-0291, ADR-0090.
- Review — The codebase review of 2026-11-08. v0.276, TASK-0293.
- P0.123 — The main function of a stale session of the phone does not return, and a native crash gives its stack.
  v0.277, TASK-0294, ADR-0079.

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
| P0.51 | A radius at the noise level does not solve, and the check of the tasks binds the real gate files (E102, T103 of the codebase review of 2026-10-31; before R11a). TASK-0130. | 1–2 |
| P0.52 | The engine, the surface and the gates close the forms of the fifteenth review (E103 to E105, T104 to T109; after R11a). TASK-0132. | 3–4 |
| P0.53 | The pad of an angle takes degrees, and the tools and the window follow the review (V56 to V62; after R11a). TASK-0133. | 2–3 |
| P0.54 | The log of the Document is read-only, a cancelled replay is a cancellation, and a query of a lost body gives an error (E26, E29, E32 of R-0036). TASK-0135. | 1 |
| P0.55 | An exception of a handler in a replay is a divergence, and a replay needs a backend (E28, E30 of R-0036). TASK-0136. | 1 |
| P0.56 | The command line checks a name before the backend, and the comments of the replay are true (E37, E31 of R-0036). TASK-0137. | 1 |
| P0.57 | The reason of a native library that does not load separates a missing file from a platform with no payload (E38 of R-0036). TASK-0138. | 1 |
| P0.58 | The schema of the backend is in the list of the surface, and it gives the identity of the backend (E35 of R-0036). TASK-0139. | 1 |
| P0.59 | A sink of events does not throw, and a test holds each sink to it (E18 of R-0033, Q4 of 2026-10-04). TASK-0140. | 1 |
| P0.60 | The open follows the replay, the adds follow the solver, and the comments of the cache are true (E106 to E112 of the codebase review of 2026-10-31 (b); after R11b). TASK-0142. | 2–3 |
| P0.61 | The gates read disabled text, the routes of the test host, each sink, and the next forms of the fifteenth review (T110 to T116; after R11b). TASK-0143. | 2–3 |
| P0.62 | An arc ends within a pixel of its start, and Escape cancels a press on a handle before the slop (V63, V64; after R11b). TASK-0144. | 1 |
| P0.63 | The store of the assets takes only a true name and gives a code for each error, and an arc follows the solver (E113 to E118 of the codebase review of 2026-10-31 (c); after V2). TASK-0149. | 2–3 |
| P0.64 | The pin rules read each imported file, the sink gate reads the desktop, and the next forms of the sixteenth review fail (T117 to T122; after V2). TASK-0150. | 2–3 |
| P0.65 | A cancelled press gives nothing, and the mark of the snap is where the point goes and only while a finger is down (V65 to V67; after V2). TASK-0151. | 1–2 |
| P0.66 | The lag test of the events fills the queue of the subscriber with a count and no timing (register entry R-0047). TASK-0154. | 1 |
| P0.67 | Each runner hashes a native replay with geometry and solved sketches, and a last job compares the hashes (anti-objective 16, register entry R-0023). TASK-0156. | 1 |
| P0.68 | The PNG reader refuses each error of the decompressor, so an image cannot stop the desktop (V68 of the codebase review of 2026-11-01; before the next product phase). TASK-0159. | 1 |
| P0.69 | The export reads the document file only, the store takes parallel puts, and each edge of the export and the calibration gives a code (E119 to E124; after the next product phase). TASK-0160. | 2–3 |
| P0.70 | The comparison refuses an empty hash, the pin rules read a global reference, and the next forms of the seventeenth review fail (T123 to T126; after the next product phase). TASK-0161. | 1–2 |
| P0.71 | A proposal gives a simple polygon, the taps of a calibration reach it, and the image actions follow their refusals (V69 to V73; after the next product phase; V69 after Q3). TASK-0162. | 2–3 |
| P0.72 | A datum and a reference of a sketch refuse a frame or a point that is not finite, and give their warnings and codes as a body does (E125 to E127 of the codebase review of 2026-11-03; after K0h; E126 after Q3 and E127 after Q2). TASK-0169. | 1–2 |
| P0.73 | The mark of the snap during a calibration, the cost of the snap to an intersection, and the next forms of three gates (V74, V75, T127 to T129; after K0h). TASK-0170. | 1–2 |
| P0.74 | A put of an asset waits for a name that another program holds, and does not replace a name that it cannot read (register entry R-0050). TASK-0172. | 1 |
| P0.75 | The walk of the planes repeats until no datum and no sketch changes, and an edit of a sketch takes the record of the walk (E128 and E129 of the second codebase review of 2026-11-03; before R13 by the order of the owner). TASK-0176. | 1–2 |
| P0.76 | A sketch on a face looks from the side of the eye, the arm of a face ends with a new sketch, a datum on a short side has axes of the length 1, and the next forms of three gates (V76, V77, E130, T130 to T132; after R13). TASK-0177. | 2–3 |
| P0.77 | The far plane of the camera of the desktop follows the box of the visible bodies, so a model larger than 1000 mm shows whole (register entry R-0051; the owner, 2026-11-04, before P0.76). TASK-0179. | 1 |
| P0.78 | The far plane holds each visible body from the eye, and the dimension overlay keeps its labels apart and inside the window, follows a drag and reads the boxes once for each change (V78 to V84 of the codebase review of 2026-11-05; after the next product phase; V83 after Q2). TASK-0181. | 2–3 |
| P0.79 | A datum on a diagonal subnormal side has unit axes, the walk of the planes gives its sketches in the order of the log, and the next forms of three gates (E131 to E133, T133 to T135; after the next product phase). TASK-0182. | 1–2 |
| P0.80 | A gate fails when an open task plans an ADR number that another ADR file or another open task has (register entry R-0052; the owner, 2026-11-05, before P8h). TASK-0187. | 1 |
| P0.81 | A press and a release of the mouse take the place of the cursor, not the last place of a motion (register entry R-0043; the owner, 2026-11-05, before the codebase review). TASK-0188. | 1 |
| P0.82 | A document whose log holds a datum on a face that a cut removed opens again (E134 of the codebase review of 2026-11-05 (b); before the next product phase). TASK-0190. | 1 |
| P0.83 | A press of a hand keeps its place, the far plane holds the images, and a drag queries no size in each frame (V85 to V87 of the codebase review of 2026-11-05 (b); after the next product phase). TASK-0191. | 1–2 |
| P0.84 | The kept solids belong to one Document, the faces of the kernel keep their holes, and the next forms of five gates fail (E135 to E141, T136 to T140 of the codebase review of 2026-11-05 (b); after the next product phase). TASK-0192. | 2–3 |
| P0.85 | Each hole of a face has its owner, the STEP export refuses what a strict reader refuses, and the session of the desktop holds an open (E142 to E151 of the codebase review of 2026-11-06; after the next product phase). TASK-0197. | 2–3 |
| P0.87 | An export of the command line refuses a file that is its own document file, so the document stays (finding 1 of the review of Codex of v0.198, high; before the next product phase). TASK-0220. | 1 |
| P0.88 | An export of the command line resolves a link of each folder of its paths, and a path that is not valid gives its code (findings 1 and 2 of the review of Codex of v0.200, high and medium; before the next product phase). TASK-0221. | 1 |
| P0.89 | A void on the plane of a distant face is written, a replay and an export know an assembly, and two gates read the transitive form (E152 to E155, T145, T146 of the codebase review of 2026-11-06 (b); after the next product phase). TASK-0224. | 2–3 |
| P0.90 | The start again keeps an unsaved document, a window that does not open gives a code, and the flush holds its place (V91 to V96 of the codebase review of 2026-11-06 (b); after P0.89). TASK-0225. | 1–2 |
| P0.91 | The review gate counts each product phase, and the export, the host and three checks hold their next forms (E156, E157, T147 to T151 of the codebase review of 2026-11-06 (c); after the next product phase). TASK-0231. | 2 |
| P0.92 | The start again sees the commands of the stop, and the panel and the phone hold their next forms (V97 to V103 of the codebase review of 2026-11-06 (c); after P0.91). TASK-0232. | 2 |
| P0.93 | The smoke test of the emulator gives the cause of a start that fails: the versions of the workload, of the emulator and of the image, the output of the start, and the lines of Android about the program (the emulator job of v0.217 failed with no change of code). TASK-0235. | 1 |
| P0.94 | The key Escape goes back from the face to its body, and the button "Sketch on face" ends the arm of Face (findings 1 and 2 of the review of Codex of v0.221, medium; R12a only just shipped, and its ledger entry states the first as done). TASK-0237. | 1 |
| P0.95 | The smoke test of the emulator gives the exception of the program as public annotations: one notice for the start, no notice for the lines of the hash, and the line "ended" as the text of the failure (the emulator job of the run of main of v0.222 ended the program with an exception that no annotation showed). TASK-0239. | 1 |
| P0.96 | A save to a new name keeps a handle on the document at each moment (E162 of the codebase review of 2026-11-06 (d), high; before the next product phase). TASK-0241. | 1 |
| P0.97 | The readers of a file, the pin rule, the smoke test and the ledger hold their next forms (E158 to E161, E163, T152, T153, T155 to T159 of the codebase review of 2026-11-06 (d); after the next product phase). TASK-0242. | 2 |
| P0.98 | A cancelled finger ends, the activity tells its starts, and the face and the texts hold their next forms (V104 to V111 of the codebase review of 2026-11-06 (d); after P0.97). TASK-0243. | 2 |
| P0.99 | The activity of the phone may start again in its process: the hint of SDL that lets a later create go on, and two more changes of the configuration (V105, the cause of the failures of the emulator job; ADR-0079). TASK-0247. | 1 |
| P0.100 | A second session of the phone in its process makes its window (V112 of the codebase review of 2026-11-06 (e), high; before the next product phase). TASK-0250. | 1 |
| P0.101 | A failed delete, an edge of three faces, the ledger, two gates and the smoke test hold their next forms (E164 to E166, T160 to T164 of the codebase review of 2026-11-06 (e); after the next product phase). TASK-0251. | 1–2 |
| P0.104 | A failure of the window of the phone and a new create of its activity name their cause: the text of SDL, the lines of Android about a relaunch, and a wider wait in the test of V109 (the failed run 42313242154). TASK-0256. | 1 |
| P0.103 | The session of a destroyed activity ends before its window, and a session waits for the window of an earlier one (the failed run 42308415060 of `main`; before the next product phase). TASK-0255. | 1 |
| P0.105 | The check of an open reads the store of the assets (E167 of the codebase review of 2026-11-07, high; before the close of P9c). TASK-0262. | 1 |
| P0.106 | A failed copy, a test of the faces, the STL reader, the command line, two gates and the affects fields hold their next forms (E168 to E171, T165 to T167 of the codebase review of 2026-11-07; after P9c). TASK-0263. | 2 |
| P0.107 | The chips, the ring and the collapsed panel hold their next forms (V119 to V123 of the codebase review of 2026-11-07; after P0.106). TASK-0264. | 2 |
| P0.108 | A lost surface of a destroyed activity ends the session quietly, and the activity names its change (V124 of the codebase review of 2026-11-07, high; before the close of P9c). TASK-0265. | 1 |
| P0.109 | The wait of the phone checks its turn, the STL reader keeps the results of v0.243, and the test sheet says what the phone keeps (E172, E173 and E174 of the codebase review of 2026-11-07 (b), high; before the next product phase). TASK-0269. | 1–2 |
| P0.110 | The import of a chosen STL reaches only its new part, and an open by name ends the home screen (V125 and V126 of the codebase review of 2026-11-07 (b), high; after P0.109, before the next product phase). TASK-0270. | 1–2 |
| P0.111 | The home screen, the rows, the chips, the dialog and three comments hold their next forms (V127 to V133 of the codebase review of 2026-11-07 (b); after the next product phase). TASK-0271. | 1–2 |
| P0.112 | The phone, the reader, the APK check, the gates and the roadmap hold their next forms (E175 to E179, T168 to T173 of the codebase review of 2026-11-07 (b); after P0.111). TASK-0272. | 2 |
| P0.113 | The name of the solid keeps the results of v0.250, and the test sheet says how the size comes back (the review of Codex of v0.254, high; after R16e, before the next product phase). TASK-0274. | 1 |
| P0.114 | The tests of the home state of the desktop run in one collection, so an open of one class does not end the home screen of another (run 42371538462 on macOS). TASK-0278. | 1 |
| P0.115 | The reader gives the result of each earlier reader that accepted a file, and the ring of a sketch keeps each tool in reach (E180, E181 and V134 of the codebase review of 2026-11-07 (c), high; before the next product phase). TASK-0280. | 1–2 |
| P0.116 | The size limit, the lost surface, the taps over a window, the portrait ring and the host warnings hold their next forms (E183, E184, V135, V136 and T174 of the codebase review of 2026-11-07 (c); after the next product phase). TASK-0281. | 1–2 |
| P0.117 | The asset verb, the second ring, three comments and the dialog filter hold their next forms (E185, V137, V138 and V139 of the codebase review of 2026-11-07 (c); after P0.116). TASK-0282. | 1 |
| P0.118 | The ring of a sketch sees the windows that come after it in the frame, and a window of the sketch at the left does not pull it under the panel (two leads of the review of Codex of v0.265, medium). TASK-0284. | 1 |
| P0.119 | The platform step builds its probe in the host of the phone and reads its analyzer switches, and the proposal of K5c gives the volume of the sphere (the reviews of Codex of v0.266, medium, and v0.268, low). TASK-0285. | 1 |
| P0.120 | A surface lost while the phone engine starts ends the engine as the background does, and the document stays (the audit of Codex of the phone host at d5209a2, high; before the next product phase). TASK-0286. | 1 |
| P0.121 | Each query of a surface checks its result, and a device that fails to start frees what it made (the review of Codex of v0.271, high and medium; before the next product phase). TASK-0287. | 1 |
| P0.122 | The HTTP answer of a save lists its warnings, as W-IO-OLD-FILE-KEPT (finding E164 of the codebase review of 2026-11-06 (e); the owner, 2026-11-08). TASK-0291. | 1 |
| P0.123 | The main function of a stale session of the phone does not return, and a native crash gives its stack (V141 of the codebase review of 2026-11-08, high; before the next product phase). TASK-0294. | 1 |
| P0.124 | A solid mesh with inward triangles is refused, and a file too large for the store has its own code (E186 and E190 of the codebase review of 2026-11-08; after the next product phase). TASK-0295. | 1 |
| P0.125 | Five comments and messages, the cleanup of a failed device, the second ring and two gates hold their next forms (E187 to E189, E191, E192, V140, T175 and T176 of the codebase review of 2026-11-08; after P0.124). TASK-0296. | 1 |
| P0.102 | The cache of a face, the fixed controls, the list New, the edges and the safe area hold their next forms (V113 to V118 of the codebase review of 2026-11-06 (e); after P0.101). TASK-0252. | 1–2 |
| P0.86 | The window keeps no input between sessions, the hook has the cases of one axis, and the next forms of four gates fail (V88 to V90, T141 to T144 of the codebase review of 2026-11-06; after the next product phase). TASK-0198. | 1–2 |

## Track P — The platform

| Phase | Content | Evenings |
|---|---|---|
| P8a | The document file in the engine and the server. The command log in JSON Lines, written by `Engine.Core/Persistence/` (Q7). Each applied command goes into the file before its result, with fsync. The header records the length unit of the document, `Document.LengthUnit`, with the millimetre as the only unit for now (KD4, Q3). Each entry records the name and the version of the backend from `IGeometryBackend`, and the bus writes them (anti-objective 10, KD3, Q2). An open gives a new document identifier (Q1). A document with no name writes a temporary file from its first command, and a save moves it (Q4). The server opens the file that its command line names. ADR-0015 is written again (register entry R-0025). TASK-0091. | 3–4 |
| P8b | The container model. One document holds one part. An assembly holds references at a fixed version. | 5–8 |
| P8b1 | The kind of a document, part or assembly, and the references of an assembly to a part of the store at a fixed version, as data that the engine does not resolve; each part file keeps its bytes (the engine lane of the order of 2026-11-05). ADR-0071, TASK-0202. | 2–3 |
| P8c | Undo and redo. `Undo` and `Redo` are commands of the log, and the bus reverses the recorded body changes of a step (Q1 B to Q4 A). The keys wait in R-0041. ADR-0031, TASK-0093. | 3–5 |
| P8d | The three layers. Log, ordered feature list, regeneration cache. Two slices (Q1 A of 2026-11-03). | 5–8 |
| P8d1 | The ordered feature list: a fold of the log that the engine reads, and the query `GetFeatures` with the inputs and the state of each feature (four answers A of 2026-11-03). ADR-0054, TASK-0173. | 2–3 |
| P8d2 | The regeneration cache: a regeneration of each feature after a changed feature, with reuse of each unchanged solid, and the place of a fault (charter rule 4). One regeneration replaces the three rebuild paths, and the faults stay in the records (four answers A of 2026-11-03). ADR-0055, TASK-0174. | 3–5 |
| P8e | The unit in the numeric pad. The header of P8a holds the length unit of the document (KD4). TASK-0119. | 1 |
| P8f | The document file in the desktop and for an agent. The desktop opens the file that its command line names, and an open in the window starts the program again (Q5). "Save" moves the file to the name typed in the panel. HTTP save and open on the loopback address, in one folder (Q6, ADR-0002). ADR-0030, TASK-0092. | 2–3 |
| P8g | An STL export of the live bodies, from the surface, the command line and the desktop (ADR-0048). TASK-0157. | 1 |
| P8h | The unit kind of each number parameter in the command schema: `Unit` and `UnitBy` on `FieldSchema`, a unit on each of the 25 number parameters, and a gate. From the module-declaration proposal of the desktop redesign; the owner chose `constraint-value`, `scale` and `pixel` on 2026-11-05. ADR-0060, TASK-0186. | 1–2 |
| P9a | The demonstration "Open the same STL on desktop and Android" (the owner, 2026-11-07): ADR-0083 with the decisions D1 to D6, the mesh body, the command `ImportStl` and the capability `IMeshImportOps`. TASK-0257. | 1 |
| P9b | The STL reader of the engine, binary and text, with no arithmetic on the floats at the read; the handler, the mesh body in both backends, the codes and the refused operations; the files `bracket.stl` and `bracket-damaged.stl`; a test that opens both on Windows, Linux, macOS and the emulator. TASK-0258. | 3–5 |
| P9c | The desktop: the home screen, the file dialog of Windows with a filter for `.stl`, the frame after an open, a control "Frame", and a message with "Choose another file". The row closes after the run of the owner. TASK-0259. | 3–4 |
| P9d | The phone: the document picker of Android with no permission of storage, the same home screen, and the part kept through the background. The row closes after the run of the owner on the S23. TASK-0260. | 3–4 |
| P9e | The test sheet of the demonstration, from its page. It closes with P9c and P9d after the run of the owner. TASK-0266. | 1 |
| P9f | A start of the desktop with the setting file opens that file and shows no home screen (ADR-0083 item 7, the owner, 2026-11-08). TASK-0290. | 1 |

## Track R — The renderer

The first objective. Each phase ends with something that a person can observe.

| Phase | Content | Evenings |
|---|---|---|
| R1 | Upgrade the Vulkan binding. Move the Vulkan code into a first-party project. TASK-0027, shipped at v0.28 (ADR-0017). | 2–4 |
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
| R11a | A tap of the sketch snaps to a midpoint, a line, a curve or the grid (the codebase review of 2026-10-31, sections 5 and 6, Q2 A). TASK-0131. | 3–4 |
| R11b | The mark of the snap under the mouse, and under a finger before its tap (ADR-0041 item 6). TASK-0134. | 2–3 |
| R11c | A tap of the sketch snaps to the intersection of two lines, of a line and a curve, or of two curves (the owner, 2026-11-02). TASK-0165. | 2–3 |
| R11 | Escalated selection and a snap engine. Closed after R11c: R11a to R11c give the snaps, and the escalated selection moves to R12 (the owner, 2026-11-02). | 4–6 |
| R12 | The escalated selection of faces and edges. It needs a stable identity of each face and each edge, so it comes after K4 (ADR-0041 item 5; the owner, 2026-11-02). | 4–6 |
| R12a | A second tap on the selected body selects the face under the point: the outline of each face name in `GetFaces`, its mark in the scene, and the face in the panel with a button Sketch (ADR-0075). TASK-0236. | 2–3 |
| R12b | A tap near an edge of the selected face selects the edge: an edge is the pair of the names of its two faces, `GetFaces` gives the edges, and the panel gives the length (ADR-0077). TASK-0245. | 2–3 |
| R13 | The dimension overlay of the desktop: a toggle in the panel, the overall size of the visible bodies or of each live body, and the size and the thickness of a selected body, as lines with labels in the scene (the owner, 2026-11-03; four answers of 2026-11-04). ADR-0056, TASK-0178. | 2–4 |
| R14a | Android in the charter: objective 8 and anti-objective 16, the closure of R-0020, ADR-0061 with its amendments of ADR-0017 and ADR-0027, and the rows R14b to R14g (the Android order of the owner, 2026-11-05). TASK-0193. | 1 |
| R14b | The host owns the document session, the surface mounts on that session as an option, and an open of a file stays in the same process (`3DEngine/DesktopDocument.cs`). The behaviour of the desktop does not change, and tests prove it (ADR-0061 item 9). TASK-0194. | 2–4 |
| R14c | `ppy.SDL3-CS` replaces `Alimer.Bindings.SDL` on the desktop, with no change of behaviour and no file of 32-bit x86; the image tests stay green (ADR-0061 item 5). TASK-0195. | 2–4 |
| R14d | The native builds of Manifold and cimgui for `android-arm64` and `android-x64` on GitHub Actions, with the NDK, serial (ADR-0061 items 6 and 7). TASK-0196. | 3–6 |
| R14e | The Android host: a `net10.0-android` project, an activity that subclasses `SDLActivity`, the APK, the document picker for Open and Save, a new surface and swapchain after a loss, the pre-rotation, and a pause in the background. A box, a subtract and the cut on the S23 Ultra, and a test sheet for the owner. The row closes only after the test of the owner (ADR-0061 item 9). | 10–20 |
| R14e1 | The drawing code of the desktop becomes the library `3DEngine.App`, which an Android program can reference: it reaches the session through `ISessionHost` and the files through `IDocumentFiles`, and it references no surface and no ASP.NET Core. No change of behaviour (ADR-0072). TASK-0222. | 2–3 |
| R14e2 | The Android program: a `net10.0-android` project for `android-arm64` and `android-x64`, an activity that subclasses `SDLActivity`, a session of its own, and an APK that draws its document with `3DEngine.App` and holds the native libraries of R14d (ADR-0061, ADR-0072). TASK-0226. | 3–5 |
| R14e3 | A person cuts a body in the panel with no agent, the phone keeps its document through the background with a new engine at each return, the screen stays in portrait, and the owner has a test sheet (ADR-0073). TASK-0227. | 2–3 |
| R14f | The gate of Android: a job builds the APK at each push, a job runs the x86-64 emulator with software Vulkan and a smoke test, and the pipeline compares the hash of Android with the hashes of the desktop runners (ADR-0061 items 1 and 8). | 4–8 |
| R14f1 | The pipeline builds the APK of the phone at each push, checks its ABIs and its native libraries, and keeps the APK for the owner (ADR-0061 item 9). TASK-0228. | 1 |
| R14f2 | The APK holds its assemblies, and the pipeline starts it on the x86-64 emulator with Vulkan in software and reads its log (ADR-0061 item 9). TASK-0229. | 1–2 |
| R14f3 | The probe of the determinism hash moves into `Engine.Core`, the program of the phone runs it on the emulator, and the pipeline compares four platforms (ADR-0061 item 8, ADR-0074). TASK-0233. | 1–2 |
| R14g | The layout of the phone, inside the redesign of the desktop: each region of the redesign gives its form on the screen of the S23 Ultra (ADR-0061 item 9). | 8–15 |
| R15 | The user interface of the desktop is one partial file for each region: the panel, the overlays, the window of the sketch, the window of the images and the pad, with no change of a line of code (the fallback queue of the owner, 2026-11-05). TASK-0234. | 1 |
| R16 | The redesign of the desktop and of the phone, from the designer brief of 2026-11-05 (the "mix" direction): the theme and the fonts, then the layout slice by slice, with no change of the contract (the owner, 2026-11-06, evening). | 20–40 |
| R16a | The theme "Proposed (harmonized)" of the brief: the colours of the panels, the clear of the scene, the colours of the marks and a casing under each mark, with no change of layout (ADR-0078). TASK-0246. | 1 |
| R16b1 | The fixed controls of the redesign: the title of the document in the title bar of the panel, Undo and Redo as round anchors at the bottom left, and the status line on their row (ADR-0080). TASK-0248. | 1 |
| R16b2 | The tool ring of the redesign: the tools of the selected body that need no text are segments of a ring next to it on a landscape display, and under the thumb on a portrait display (ADR-0081). TASK-0253. | 2 |
| R16c | The values on the edges of the redesign: the values of a selected extrude that a command changes sit on the edges that they measure, and a tap opens the pad of the value (ADR-0082). TASK-0254. | 2 |
| R16d | The rows of the redesign: the name, the state and the values of the selected body in rows with no panel at the upper right of a landscape display, and a tap on a value opens its pad (ADR-0084). TASK-0267. | 1 |
| R16e | The sketch under its body: "Edit" of the sketch of a live extrude keeps the camera and shows the sketch out of the body along the normal of its plane, with a line from each point to its place (ADR-0085). TASK-0273. | 1 |
| R16f | The tools of the sketch on the ring: while a sketch is active, the ring holds Line, Select, Circle, Arc and Finish beside the sketch (ADR-0086). TASK-0275. | 1 |
| R16g | "Constrain" on the ring of a sketch: a second ring of the kinds of a constraint that fit the selection, with "Back" (ADR-0087). TASK-0276. | 1 |
| R16h | The window of a sketch beside its ring: while the ring of a sketch shows, the window holds no tool, no Finish and no kind of a constraint (ADR-0088). TASK-0277. | 1 |
| R16i | The fonts of the redesign: Instrument Sans for the interface and JetBrains Mono for a value on an edge, after the owner allowed their download (ADR-0091). TASK-0292. | 1 |

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
| K0f | A command changes the distance of a live extrude, and the extrude and each step of its chain rebuild; the desktop gives the pad of the distance (Q1 and Q2 A of the codebase review of 2026-11-01). ADR-0049, TASK-0163. | 2–3 |
| K0g | A datum on a face of an extrude or a box, a sketch on it, and the rebuild of each datum after an edit; a reference resolves at its place in the log (the eight answers of the owner of 2026-11-02). ADR-0051, TASK-0166. | 6–9 |
| K0h | The edges and the vertices of an extrude and a box as references in a sketch: the projection onto the plane of the sketch, a point with no variable, no cycle, and a fault that keeps the points (the four answers of the owner of 2026-11-02). ADR-0052, TASK-0167. | 4–6 |
| K0i | The desktop: a tap on a face starts a sketch on a datum of it, and the overlay draws a sketch on a datum of the Document (ADR-0051 item 11; the four answers of the owner of 2026-11-03). ADR-0053, TASK-0171. | 3–5 |
| K0 | A feature graph over Manifold. No B-Rep. Deferred-query references. The rest after K0f is K0g, K0h and K0i (the owner, 2026-11-02). | 12–20 |
| K1 | Exact predicates. A deterministic arithmetic layer. The 2D predicates of the profile of a sketch, a rounded filter and an exact path with integers; the 3D predicate arrives with K4 (Q1 and Q2 A of 2026-11-02). ADR-0050, TASK-0164. | 4–8 |
| K2a | The sketch model in the engine. The point, the line segment that references two points (Q3), the fixed frame of a sketch, and the five constraints of K3a. A sketch records the identifier of a datum; the three origin planes have fixed identifiers (Q4). One command for each item, with scalar fields; the Document holds one sketch record for each sketch, and an undo restores the record before; the events `sketch.created`, `sketch.changed` and `sketch.removed`, and a query `GetSketch` (Q2). The positions are a projection of the record, and the log holds the inputs only (Q6). An angle records the pair (cos θ, sin θ) (Q5). The ADR names the five entities and the fourteen constraints now (Q1). The desktop draws a sketch as lines of the UI layer, read only (Q9). ADR-0033, TASK-0100. | 4–6 |
| K2b | The sketch tools of the desktop. A sketch mode in the intent layer, a ray to the plane of the sketch, a pad for a line, and a panel of constraints (Q8). ADR-0034, TASK-0101. | 4–6 |
| K2c | The seven constraints of ADR-0033 item 3 on points and lines: pointOn, midpoint, parallel, perpendicular, equal, symmetric and angle, with their equations in the solver (the plan of 2026-10-30). TASK-0110. | 3–5 |
| K2d | The circle and the arc in the engine: two commands, the radius as a variable of the solver, the equation of an arc, and tangent, radius, pointOn and equal on a curve (the plan of 2026-10-30). The desktop waits for K2e. TASK-0117. | 4–6 |
| K2e | The desktop draws and adds circles and arcs, and the panel offers the constraints of K2d on a curve (the codebase review of 2026-10-30 (b), sections 5 and 6). TASK-0122. | 4–6 |
| K2f | The window of the sketch offers the constraints of K2c on lines: On line, Midpoint, Equal, Parallel, Perpendicular and Angle (the Scope (out) of K2e). TASK-0125. | 1–2 |
| K2g | The selection holds three items, and the window of the sketch offers Symmetric (the answer "1 3 4" of 2026-10-31). TASK-0127. | 1 |
| K3a | A constraint solver for five constraints: coincident, horizontal, vertical, distance and fixed. It uses only `+ - * / sqrt`, and its output stays outside the log (KD5). ADR-0035, TASK-0102. | 8–12 |
| K3 | The rest of the constraint solver, after K3a. Done: the fourteen kinds of ADR-0033 item 3 are in K3a (TASK-0102), K2c (TASK-0110) and K2d (TASK-0117); the owner closed the row on 2026-10-31. | 4–8 |
| K4 | Planar faces only, with a boolean. Provenance inside the kernel. In slices from K4a (the four answers of the owner of 2026-11-05). Done: K4a, K4b and K4c, each in its own row; the owner closed the row on 2026-11-05. | 35–55 |
| K4a | The exact planar subtract in the new project `Engine.Kernel`: rational solids of boxes, extrudes and moves, `Orient3D`, a binary space partition, the provenance of each face, and the B-Rep of the result, proven against Manifold. The boolean first; one face for each curve of a profile; no engine change. ADR-0057, TASK-0183. | 8–12 |
| K4b | The engine reads the kernel: the B-Rep of each body as a projection of the feature list, and a query of its faces with their names. Beside Manifold, built at the query and cached, one entry for each name, and a split face is one name with its parts (the four answers of the owner of 2026-11-05). ADR-0058, TASK-0184. | 6–10 |
| K4c | The datums and the references by the identity of a face, and a fault for a face that is gone (ADR-0051 item 8). The existing fault, a check after a cut only, a datum on a hole wall faces into the hole, a split face keeps its plane, and the turn applies to older documents (the five answers of the owner of 2026-11-05). ADR-0059, TASK-0185. | 4–6 |
| K5 | Add the cylinder, the cone, the sphere and the torus. | 55–85 |
| K5a | The cylinder: a command CreateCylinder with a radius and a height, the prism of the circle of ADR-0040 with 64 plane facets, the names +z, -z and side in the owned kernel, and a datum on the top or the bottom (the fallback queue of the owner of 2026-11-05, with the recommended answers). ADR-0076, TASK-0238. | 2–3 |
| K5b | The list "New" of the panel of the desktop and of the phone, with the box and the cylinder, and a pad of the radius and the height (ADR-0076 item 7, which amends ADR-0032 item 4). TASK-0244. | 1 |
| K5c | The cone: a command CreateCone with a radius and a height, a closed mesh of the 64-gon of ADR-0040 and an apex that a new capability ISolidMeshOps stores as a solid, and the faces -z and side (ADR-0089). TASK-0288. | 1–2 |
| K6 | STEP export for the analytic subset. | 6–12 |
| K6a | The planar B-Rep of the owned kernel of each live body as one STEP file of AP214, with the same bytes on each platform, from the surface and the command line (the engine lane of the order of 2026-11-05). ADR-0070, TASK-0200. | 1–2 |
| K6b | A void inside a body is a `BREP_WITH_VOIDS` of the smallest solid that holds it, by an exact ray (ADR-0070 item 2; the engine lane of the order of 2026-11-05). TASK-0201. | 1 |
| K7 | General extrude and revolve of a simple curve. **Optional.** Year two. | 40–65 |

## Track V — Machine vision

The first domain extension. Objective 15 puts it before materials, chemistry and biological data.

| Phase | Content | Evenings |
|---|---|---|
| V0 | A verification spike. The vision library on three platforms (ADR-0042; the owner chose it on 2026-10-31, after R11b). TASK-0145. | 1 |
| V1a | The scene draws an image on a quad with the depth of the bodies (ADR-0043). TASK-0146. | 1 |
| V1b | The engine keeps each asset under the SHA-256 of its bytes, in one store of the account (ADR-0044). TASK-0147. | 1–2 |
| V1 | A content-addressed asset store. A textured quad. Done: V1a (TASK-0146) and V1b (TASK-0147); the owner accepted the picture of V1a on 2026-10-31. | 2–3 |
| V2a | The Document holds an image on a datum, and a command calibrates it to real units (ADR-0045). TASK-0152. | 1–2 |
| V2b | The desktop places a PNG file on a datum, draws it, and calibrates it with two taps (ADR-0046). TASK-0153. | 1–2 |
| V2 | Place an image on a datum. Calibrate it to real units. Done: V2a (TASK-0152) and V2b (TASK-0153), on 2026-10-31. | 2–3 |
| V3 | A contour proposal becomes an editable sketch. Propose and accept. The desktop proposes the contour of the largest region with its own code, and an accept records a sketch (ADR-0047). TASK-0155. | 3–5 |

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
   The second codebase review of 2026-11-03 puts P0.76, which holds medium and low findings, after R13, by the rule of
   `docs/templates.md`, section 7. On 2026-11-03 the owner gave the order "merge, fix E128 and E129, then R13", so
   P0.75, which holds two medium findings, comes before R13. The owner added R13 to the plan on 2026-11-03, after
   P0.75 and before P0.76. On 2026-11-04 the owner gave the order "merge when green, then fix R-0051", so P0.77 comes
   after R13 and before P0.76.
   The codebase review of 2026-11-05 puts P0.78 and P0.79, which hold medium and low findings, after the next
   product phase, by the rule of `docs/templates.md`, section 7. Q1 of that review asks for the next product phase.
   The codebase review of 2026-11-05 (b) puts P0.82, which holds a high finding, before the next product phase, and
   P0.83 and P0.84, which hold medium and low findings, after it.
   On 2026-11-05 the owner gave the Android order and a standing order for an autonomous run until 2026-11-08. The
   next product phase is R14a, so the order is P0.82, R14a, P0.83, P0.84, then R14b to R14f. R14g comes inside the
   redesign of the desktop.
   The codebase review of 2026-11-06 puts P0.85 and P0.86, which hold medium and low findings, after the next product
   phase, K6b, by the rule of `docs/templates.md`, section 7.
   On 2026-11-05 the owner answered A to Q1, the redesign of the desktop, and then, because the redesign is still work
   in progress, gave the order "Ok": P0.78 and P0.79 now, and then K4.
   On 2026-11-05 the owner answered the four questions of K4: the boolean first, a new project, one face for each
   curve of a profile, and the references by identity in a later slice. So K4a comes first, then K4b, then K4c.
   On 2026-11-05 the owner put P8h (TASK-0186) on the task list for a later session, from the module-declaration
   proposal of the desktop redesign. It comes after K4c, unless the owner gives another order.
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
