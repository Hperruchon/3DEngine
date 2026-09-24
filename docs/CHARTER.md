# Charter

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

It replaces the charter of 2026-06-13. That charter gave the mission as "We do not build a 3D app".
That sentence had a correct intent and incorrect words. This document gives the intent.

## Mission

We do not build one application. We build the parts that many applications use.

Each part is deterministic and observable. Each change to design truth uses a command.

The engine is the only authority over **design truth**. Design truth is an ordered command log that
a replay can reproduce. Each other state is a projection of that log: a geometry cache, a render
scene, an event stream, a snapshot. The loss of a projection is a recovery, not a loss of data.

Three commitments make the mission operate:

- **One surface for each consumer.** A person, a script, a service and an agent each use the same
  commands, the same queries and the same event stream. No consumer has a privilege. Control without
  a user interface can never decay.
- **Two kernels, never joined.** Design truth lives in `Engine.*`. Render state lives in
  `3DEngine.Core`. They are peers. A host that draws owns the projection from one to the other.
  Therefore a camera, a light and a material never enter the design boundary.
- **The surface describes itself.** The engine publishes its own schema, therefore a dynamic client
  builds against the engine while it runs. Geometry sits behind opaque handles and capability
  interfaces, therefore a backend changes by replay and no client contract changes.

## The long objective

The platform serves engineering work and scientific work. It is extensible.

The first objective is a 3D engine with a Vulkan renderer, on Windows, Linux and macOS. A person
must be able to understand it. Vulkan is a technical direction and a learning objective at the same
time.

Editable parametric CAD is an objective. Each operation works by touch and by mouse. Neither method
is secondary.

Each domain keeps its own model. The platform does not put CAD, electrical diagrams, chemistry and
biological data in one geometry structure. A domain object gives its own data, and it gives one or
more visual representations. The visual representation is derived. It is never the authority.

## The twenty objectives

The owner confirmed each objective on 2026-09-20, in step 1 of the review that TASK-0015 records.
No file held the list until TASK-0029 recorded it. A document cites an objective by its number.
`Engine.Tests/Governance/ObjectiveReferenceGateTests.cs` fails when a cited number does not exist.

A change to an objective needs a decision from the owner. See `CLAUDE.md`, section "Workflow".

### The purpose

1. This is a personal project. It is separate from work for an employer. It is separate from each
   3D-printing application.
2. The project builds a platform for engineering work and scientific work. The platform is
   extensible.
3. The solution gives each tool that a 3D application needs. The solution is not one 3D
   application. A "3D app" is one product. This platform is the set of parts.
4. Each domain keeps its own model. The project does not put CAD, electrical diagrams, chemistry and
   biological data in one geometry structure.

### The first objective

5. The first objective is a cross-platform 3D engine with a Vulkan renderer. A person must be able
   to understand it. It becomes the foundation for the platform.
6. The first demonstration creates a box, moves a second box, subtracts it, and shows the cut solid
   in the viewport.
7. Vulkan is a technical direction and a learning objective. Both apply at the same time.

### The hard requirements

8. Windows, Linux and macOS each work. A gate verifies each one continuously.
9. Each operation works by touch. Each operation also works by mouse and keyboard. Neither method is
   secondary.
10. Learning and usefulness are both objectives. One does not have priority.
11. Editable parametric CAD is an objective, not an option.
12. Time is irregular and limited. Each milestone must end in one session. A person must be able to
    continue it after three weeks.

### The long-term directions

13. Scenes become very large.
14. Materials, chemistry and DNA are directions. They have no dataset and no workflow now.
15. Machine vision comes before materials, chemistry and DNA.
16. Simulation is a long-term ambition. Examples are airflow over a car and molecular data.

### The shape of the work

17. The engine keeps its current shape. The command log, the event stream and replay determinism
    stay.
18. 2D drawing, electrical schematics and technical drawing stay directions. They have no date.
19. C# and the .NET platform are the default for the engine. Another language is acceptable when
    learning that language is also a goal.
20. The project stays open source.

## Target consumers

No consumer holds business logic. Each consumer turns input into commands and observes events.

- **Agents and automation** — first class. An agent reads the schema while the engine runs, sends
  commands, and subscribes to the event stream. It needs no human interface.
- **`Engine.Cli`** — the approved embedded host and the approved test client. One client, no state
  between invocations, JSON in and JSON out. If a capability does not work here, it is not built.
- **`Engine.Api.Http`** — the approved deployment process. Its lifecycle is independent of each
  client. It binds to localhost only.
- **Hosts that draw** — the desktop host now, and other hosts later. Each one references both
  kernels and projects events into render state. Each one owns ephemeral state only: the camera, the
  selection, the hover.
- **Command handlers and query handlers** — they reach geometry only through a capability. They read
  their parameters, the current Document and the active backend.
- **Contributors, human and agent** — the reader of this charter. Use the scope test below. Read
  `CLAUDE.md` for position and `docs/working-agreement.md` for behaviour.

## What exists

V1 and V1.x are complete. `docs/CURRENT-STATE.md` is the authority for the question "does X exist".
Do not repeat that list here.

The properties below hold today:

- Mutation has one authoritative path. A read can never become a second path.
- A replay is deterministic. Each projection is reproducible from the log.
- The event stream is a faithful record. A slow consumer cannot stop the authority.
- The surface describes itself. A command schema and a query schema are projections of the
  declaration in each handler.
- A geometry capability proves the opaque-handle design from end to end, without a user interface.
- Continuous integration holds each boundary rule, therefore the rules are not conventions only.

## Direction

The roadmap holds each track and each phase. `docs/roadmap.md` is the authority. The tracks are:

- **Track P — the platform.** Persistence, the container model, undo and redo, the feature list.
- **Track R — the renderer.** A tessellation capability, then a Vulkan pipeline, then geometry from
  the engine, then an editor.
- **Track K — the kernel.** A feature graph over Manifold, then an owned analytic geometry kernel
  with a closed feature boundary.
- **Track V — machine vision.** The first domain extension.

## The kernel boundary

The project owns a geometry kernel. Its purpose is **identity**, not computation.

Manifold computes each boolean, each transform and each mesh. The owned kernel holds the topology
and gives each face, each edge and each vertex a stable identity with its provenance. A kernel that
reports provenance is a kernel that can support a parametric feature. Manifold cannot report
provenance, therefore Manifold supports geometry and not identity.

The feature boundary is closed. A change needs an ADR.

- **Surfaces, five:** plane, cylinder, cone, sphere, torus. Each one is analytic and exact.
- **Curves, three:** line, circle, ellipse. A fourth type records an approximate intersection, and
  it declares that it is approximate.
- **Topology, seven:** vertex, edge, coedge, loop, face, shell, solid.
- **Operations, six:** extrude a sketch, revolve a sketch, boolean, transform, flat chamfer, section
  by a plane.
- **Tolerance:** one global length value and one chord value for tessellation.

A pair of surfaces with a closed formula computes exactly. Each other pair refuses, and the
operation falls back to Manifold. A kernel that can refuse is a kernel that one person finishes.

## Non-goals — deferred

A non-goal is work that the project may do later. It arrives with its own ADR and its own task. One
item is on each line, therefore each item has its own state.

Do not build one of these under a current task.

- Multi-Document in one runtime.
- Exact booleans with exact arithmetic.
- Saved views.
- Presence of many clients, and live collaboration.
- Event filtering.
- A bind to an address other than localhost.
- An authentication design.
- An event schema from a registry.
- 2D drawing, technical drawing and electrical schematics.
- Materials, chemistry and biological data.
- Simulation and analysis.
- Assemblies. These arrive after the reference model operates.
- Mesh shaders, and culling on the graphics processor.
- A renderer that uses WebGPU, for a web client.

This list is an example, not a complete list. If a request is clearly later work and it is absent
here, treat it as a non-goal.

## Anti-objectives — refused at each version

An anti-objective is work that the project refuses at each version. These two lists are different,
and you must not join them.

### The log

1. **No second source of truth.** No client owns Document state, and no client keeps a parallel copy
   that it treats as correct. *Test:* a second source of truth is client state that, after a
   difference appears, the client treats as correct. A read-only projection that the client discards
   and builds again is not one.
2. **Never bypass the CommandBus. No business logic in a client.** No state that can enter the saved
   document arrives by a path other than a command on the log. An interactive tool keeps its preview
   in the client, and it sends one command when the person releases the control (ADR-0007). A preview
   never enters the log or the Document. Do not implement "save" as "write the memory to a
   file". Implement it as "write the log, and write an optional checkpoint that the log produces".
3. **A query never changes state, never writes to the log, never replays and never streams.**
4. **A command lands completely, or it does not land.** This rule applies to the log and to the
   Document. **It does not apply to a rebuild.** A rebuild is a projection. A feature in a rebuild is
   correct, failed, suppressed, or has an unresolved reference. A failed feature stays in the feature
   list, and the features after it continue to evaluate. An error is a fact about a rebuild,
   therefore an error lives in the cache and never in the log.
5. **Nothing enters the log that a replay cannot reproduce.** A result, a field dataset, the output
   of a solver, the output of an inference and derived geometry each stay outside the log. The log
   records that a job produced an artifact. A replay rebuilds the reference, never the bytes.

### The boundaries

6. **The two kernels never reference each other, and the kernel knows nothing about topology.**
7. **Never human-only-operable.** Automation drives each capability through commands, queries and
   the stream. This rule also gives the test for touch: record a session by touch, record the same
   session by mouse, and compare the two logs.
8. **No privileged client lane.** A slow subscriber can never stop the authority.

### Geometry

9. **No lowest-common-denominator geometry.** Refuse a universal body type, an automatic conversion
   between a mesh and a B-Rep, a geometry object on the wire, and a silent fallback when a capability
   is absent. *Test:* a type that forces unlike geometries into one shape trips this rule. A typed
   capability does not.
10. **A command records the backend that performed the operation.** A replay uses the recorded
    backend. Capability negotiation never selects a backend during a replay.

### The kernel boundary

Each refusal below is a measured wall, not a preference. The measurements are in the sizing research.

11. **No fillet and no chamfer on an edge chain.** A fillet creates tangency, and tangency is where
    the arithmetic stops converging. SolveSpace refused this for seventeen years. OCCT spends 96,234
    lines on it.
12. **No general NURBS surface.** No knot vector, and no arbitrary degree.
13. **No shape healing, and no tolerance on an entity.** A tolerance for each entity starts shape
    healing, and shape healing is 93,110 lines in OCCT.
14. **No general surface intersector, and no exact STEP import.** Each imported file is invalid, and
    an exact import needs shape healing.

### What we do not write

15. **No solver for fluid dynamics. No mesh generator. No engine for molecular dynamics.** Each one
    is the work of many hundreds of people, and each one exists as free software. A small program
    that teaches you a method is correct. A program that the engine depends on is not.

### Determinism

16. **A replay gives the same result on each supported platform.** The platforms are Windows, Linux
    and macOS, on x64 and arm64. `CLAUDE.md` holds the operational rules that keep this property.

## How an agent uses this charter

Before you act on a request, use this test in order. The order is important. Classify the request by
its effect on design truth, not by the word that the person used.

1. **Anti-objective — refuse.** Does the request need something in the list above? If yes, refuse
   it. Give the number of the anti-objective and its reason. Then propose the correct alternative,
   which routes the work through a command, a query or an event.
2. **Non-goal — defer.** Is the request later work, in the list or absent from it? If yes, do not
   build it. Point to `docs/roadmap.md`. The request needs its own ADR and its own task. If the
   person wants it now, the product is an ADR proposal and not code.
3. **Already exists, then in scope — proceed.** First confirm in `docs/CURRENT-STATE.md` that the
   thing is absent. Then proceed only if each condition holds: the request extends the surface and
   removes nothing; it breaks no anti-objective and crosses into no non-goal; and it advances a
   current task or an approved ADR. A request in the shape of a command is not sufficient. The
   project must have decided to do the work.
4. **If none of these apply — stop and ask.** Stop if the request changes the public shape of
   `Engine.Contracts`, replay determinism, event order or the authority boundary. Stop if two ADRs
   disagree. Stop if steps 1 to 3 do not apply.

   The roadmap holds a track for each approved objective. Therefore the absence of a track is a stop
   signal. In that condition the product is a roadmap entry and an ADR, not code.

Rule of thumb: **the charter says if you can act. `CLAUDE.md` says where. The relevant ADR says how.
`CURRENT-STATE.md` says what exists. `docs/working-agreement.md` says how to behave.** Read in that
order, and read only the one ADR that applies.
