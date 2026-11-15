# Design file — the design of assemblies and the work with an agent

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-15. Prepared by a side session, from the brief of the owner of 2026-11-15.
- Status: for discussion. It creates no task and changes no roadmap row. Section 12 gives the decisions for the owner.
- Answered on 2026-11-15, in the chat: option A for A1 and A3 to A15, and option C for A2. TASK-0394 (P0.160) and ADR-0157
  record the answers; `docs/proposals/2026-11-15-answer-sheet.md` holds them.
- State of the repository: `origin/main` at v0.375 (f7786c1).

**How to read the sources.** Four research agents read the repository and the web. A statement about the repository has a
path. I read these myself: the roadmap rows, ADR-0126, ADR-0153, the proposal of 2026-11-12, the kernel boundary and
anti-objective 11 of the charter, `Proposals.cs` (accept and stale), `Rotation.cs`, ADR-0112 items 5 to 8, `FeatureList.cs`,
the list of commands. The other repository statements come from the report of a research agent, with its paths; section 14
names them. A web source has a mark: **[R]** an agent read the page, **[S]** a search snippet only, **[M]** from memory.
Section 13 lists the sources.

---

## 1 The start: objectives, model and capabilities

### 1.1 What the charter says

- Objective 11: "Assemblies of the bodies of one part with joints, and the geometric checks of their motion, have a place in
  the roadmap after P10c" (`docs/CHARTER.md:78-80`). Objective 16: a load check of a printed part follows.
- The commitment: "A person, a script, a service and an agent each use the same commands, the same queries and the same
  event stream. No consumer has a privilege" (`CHARTER.md:20-22`). Anti-objective 7: "Never human-only-operable."
- Anti-objective 2: "A preview never enters the log or the Document." Anti-objective 3: "A query never changes state".
  Anti-objective 5: the output of a solver stays outside the log. Anti-objective 16: a replay gives the same result on each
  platform.
- The kernel boundary: five surfaces, and six operations: "extrude a sketch, revolve a sketch, boolean, transform, flat
  chamfer, section by a plane" (`CHARTER.md:161-162`). Anti-objective 11: no fillet.
- Non-goal: "Presence of many clients, and live collaboration".

These lines shape each option below. A function that only a person can use is refused. A preview lives outside the log. A
solver result does not enter the log. A check uses the exact kernel where it can.

### 1.2 The model today

| Subject | Today | Source |
|---|---|---|
| Document | Kind `part` or `assembly`, fixed at its creation. A part holds bodies, sketches, images, datums and sheets. An assembly holds part references only. | `Engine.Contracts/Document.cs:17-24` |
| Part reference | `PartReference(Id, Part, Version, X, Y, Z)`: the name of the bytes of a part file, a version, and a place with no rotation. The engine does not read the part. | `Assembly/PartReference.cs:7`; ADR-0071 item 7 |
| Body | `BodyRecord(Handle, Kind, Name, Visible, Geometry, Fault)`. No role, no joint. | `BodyRecord.cs:13-19` |
| Identity of a body | `Translate`, `Rotate` and `Subtract` consume a body and make a new one. The handle changes at each move. | Header comments of the three commands |
| Identity of a face | `DatumReference(FeatureId, Element, ItemId)`. It follows the chain of consumers to the live body. | ADR-0051 item 1, ADR-0059 item 3 |
| Frame on a part | A datum has a frame (origin, U, V, normal) and follows each `Translate` and `Rotate` of its body. | `Sketch/Datums.cs`; ADR-0112 item 7 |
| A turn | About the origin of the Document only. The kernel holds exact rationals and reads the nine doubles of a turn exactly. | ADR-0112 items 5, 8 and 9; `Engine.Kernel/Geometry.cs:4` |
| Second Document | Allowed in one runtime (R-0055, answer B of 2026-11-09). No ADR and no code yet. | ADR-0071, History |

### 1.3 What a person and an agent can do today

- **Change:** 33 commands. Primitives, sketches with constraints, `Extrude`, `Subtract`, `Translate`, `Rotate`, datums, STL
  import, sheets. No union, revolve, hole, pattern, copy, delete of a body, joint or role.
- **Read:** 9 queries. `GetBoundingBox`, `GetFaces` (area, point, normal, edges with length), `GetFaceAt`, `GetBodyInfo`,
  `GetFeatures`, `GetSketch`, `GetDatum`, `GetTessellation`, `GetPartReferences`.
- **Measure between two things:** nothing. No query gives a distance between two bodies, an overlap, a volume or a mass. The
  kernel has an exact `Volume()` (`Engine.Kernel/KernelSolid.cs:99`), but no query gives it. The dimension overlay of the
  window is presentation: "it reads the boxes of the meshes that the desktop holds and sends nothing to the engine"
  (`3DEngine.App/DimensionOverlay.cs:7-9`). So the window shows a size that an agent cannot ask for by the same path.
- **The surface of an agent:** `POST /commands`, `POST /queries`, the schema routes, the WebSocket of events, the exports,
  and the proposals. No route gives a picture. No MCP server and no guide for an agent exist. The CLI builds a new engine
  at each call, so it cannot hold a session.
- **A proposal (ADR-0092):** an agent posts a summary and 1 to 16 commands. The post checks the names and the parameters
  and runs no handler. The queue is in the host, outside the log. Only the person accepts, in the window.

---

## 2 The four families and the validation cases

| Family | What it needs most | Validation case of the owner | State of the case |
|---|---|---|---|
| F1 Modular furniture | Many flat parts, exact places, repeated modules, fixed links, an order of assembly | `Bibliotheque120.3de`: 19 bodies, a niche of five panels in the middle of modules. Also the entry hall shelf: 5 boards. | On the computer of the owner; boxes placed by `Translate`; no link between the boards. Not in the repository. |
| F2 Fixed mount with screws | Holes that agree, screws as standard parts, access for a tool, contact faces | The bracket of the lamp support on the crossbar with its two fixing points. Also `testdata/stl/bracket.stl`, an L bracket with two holes of 6 mm. | The lamp case is planned (K10). The STL bracket is a mesh: no edit. |
| F3 Moving mechanism | A joint with limits, a travel with no collision, a lock, an envelope | The tilting support of a light: arm, knob, pivot, 0 to 150 degrees, 60 mm under the crossbar. | Planned (K10, R24). |
| F4 Multi-level assembly | Sub-assemblies, reuse of one part many times, a change of one source | No case exists. Proposal: `Bibliotheque120` as modules, each a sub-assembly of panels, with one module used several times. | To confirm with the owner (decision A12). |

One real project can serve two families. The lamp support is F2 (the bracket on the crossbar) and F3 (the arm). The
bookcase is F1 and can be F4.

---

## 3 Subject 1 — The structure of the model

**The need.** A person names the parts of a product, uses one part many times, groups parts into a sub-assembly, replaces a
part by another, and changes a source with a known effect on each place of use.

**What 3DEngine covers.** Bodies with names in one part Document. A reference to a part file at a fixed version, as data
only. Nothing else: no instance, no group, no replace.

**What other tools do.**

- *Source and instance.* Onshape: an assembly holds instances; each refers to a part or to another assembly [R1]. Fusion: a
  component is a container; "Paste" gives a linked instance, "Paste New" an independent copy [R2]. FreeCAD: "Insert
  Component" places links and leaves the original outside [R3].
  → For 3DEngine: two commands are enough at first, "insert an instance of a source with a place" and "copy as a new source".
- *Rigid or flexible sub-assembly.* SolidWorks and Onshape: rigid by default, flexible for each instance [R4][R5]. Fusion is
  the reverse, with a rigid group [R6].
  → Rigid by default keeps each computation small. "Flexible" is a flag of an instance, not a second model.
- *Replace.* SolidWorks re-attaches a mate when the new part has an entity of the same name; else the person picks each
  entity again [R7]. Onshape says only that the mates are "reapplied" [R8].
  → A link that names a connector of the source survives a replace. A link that names a raw face does not. This is the
  strongest reason for named connectors (section 4).
- *A reference that evolves.* Onshape: a link points to an immutable version; nothing changes by itself; "Update" is an
  explicit action, also to an earlier version [R9]. Fusion: an out-of-date mark and "Get Latest" [R10]. SolidWorks: the
  update depends on which files are open [R11].
  → The Onshape rule is the rule of ADR-0071 ("the name of the bytes fixes the version"). An update of a reference is then one
  logged command, and a replay is the same on each platform. The SolidWorks rule is the one to avoid.
- *Design in context.* Onshape stores a frozen snapshot of the assembly in the part; an update is manual [R12].
  → A snapshot with an explicit update has no cycle (a part that depends on an assembly that depends on the part).
- *Many bodies in one part, or an assembly.* SolidWorks: multibody parts "should not replace" assemblies; motion and
  collision tools need an assembly [R13]. Onshape: design related parts together, then make instances for the motion; a
  "Group" keeps parts at their modelled places with no mate [R14]. Shapr3D: no assembly relation; "Align" places a body one
  time [S15].
  → A cheap first stage exists: bodies modelled in place, plus a group. Reuse and motion are what force real instances.

**The options.**

| Option | What it is | Good | Bad |
|---|---|---|---|
| S-A One part Document, bodies and groups | A part of a mechanism is a body. A group is a named set of bodies, which can hold groups. | ADR-0126 item 2 already decides it. Small. Serves F1, F2, F3. | No reuse: 12 equal shelves are 12 bodies with 12 histories. No F4. |
| S-B Instances inside one Document | A body can be the source of instances. An instance has a place and no geometry of its own. | Reuse and patterns (screws, shelves). One file. | A new record and a placement for each read (section 4). |
| S-C An assembly Document that resolves its references | The engine reads each part file at its fixed version in a second Document, and shows its bodies in the assembly. | True multi-level (F4). ADR-0071 has the record. | The largest work: the store, the second Document, a rotation in the reference, update and replace commands, the window. |

**Recommendation.** S-A first, then S-B, then S-C. Each stage keeps the one before it. Three rules hold from the start, so
that a later stage rewrites no log:

1. Each thing that names a part (a joint, a role, a requirement, a check result) names it by the identity of its first
   feature, resolved through the chain of consumers. A handle changes at each move; the feature identity does not. The
   datums already do this (ADR-0059 item 3).
2. A reference to another file is (source, fixed version), and only a command changes it.
3. A group is rigid by default.

---

## 4 Subject 2 — Placement and constraints

**The need.** A person puts a part at a free place fast, or at an exact place against another part. The product knows which
movements stay possible, within which limits, and says why a link cannot hold.

**What 3DEngine covers.** A gizmo that sends one `Translate` at the end of a drag. `Rotate` about the origin by the surface
only: "No tool of the window turns a body" (ADR-0112 item 10). In a sketch, a snap to a point, an intersection, a curve and
the grid (ADR-0041). No snap of a body to a body, no link, no degree of freedom.

**What other tools do.**

- *Place one time, with no stored link.* SolidWorks "Use for positioning only" moves the part where a mate puts it and adds
  no mate [R16]. Fusion "As-Built Joint" makes a joint between parts that are already in place [R17]. Shapr3D "Align" [S15].
  → "Place by a relation, one time" needs no solver, and it is the most frequent need of F1.
- *A connector.* Onshape mate connector: a local frame, implicit from a face or an edge, or explicit and owned by the part,
  so each instance has it [R18]. Fusion joint origin [R17]. SolidWorks mate reference: a named set of entities; two parts
  mate when the names agree [R19].
  → A connector is a named frame with an owner. An agent lists the connectors and says "fasten A.top to B.seat" with no pick
  of a face. A datum of 3DEngine is already a frame that follows its body.
- *One joint for each pair, or many geometric mates.* Onshape and Fusion: one mate between two connectors leaves a known set
  of freedoms: fastened 0, revolute 1, slider 1, cylindrical 2, planar 3, ball 3 [R5][R20]. SolidWorks: coincident,
  concentric, distance, angle; a pair needs two or three mates; redundant mates give warnings, and the start position
  changes the result [R16][R21].
  → With joints, "the freedoms left" is a table, which an agent reads with no solver.
- *Limits and positions.* Onshape: a minimum and a maximum for each freedom; a named position stores the values, and a value
  outside a limit fails with a message [R22]. Fusion: minimum, maximum and rest; "Drive Joints" sets the value [R23].
- *Fixed and grounded.* Onshape: one fixed base part for each assembly [R5]. FreeCAD and Fusion ground the first part
  [R3][R24].
- *Diagnosis.* SolidWorks MateXpert gives the subsets of mates that conflict and the distance or angle of the misfit [R25].
  → Give the diagnosis as data of a query: the freedoms left for each part, and for each joint "holds", "broken by how much"
  or "redundant".
- *The solver.* SolveSpace solves with a Newton method; the start position selects one of several solutions [R26]. A
  kinematic tree needs no solver: the place of a child is the place of its parent, then the connector, then the joint value;
  loops are not permitted in the tree [R27].
  → A tree uses `+ - * /` only, so it is exact and the same on each platform. A Newton result depends on its start, so a
  replay would need the result in the log, which anti-objective 5 refuses.

**The options.**

| Option | Work | Strength | What the agent can reason about | Determinism | Limit |
|---|---|---|---|---|---|
| P-A Free place only (today, plus a turn tool) | Small | No failure | The places only | Full | No intent: a change of a size leaves a gap |
| P-B Connectors, "place by a relation one time", and joints in a kinematic tree | Medium | No convergence failure can occur | All: each place is a closed formula; freedoms are a sum | Full | No closed loop (a four-bar linkage); one parent for each part |
| P-C Geometric mates with a 3D solver | Largest | Weakest (flips, redundancy) | Needs the answer of the solver | Needs the result in the log | Hard diagnosis |

**Recommendation.** P-B. The joint kinds in order: fixed, revolute, slider; then cylindrical and planar. A joint that would
close a loop is refused with a code; a solver for loops is an advanced function, and its result would be a preview, not a
log entry.

**One finding that concerns a decision of the owner (ADR-0126 item 3, answer D3).** D3 says that a move is a `Rotate`
command, and asks the ADR of P11 to study the effect on the log. The evidence that I found:

- A `Rotate` consumes the body and makes a new body with a new handle. The kernel holds exact rationals and "reads the nine
  doubles exactly" (ADR-0112 item 5). So each turn makes each coordinate a longer rational, and the nine doubles "are not
  exactly a turn". I deduce two effects of 150 moves on one body: the numbers grow and the rebuild slows, and the shape
  changes by a very small amount at each move. **I did not measure this.** The measure of K10c (150 turns) gives the fact.
- A turn about a pivot is three commands today (ADR-0112 item 9).
- A named position ("open", "closed"), an exploded view, a check of a travel and an instance each need the place of a part
  as a value that can change with no new geometry.

The alternative is a **placement**: the body keeps its geometry in its own frame, and its place in the world is data. For a
joint, the log holds the value of the joint (a cosine and a sine, or a length), and the engine computes the place from the
tree. One move is one small command, no new body, no drift. The cost: each read of a body in the world (the faces, the box,
the export, a datum) must apply the placement. That cost comes in any case with instances (S-B) and references (S-C).

This is decision A2. I report both; the owner says which one is correct.

---

## 5 Subject 3 — The check of the real assembly

**The need.** Before a print or a cut, a person knows that the parts fit, move, can be assembled and can be reached by a
tool, and knows which of these statements the software proves.

**What 3DEngine covers.** Nothing by a query. `Subtract` exists, so an overlap can be made by hand. Rows K11 (interference),
K12 (travel) and K13 (envelope) are on the roadmap.

**The three classes.** Each result of a check must say which class it is in:

- **G — guaranteed** from the nominal geometry alone.
- **D — depends on data** that a person or a standard gives: a tolerance, a material, a printer, a tool size, a screw table.
- **P — physical:** only a test on the real parts answers.

| Check | What the tools do | The technique | G | D | P | Work |
|---|---|---|---|---|---|---|
| Contact and overlap at one position | SolidWorks, Fusion, Onshape and Inventor list each overlap with its volume. "Touching" is a separate class and is off by default. Each has an "Ignore", or a class for fasteners and threads [R28][R29][R30][R31]. None has a press-fit concept. | The boolean intersection: empty is apart; a face, an edge or a point is touching; a volume is overlapping [M]. For a mesh: triangle pairs under a box tree (FCL) [R32]. | Apart, touching or overlapping, and the volume, for exact solids. For a mesh: exact for the mesh only. | Which overlaps are intended (a thread, a press fit); a volume to ignore. | Whether a press fit holds; whether faces really seat. | Basic for exact solids (it reuses the boolean). Medium for meshes. |
| Minimum gap | SolidWorks Clearance Verification lists each pair under a stated gap [R33]. Onshape Measure gives the minimum distance [R34]. | GJK for convex pairs [M]; a box tree for meshes [M]; closed forms for plane, cylinder and sphere pairs with `+ - * / sqrt` [M]. | The nominal gap of each pair, with its two points. | The gap that the process needs. | The real gap after the print. | Basic for boxes and cylinders. Medium for general convex. Advanced for any two cut solids. |
| Tolerances and fits | SolidWorks TolAnalyst gives worst-case and RSS stacks, from tolerances on the parts [R35]. | ISO 286 fits [R36]. FDM guides: "at least 0.3 mm" for moving parts (Prusa) [R37]; ±0.5 % with a floor of ±0.5 mm for desktop FDM (Hubs) [R38]. Each guide says to print a test. | Only arithmetic on given limits. | Each tolerance, the printer, the material. | The feel of the fit; the result on one printer. | Basic: a rule "gap ≥ the value of the process". Medium: a one-dimensional chain. Advanced: a 3D stack. |
| Collision over a travel | SolidWorks checks while the person drags, and can stop at a collision or at a gap [R39][R40]. No page states a guarantee. | Sampling with a bound: if the gap at a sample is more than the largest movement of a point before the next sample, the segment is free (LaValle, section 5.3.4) [R41]. Conservative advancement finds the first contact [R42]. For a straight move of a prism, the swept solid is a prism, and one boolean proves the travel [M]. | "No collision on this path", under the bound or by an exact sweep. | The path, the declared contact pairs, the margin. | Flex and play. | Basic for a straight move. Medium for sampling with a bound. Advanced for meshes with many joints. |
| Order of assembly | Fusion and SolidWorks give exploded views made by hand or with no collision check [R43][S44]. No tool that the agent read computes a checked order. | Assembly by disassembly [M]. Blocking graphs (Wilson and Latombe 1994) [S45]. The general problem is NP-complete; it is polynomial for single straight moves [R46][R47]. | For a fixed set of directions and single straight moves: a part can leave or not, and so an order if one exists in that family. | The directions, the sub-assemblies, the flexible parts. | Stability during the assembly, snap fits, a part that needs a tilt. | Medium for six directions and the screw axes. Advanced for more. |
| Insertion path and tool access | No tool that the agent read checks tool access. | A tool is a "use volume" that must be free in a state of the assembly (Wilson 1998) [R48]. A sampling planner finds a path and cannot prove that none exists [M]. | A given volume is free at a given state. A straight path is free. | The size of each tool; the state in which the screw is driven. | Room for a hand, a sight line. | Basic for a cylinder on the screw axis. Advanced for a general path. |
| Fasteners | SolidWorks Smart Fasteners reads hole features and does "not recognize holes in derived or imported bodies" [R49]. Onshape and Fusion place standard parts on holes or circular edges [R50][R51]. | ISO 273 clearance holes: M3 3.4, M4 4.5, M5 5.5, M6 6.6 in the medium series [S52]. A screw is a record (size, pitch, length, head) on an axis. | Same axis, the class of each hole from a table, the length of engagement, a seat for the head, no bottoming. | The screw tables, the rule of engagement for the material. | The strength of a printed thread, the torque. | Basic for a screw as data with a rule check. Advanced for holes of a mesh. |

**What this gives 3DEngine.**

1. A pair query answers each of the first two rows: for two bodies, the class (apart, touching, overlapping), the overlap
   volume and its box, and the minimum gap with its two points. The exact kernel answers for a pair of surfaces with a
   closed formula; Manifold answers for the rest, and the result says "for the mesh only". This is the rule of the charter
   for each operation.
2. Each tool has a list of intended contacts. In 3DEngine it is data of the Document: a joint declares its two parts as a
   contact pair, and a person or an agent declares the others. **A pivot has a gap of zero, so the travel check of
   ADR-0126 item 4 cannot pass at the pivot.** The check must exempt the declared pairs. This is a gap of rows K11 and K12.
3. A guarantee over a travel is rare: no commercial page states one. ADR-0126 item 4 already has the right rule, which is
   the bound of LaValle. For a slider, the exact sweep is cheaper and stronger.
4. No mainstream tool computes an order of assembly or a tool access. Both are advanced, and both use the same primitive:
   "move this set along this axis; does it meet another part".
5. A tolerance is always data. The cheap and honest form is a process profile in the Document ("FDM, 0.3 mm for a moving
   pair") and a result of class D.

---

## 6 Subject 4 — The work with the agent

**The need.** The agent and the person see the same model. The agent can read it, measure it, check it, change it and
propose a change, and the person stays in control.

**What 3DEngine covers.** See 1.3. The strengths: one surface, the schema at run time, the events, the proposals, an
expected version on a command (`E-CMD-VERSION-STALE`). The gaps: no measure between two things, no check, no picture, no
structure query for parts and joints, results with no unit (R-0053), no guide.

**What other tools do.**

- Zoo: the agent reads the selection, the feature tree and the parameters; it measures mass, volume and area; it runs the
  model often and takes snapshots from several views [R53][R54]. In the CLI its edits wait in a "diff queue"; `/render`
  shows a picture of the pending edits; `/accept` applies all [R55]. In the application, control is after the fact: the
  changes of the AI can be undone [R56].
- Onshape: each change makes an immutable microversion; a write carries the microversion that it read, and the server can
  refuse it when the element changed [R57][R58]. An identifier of geometry is not permanent; a call maps it to the current
  version and answers OK, SPLIT or FAILED_TO_RESOLVE [R59].
- Fusion: an official local MCP server drives the open session; its pages state no confirmation step [R60].
- Community MCP servers (FreeCAD, Onshape): each pairs "execute" with a read of the structure and either pictures or
  numbers; none has a proposal step [R61][R62].
- Research: a loop with pictures improves generated CAD by single digits (CADCodeVerify) [R63]. An agent needs errors,
  numbers and pictures; the numbers come first.

**What this gives 3DEngine.** 3DEngine already has the part that the others lack: a proposal that a person accepts. It lacks
the parts that they have: numbers and pictures. In order:

| Verb | Today | The base to add | Accelerator | Advanced |
|---|---|---|---|---|
| Read | Bodies, features, sketches, datums, faces | A structure query: parts, groups, connectors, joints, values, freedoms left, roles | One call for the structure and the measures | — |
| Measure | Box, face area, edge length | Distance between two references; volume and centre of a body; angle between two faces. In the engine, so the window and the agent use one path. | The mass with a density from the Document | — |
| Check | Nothing | Pair check; check results as records with a class G, D or P | The checks of the requirements (P13) run again after each change (P12) | Travel, envelope, order, tool access |
| Change | 33 commands | The commands of sections 3 and 4 | — | — |
| Propose | A list of commands | Section 7 | A picture route, so the agent checks its proposal before it asks the person | Several proposals in a queue |
| Learn | The schema | A guide for an agent in `docs/` | An MCP server that wraps the HTTP surface | — |

A reference that stays stable, with a stated result when it does not resolve, is the base of each row. 3DEngine has it for a
face (`W-DATUM-REFERENCE-FAILED`) and plans it for a dimension ("lost", ADR-0153 item 5). The same three answers serve
each new record: resolves, lost, or changed.

---

## 7 Subject 5 — The preview of a proposal

**The need.** Before the accept, the person sees what the proposal does: the new shape, the measures that change, the checks
that change state. The accept applies exactly what the person saw.

**What S9 covers (built, ADR-0130, ADR-0110).** The summary and the commands. For `SetSketchConstraintValue` and
`SetExtrudeDistance` only: the old value crossed out and the new value. A pending node on the history arc, and a glow on the
edges. After the accept: "Applied" and an undo of one step. ADR-0092 says it plainly: "It shows no simulated result."

**What S11 plans (not built).** "The engine runs the commands of a proposal on a second Document and gives the shape and the
state of the solver, with no change of the log" (`docs/proposals/2026-11-10-gaps-to-the-render.md:80`). The owner answered
"B until S9 is done, then A". S9 is done, so the condition holds. No ADR, no task and no row exist for S11.

**What the accept does today (I read `Engine.Api.Http/Proposals.cs:97-148`).**

- It applies the commands one after the other. It stops at the first one that the engine does not apply, and "the commands
  before it stay applied". The accept is not atomic.
- Each command is one step of undo. An undo of the whole proposal was refused at that time (ADR-0130 item 4).
- When the Document changed after the post, the proposal is marked stale, the window says "The document changed after the
  proposal", and "The accept still runs" (ADR-0092 item 4).
- Not verified: the bus compares `expectedDocumentVersion` with the version before each command, and each applied command
  adds 1. So a proposal whose commands all give the same expected version would fail at its second command.

**What other tools do.**

- *Two previews.* Onshape shows a slider from "before the feature" to "after", and a "Final" button for the whole model
  after the later steps [R64].
- *Before and after.* Onshape Compare: one colour for each side, a slider that blends them, and the two feature lists with a
  mark for each difference [R65]. SolidWorks Compare Geometry: the common volume, the added material and the removed
  material, each in a colour [S66].
- *A change of the base.* An HTTP `If-Match` write fails when the stored tag changed [R67]. A protected branch of GitHub in
  the strict mode must be up to date with its base, so the checks ran on the last base; a merge queue gives the same
  guarantee with no work of the author [R68]. Onshape merge applies the changes again on the target and warns of errors
  [R69]. Figma: all or nothing, and the updates of the main file come first [R70].
- *What makes an approval mean something.* The review covers each effect: VS Code states that a change made by a terminal
  command does not show in its review [R71]. The person sees results, not only the text of the edit (the `/render` of Zoo,
  the checks of a pull request). The thing approved is the thing applied.

**The design that I propose.**

1. **A fork.** The host makes a second Document from the first at a stated version, and applies the commands of the
   proposal to it. The first Document and its log do not change (anti-objectives 2 and 3). The fork is the second Document
   that R-0055 allows. It is the same mechanism as the check of a travel "that writes nothing", and as the read of a part
   reference: one foundation, three uses.
2. **What the dry run returns.** For each command: applied, or its code. Then four differences between the base and the
   fork:
   - the bodies: added, removed, changed, with the tessellation of each changed body for the view;
   - the sketches: the state of the solver after ("solved, 2 free"), as gap G12 of the render asks;
   - the measures: the box and the volume of each changed body, before and after;
   - the checks: each check result that changes state (a new overlap, a gap now under its rule, a requirement that fails).
3. **The inspection in the 3D view.** The new shape in dashed amber lines on the present shape (G12). A switch "before,
   after, both". An accelerator: the added material and the removed material in two colours, from two `Subtract` operations
   in the fork.
4. **The comparison.** A table "measure, before, after" and a list of the checks that changed. With P13, the requirements
   are the watch list: the table says which requirement changes state.
5. **A change of the Document between the preview and the accept.** Four options:

   | Option | What it does | Good | Bad |
   |---|---|---|---|
   | V-1 Refuse as stale | The accept fails; the agent posts again. | Simple, always correct. | The person loses a decision; a busy Document makes each proposal expire. |
   | V-2 Run again and compare | The accept runs the dry run again on the present Document. If the result is the same as the result that the person saw, it applies. If not, it shows the new preview and asks again. | The person answers again only when the answer can differ. | Needs a definition of "the same result". |
   | V-3 Merge | A three-way merge. | Most permissive. | A merge engine; Onshape itself warns of errors. |
   | V-4 Apply with no check (today) | A banner only. | Nothing to build. | The thing applied can differ from the thing seen. |

   I recommend V-2. For a log with a deterministic replay, the commands are the change and the replay is the merge. "The same
   result" is exact here: the outcome of each command, and the four differences of item 2, compared as values. A reference
   that no longer resolves is a different result by definition.
6. **An accept that is atomic.** The dry run proves that each command applies before the first one touches the Document.
   So "the commands before it stay applied" can no longer occur, except for a failure of the host.
7. **One undo for one proposal.** This needs a group of steps in the history, which ADR-0130 refused at that time. It is decision
   A10.

**The cost.** The fork needs a copy of the Document or a replay of its log. I did not measure either. A cache of one fork for
each version can serve the preview and the checks.

---

## 8 Each need, the projects that have it, and the common primitive

| Need | F1 furniture | F2 fixed mount | F3 mechanism | F4 multi-level | The common primitive |
|---|---|---|---|---|---|
| Name a part in a way that survives a move | ● | ● | ● | ● | **Part identity**: the first feature, through the chain of consumers |
| Put a part exactly against another | ● | ● | ● | ● | **Connector** (a named frame on a part) and "place by a relation" |
| Keep a link when a size changes | ● | ● | ● | ● | **Joint** of kind fixed |
| A movement with limits | ○ a door, a drawer | — | ● | ● | Joint of kind revolute or slider, with a value |
| Use one part many times | ● shelves, modules | ● screws | ○ | ● | **Instance** of a source, and a pattern |
| Group parts | ● a module | ○ | ● the moving set | ● | **Group**, rigid by default |
| Replace a part | ○ | ● another screw | ○ | ● | Named connectors; a command that replaces a source |
| Know that parts do not overlap | ● | ● | ● | ● | **Pair query**: class, volume, gap |
| Know that a gap is enough | ● a drawer | ● a clearance hole | ● a moving pair | ● | Pair query, and a **rule from the data** |
| No collision over a travel | ○ a door | — | ● | ● | Pair query on a **fork**, sampled with a bound |
| An order of assembly | ● | ● | ○ | ● | "Move a set along an axis" on a fork |
| A tool reaches a screw | ● | ● | ○ | ● | A use volume on the screw axis, and the pair query |
| Holes agree with screws | ● | ● | ● the pivot | ● | A **screw as data** on an axis, and a rule table |
| See a change before the accept | ● | ● | ● | ● | **Fork** and the four differences |
| State what is proved | ● | ● | ● | ● | **Check result** with its class G, D or P |
| Export each part to make | ● a cut list | ● | ● STL for each part | ● | **Role** of a part |

● the need is central. ○ the need occurs. — the need does not occur.

Each of the sixteen needs occurs in three families or more. So the work is not "a mechanism feature" or "a furniture
feature": it is the small set of primitives of the last column.

## 9 The foundations

**Shared foundations.** Each one serves several functions, so its design comes first.

| Foundation | It serves | Exists today |
|---|---|---|
| FD1 Part identity through the chain | Joints, roles, requirements, check results, proposals | Yes, for a face and a datum (ADR-0059). To extend to a body as a whole. |
| FD2 Connector | Placement, joints, replace, screws, the commands of an agent | Part: a datum is a frame that follows its body. |
| FD3 Placement and the kinematic tree | Joints, named positions, instances, references, exploded views, the travel | No. Decision A2. |
| FD4 Pair query (class, volume, gap) | Interference, gap rules, travel, envelope, order, tool access, screw checks, the differences of a preview | No. The boolean and the exact volume exist in the kernel. |
| FD5 Fork (a second Document) | The preview, an atomic accept, the travel that writes nothing, the order search, the read of a part reference | No. Allowed by R-0055. |
| FD6 Declared data | Roles, intended contacts, the process profile, screws, requirements | No. P13 and P14 plan a part of it. |
| FD7 Check result as a record with a class | Each check, the differences of a preview, the loop of P12 | No. |

**Functions that can advance alone.** They need no foundation above:

- the measure queries of one body (volume, centre) and between two references;
- union and intersection;
- the section view in the window;
- a turn tool in the window;
- linear and grid patterns of a body;
- the guide for an agent;
- one STL for each chosen part, with the mesh check (a part of P14).

## 10 The three levels

| Level | Content |
|---|---|
| **The base.** Without it, an assembly is a set of boxes that a person reads by eye. | FD1 to FD7 in their small form. Groups. Connectors. Place by a relation. Joints fixed, revolute and slider with limits. The pair query. Measure queries. Intended contacts. The fork with a dry run, an atomic accept and the rule V-2. Roles. |
| **Accelerators.** They save time on each project. | The preview in the 3D view with the two colours. Checks that run again after each change (P12). Requirements as the watch list (P13). Instances and patterns. "Replicate": put this screw in each hole like this one. Screws as data with the hole check. The travel check and the envelope. A picture for the agent. An MCP wrapper. One undo for one proposal. The section view. |
| **Advanced.** | The resolution of part references (S-C), with update and replace. The search of an order of assembly along the axes. Tool access volumes. A tolerance chain. A solver for closed loops. Several proposals in a queue. The load check (K14). |

## 11 The scope and the order that I propose

### 11.1 The scope

In: the base and the accelerators of section 10, for one part Document, with the lamp support and the bookcase as the two
cases. Later: the advanced level. Out, by the charter: live collaboration of many clients; a fillet; a solver result in the
log.

### 11.2 The order

The order of 2026-11-12 is K10, P11 and P13, R23, K11, K12, P14, K13, P12, R24, K14. I propose one change: the measure, the
booleans, the pair check and the preview come **before** the joints. The reasons: they need no decision on the model of a
move; the agent gets numbers at once; and the pair check at one position (K11) has no real need of a joint. This is
decision A11.

| Wave | Content | Needs | Rows that exist |
|---|---|---|---|
| 0 | K10: the lamp case, with the measure of 150 turns. Add the bookcase as a second builder. | The sizes from the owner | K10 |
| 1 | Measure queries. Union and intersection. The pair query at one position, with intended contacts. The section view. The guide for an agent. | Nothing | K11 (moved forward) |
| 2 | The fork. The dry run. The atomic accept with rule V-2. The preview in the window. | Wave 1 for the differences of measures and checks | S11 (no row yet) |
| 3 | The ADR of the move (A2). Connectors. Place by a relation. Groups. Joints. The joint in the window. | Wave 0 for the measure | P11, R23 |
| 4 | Roles, the process profile and the requirements. The travel. The envelope. The checks that run again. One STL for each part. | Waves 1 to 3 | P13, P14, K12, K13, P12 |
| 5 | The end-to-end scenario of the lamp (F2, F3). Then instances, patterns and screws as data, and the end-to-end scenario of the bookcase (F1). | Wave 4 | R24 |
| 6 | The advanced level, each on its own decision. | Wave 5 | P8b (references), K14 |

A codebase review is due after each four product milestones, so each wave has one or two reviews.

### 11.3 The next items, each in a form to code

Each card is a first form for the discussion. A card with "Contract: yes" changes the public shape of `Engine.Contracts`:
it needs an ADR and the answer of the owner before code (`CLAUDE.md`, "Stop and ask").

**N1 — Measure queries.**
- What: three queries in `Engine.Core/Queries/`.
  - `GetMassProperties(bodyId)` gives `volume` and the centre `cx, cy, cz`. The kernel has the exact volume.
  - `GetDistance(a, b)` gives `distance`, the two nearest points, and `class`: apart, touching or overlapping. `a` and `b`
    are each a body, or a face, an edge or a vertex as a `DatumReference`.
  - `GetAngle(faceA, faceB)` gives the cosine and the sine between two normals. The client computes the degrees: the
    determinism gate forbids `Atan2` in `Engine.Core`.
- Why: the agent and the window measure by one path (anti-objective 7). It is the base of each difference in a preview.
- Contract: no. Each result record is in `Engine.Core`. Open point: results with no unit (R-0053).
- First slice: `GetMassProperties`, and `GetDistance` for two bodies made of planes. Curved faces follow.
- Tests: a box 20 × 10 × 30 has the volume 6000; two boxes with a gap of 5 give 5 and the two points; two boxes that share
  a face give 0 and "touching". Injection: a distance between the boxes of the bodies, not the bodies, must fail on an L
  shape.

**N2 — `Union` and `Intersect`.**
- What: two commands in the form of `Subtract`: each consumes two bodies and makes one.
- Why: the knob with a collar (K10); the overlap volume of the pair query; the two colours of a preview.
- Contract: not verified. I did not read how `Subtract` reaches the backend (ADR-0012). If the backend interface in
  `Engine.Contracts/Geometry` needs a new member, it is a contract change.
- Tests: the union of two boxes that overlap has the volume of the two less the overlap; the intersection has the volume
  of the overlap; two boxes apart give an intersection that the command refuses with a code.

**N3 — The pair query (K11).**
- What: `CheckInterference(bodyIds?)` gives, for each pair of live bodies or of the given bodies: `class`, the overlap
  `volume`, the box of the overlap, the minimum `gap`, `intended` (true for a declared contact pair), and `exact` (false
  when Manifold answered).
- Why: "which parts touch or overlap, where, and how deep" (row K11).
- Needs: N1 and N2. Before joints exist, the list of intended pairs is a parameter of the query.
- Cost to watch: 19 bodies give 171 pairs. A first test of the boxes removes most pairs.
- Tests: the knob and the arm of the lamp case at a known place report their overlap; a seeded overlap of 0.5 mm is found;
  the clean case reports none. Injection: the query reads only the first pair.

**N4 — The fork and the dry run (S11, engine and host).**
- What: `POST /proposals/{id}/preview` makes a second Document at the present version, applies the commands of the proposal
  to it, and gives the result of section 7 item 2. An ADR records the second Document (R-0055 asks for it).
- Why: G12 of the render; an accept that applies what the person saw.
- Contract: to study. The proposal record is in `Engine.Api.Http`. The fork needs a way to copy a Document or to replay its
  log, in `Engine.Core`.
- Tests: after a preview, the version and the log of the first Document are the same; the bodies of the fork equal the
  bodies after a real accept, by their hash; a proposal whose third command fails gives its index and its code.

**N5 — The accept after a preview.**
- What: the accept runs the dry run on the present Document, then: applies each command when the result equals the stored
  result; or shows the new preview when it differs; or applies nothing when a command fails.
- Why: rule V-2 and the atomic accept.
- Tests: a rename of a body between the preview and the accept does not ask again; a change of the value that the proposal
  reads asks again; a failure applies nothing. Injection: the comparison reads only the count of the bodies.

**N6 — The preview in the window.**
- What: the changed bodies of the fork in dashed amber lines; a switch "before, after, both"; a table "measure, before,
  after"; the row "Solver after: solved, 2 free".
- Why: gap G12. Desktop first: ADR-0092 item 8 gives a proposal no place on the phone.
- Contract: no. Presentation.
- Tests: image tests in the form of `Proposal*Tests`.

**N7 — The section view.**
- What: a button "Section" puts a cut plane on an origin plane or on a datum; a pad sets its offset; the cut faces show a
  cap. No engine change.
- Why: to see a fit, a hole and the inside of a housing; to read what a check reports.
- Tests: an image test of a box with a hole, cut through the hole.

**N8 — Connectors.**
- What: a query `GetConnectors(bodyId)` gives each named datum of the body, and the implicit frames that `GetFaces` can
  give: the centre and the normal of each plane face, and the axis of each cylinder face.
- Why: the agent names a place on a part with no pick. A link by a name survives a replace.
- Contract: no for the query. A named datum on a face exists (`CreateDatum`).
- Not verified: that `GetFaces` gives the axis of a cylinder face.

**N9 — Place by a relation, one time.**
- What: a command puts a body so that one of its connectors meets a connector of another body, with a flip and an offset. It
  stores no link.
- Why: the most frequent act of F1; the snap of a body to a body in the window uses it.
- Needs: decision A2 or J1, because the result is a turn about a point that is not the origin.

**N10 — Groups.**
- What: a record `Group(Id, Name, Members)`, with members by part identity, and commands to create, change and remove it.
- Contract: yes. A new collection and its events.

**N11 — Joints (P11).**
- What: a record `Joint(Id, Kind, Parent, Child, ParentConnector, ChildConnector, Limits, Value, State)`. Kinds: fixed,
  revolute, slider. Each limit and the value of a revolute are a cosine and a sine. State: tight or loose. Commands:
  create, remove, set the value, set the state. A query gives the tree, the freedoms left for each part, and each joint
  with "holds" or "outside its limit". A joint that closes a loop is refused with a code. A joint declares its two parts
  as an intended contact.
- Contract: yes.
- Tests: the acceptance of row P11 in the proposal of 2026-11-12: a turn about an axis that is not through the origin, a
  refusal outside the limits, a refusal in the tight state, and a replay.

**N12 — Declared data.** The role of a part (P14), the intended contacts, the process profile, and the requirements (P13).
Contract: yes. One ADR can decide the common form: each is a small record that names parts by identity.

**N13 — The travel (K12).** The pair query on a fork at each sample, with the bound of ADR-0126 item 4, and with the
intended pairs exempt. For a slider: one exact sweep.

**N14 — A screw as data.** A record of a standard screw on an axis, and a check: each crossed hole is a clearance hole or a
tapped hole by a table; the length of engagement; a seat for the head. Then "Replicate". Contract: yes.

**N15 — The agent guide and a picture.** A guide in `docs/` with the loop "read, measure, propose, check". For a picture:
the SVG of a view (row P10a) is a picture that each host can give, with no renderer. A route for a rendered picture on the
desktop host can follow.

## 12 The decisions for the owner

Option A is the recommended option, unless the line says a different thing.

| # | The decision | A | B | C |
|---|---|---|---|---|
| A1 | The scope of the assembly track | This file is the base: one Document first, then instances, then part references | Keep the scope of ADR-0126 only | — |
| A2 | The model of a move. **This can change answer D3 of 2026-11-12.** | (Recommended: C.) A placement as data, and a joint value in the log | Keep D3: `Rotate` commands, with a new turn about a point | Measure 150 turns in K10 first, then decide with the fact |
| A3 | Constraints | Connectors and joints in a kinematic tree; no solver; a loop is refused | Geometric mates and a 3D solver | — |
| A4 | A connector | A named datum, plus the implicit frames from a query | A new record | — |
| A5 | Intended contacts | Data of the Document; a joint declares its pair | An ignore list in the client | — |
| A6 | A mesh body in a check | Permitted; the result says "for the mesh only" | Refused | — |
| A7 | The rule of a gap | A requirement of P13 with a process value; the result has class D | A constant in the code | — |
| A8 | The preview of a proposal | An ADR for the fork and the dry run now | Later | — |
| A9 | A Document that changed before the accept | Run again; ask again only when the result differs (V-2) | Refuse as stale (V-1) | A banner only, as today (V-4) |
| A10 | The accept | Atomic, and one undo for one proposal (amends ADR-0130 item 4) | Atomic; N steps of undo | As today |
| A11 | The order | Measure, booleans, pair check and preview before the joints | The order of 2026-11-12 | — |
| A12 | The validation cases | The lamp support (F2, F3) and `Bibliotheque120` (F1, and F4 as modules); the owner gives the sizes | The lamp support only | — |
| A13 | A picture for the agent | The SVG of a view first; a rendered picture on the desktop later | A rendered picture first | — |
| A14 | The advanced checks | After the base: an order along six axes and the screw axes; tool access by a cylinder; no general path planner | None on the roadmap | — |
| A15 | An MCP server | A guide first; a wrapper of the HTTP surface later | A wrapper now | — |

## 13 The criteria of evaluation

Each criterion applies to each family. A case passes when a test proves the line.

| # | Criterion | The measure | F1 bookcase | F2 bracket and screws | F3 lamp arm | F4 modules |
|---|---|---|---|---|---|---|
| C1 | The agent builds the case from a text brief with the commands only | The case builds; count of commands | 19 panels | Bracket, 2 screws | Arm, knob, pivot | 3 modules of one source |
| C2 | A replay is the same on each platform | The hash in continuous integration | ● | ● | ● | ● |
| C3 | Identity survives a change of a size | Each joint, connector and requirement resolves, or says "lost" | Change the width of a module | Change the spacing of the holes | Change the length of the arm | Change the source of a module |
| C4 | A seeded defect is found, and a clean case has no finding | One test for each defect | A shelf 1 mm too long | A hole 3 mm off | The knob meets the arm at 100 degrees | One instance overlaps |
| C5 | Each result states its class G, D or P | No result with no class | ● | ● | ● | ● |
| C6 | The preview is the result | Hash of the fork = hash after the accept | ● | ● | ● | ● |
| C7 | A stale proposal asks again only when the result differs | Two tests: a change with no effect, a change with an effect | ● | ● | ● | ● |
| C8 | Parity: each number that the window shows comes from a query | A search for a measure computed in the window | ● | ● | ● | ● |
| C9 | The cost stays small | The time of a full check; the growth of the log for each move | 171 pairs | 3 bodies | 150 moves | To define |
| C10 | The effort of the person | Taps to place a part exactly; one undo for one proposal | A panel on a panel | A screw in a hole | Set 40 degrees | Insert a module |
| C11 | The physical test has its own record | A requirement "to verify physically" with its result | The load of a shelf | The hold of a screw in the print | The lock under the weight | — |

## 14 The sources

Marks: [R] a research agent read the page on 2026-11-15; [S] a search snippet only; [M] from memory, no page read. I did not
open the web pages myself. Check a page before you quote it.

Structure and placement:

- [R1] Onshape, insert parts and assemblies: https://cad.onshape.com/help/Content/Assembly/insert_parts_and_assemblies.htm
- [R2] Fusion, components: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/ASM-COMPONENTS.htm
- [R3] FreeCAD Assembly workbench, a mirror of the wiki: https://reqrefusion.github.io/FreeCAD-Documentation-html/wiki/Assembly_Workbench.html
- [R4] SolidWorks, flexible sub-assemblies: https://help.solidworks.com/2024/English/solidworks/sldworks/c_Flexible_Sub-Assemblies.htm
- [R5] Onshape, mates: https://cad.onshape.com/help/Content/mate.htm
- [R6] Fusion, rigid group: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/ASM-RIGID-GROUP.htm
- [R7] SolidWorks, replace components: https://help.solidworks.com/2022/english/Solidworks/sldworks/HIDD_DVE_REPLACE_COMPS.htm
- [R8] Onshape, replace instance: https://cad.onshape.com/help/Content/Assembly/replace_instance.htm
- [R9] Onshape, linking documents and updating references: https://cad.onshape.com/help/Content/Document/linking_documents.htm and https://cad.onshape.com/help/Content/Assembly/updating_references.htm
- [R10] Fusion, update an assembly: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/ASM-UPDATE-ASSEMBLY.htm
- [R11] SolidWorks, the update path: https://help.solidworks.com/2018/English/SolidWorks/sldworks/c_Understanding_the_Update_Path.htm
- [R12] Onshape, modeling in context: https://cad.onshape.com/help/Content/Assembly/modeling_in_context.htm
- [R13] SolidWorks, multibody parts and assemblies: https://help.solidworks.com/2022/english/SolidWorks/sldworks/c_multibody_parts_vs_assemblies.htm
- [R14] Onshape, group: https://cad.onshape.com/help/Content/mategroup.htm
- [S15] Shapr3D, assemblies: https://support.shapr3d.com/hc/en-us/articles/7874501326108-Assemblies
- [R16] SolidWorks, questions on mates: https://help.solidworks.com/2022/english/SolidWorks/sldworks/c_faq_mates.htm
- [R17] Fusion, joints: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/ASM-JOINTS.htm
- [R18] Onshape, mate connector: https://cad.onshape.com/help/Content/mateconnector.htm
- [R19] SolidWorks, mate references: https://help.solidworks.com/2022/English/SolidWorks/sldworks/c_Mate_References_Overview_SWassy.htm
- [R20] Fusion, joint types: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/GUID-8818AE31-958A-4A59-989B-9875A174C67A.htm
- [R21] SolidWorks, standard mates: https://help.solidworks.com/2020/English/SolidWorks/sldworks/c_Standard_Mates_SWassy.htm
- [R22] Onshape, named positions: https://cad.onshape.com/help/Content/Assembly/named_positions.htm
- [R23] Fusion, joint limits: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/GUID-543C82D9-F1E8-42B3-9C34-31BB108AFAA3.htm
- [R24] Fusion, ground to parent: https://help.autodesk.com/cloudhelp/ENU/Fusion-Assemble/files/ASM-GROUND-TO-PARENT.htm
- [R25] SolidWorks, MateXpert: https://help.solidworks.com/2017/english/SolidWorks/sldworks/HIDD_DVE_Mate_Diagnostics.htm
- [R26] SolveSpace, technology: https://solvespace.com/tech.pl
- [R27] MuJoCo, overview: https://mujoco.readthedocs.io/en/stable/overview.html

Verification:

- [R28] SolidWorks, interference detection: https://help.solidworks.com/2024/english/SolidWorks/sldworks/HIDD_DVE_INTERFERENCE.htm
- [R29] Fusion, interference: https://help.autodesk.com/cloudhelp/ENU/Fusion-Model/files/GUID-A6629113-B137-4EDE-B3B2-C9F1F3BA83D1.htm
- [R30] Onshape, interference detection: https://cad.onshape.com/help/Content/View/interference_detection.htm
- [R31] Inventor, analyze interference: https://help.autodesk.com/cloudhelp/2024/ENU/Inventor-Help/files/GUID-33B4E343-12DF-48F0-8E00-A72DDFDBA778.htm
- [R32] FCL, the Flexible Collision Library: https://github.com/flexible-collision-library/fcl
- [R33] SolidWorks, clearance verification: https://help.solidworks.com/2017/English/SolidWorks/sldworks/HIDD_DVE_CLEARANCE.htm
- [R34] Onshape, measure: https://cad.onshape.com/help/Content/View/measure_tool.htm
- [R35] SolidWorks, TolAnalyst: https://help.solidworks.com/2022/English/SolidWorks/tolanalyst/c_TolAnalyst_Overview.htm
- [R36] ISO 286 preferred fits, a table: https://amesweb.info/fits-tolerances/preferred-tolerances-table.aspx
- [R37] Prusa, modeling for 3D printing: https://help.prusa3d.com/article/modeling-with-3d-printing-in-mind_164135
- [R38] Hubs, dimensional accuracy of printed parts: https://www.hubs.com/knowledge-base/dimensional-accuracy-3d-printed-parts/
- [R39] SolidWorks, collision detection: https://help.solidworks.com/2017/English/SolidWorks/sldworks/t_Collision_Detection_SWassy.htm
- [R40] SolidWorks, dynamic clearance: https://help.solidworks.com/2018/English/SolidWorks/sldworks/t_Dynamic_Clearance_SWassy.htm
- [R41] LaValle, "Planning Algorithms", 2006, section 5.3.4: https://lavalle.pl/planning/node217.html
- [R42] Zhang, Redon, Lee, Kim, "Continuous collision detection for articulated models using Taylor models and temporal culling", 2007 (abstract): https://graphics.ewha.ac.kr/CATCH
- [R43] Fusion, auto explode: https://help.autodesk.com/cloudhelp/ENU/Fusion-Animate/files/ANI-EXPLODE-ALL-LEVELS.htm
- [S44] SolidWorks, exploded views: https://help.solidworks.com/2020/English/WhatsNew/r_explode_views_auto_space.htm
- [S45] Wilson, Latombe, "Geometric reasoning about mechanical assembly", Artificial Intelligence 71(2), 1994, DOI 10.1016/0004-3702(94)90048-5
- [R46] Kavraki, Latombe, Wilson, "On the complexity of assembly partitioning", 1993 (abstract): https://kavrakilab.org/publications/kavraki-latombe1993on-complexity-of.html
- [R47] Wilson, Kavraki, Latombe, Lozano-Pérez, "Two-handed assembly sequencing", 1995 (abstract): https://kavrakilab.org/publications/wilson-kavraki1995two-handed-assembly-sequencing.html
- [R48] Wilson, "Geometric reasoning about assembly tools", 1998 (abstract): https://www.osti.gov/biblio/434422
- [R49] SolidWorks, Smart Fasteners: https://help.solidworks.com/2017/English/SolidWorks/sldworks/c_Smart_Fasteners_Overview.htm
- [R50] Onshape, standard content: https://cad.onshape.com/help/Content/Assembly/standard_content.htm
- [R51] Fusion, insert fastener: https://help.autodesk.com/cloudhelp/ENU/Fusion-Model/files/SLD-REF-INSERT-FASTENER.htm
- [S52] ISO 273 clearance holes, from secondary charts. The standard is not free. Check each value before use.

The agent and the preview:

- [R53] Zoo, Zookeeper: https://zoo.dev/docs/zoo-design-studio/zookeeper
- [R54] Zoo, research page: https://zoo.dev/research/zookeeper
- [R55] Zoo, the CLI copilot: https://zoo.dev/docs/developer-tools/cli/manual/zoo_ml_kcl_copilot
- [R56] Zoo, the AI features: https://zoo.dev/docs/zoo-design-studio/features/ml-ai
- [R57] Onshape, the architecture of the API: https://onshape-public.github.io/docs/api-intro/architecture
- [R58] Onshape, `sourceMicroversion`: https://onshape-public.github.io/java-client/com/onshape/api/requests/AssembliesAddFeatureRequest.html
- [R59] Onshape, associativity: https://onshape-public.github.io/docs/api-adv/associativity
- [R60] Fusion, the MCP server: https://help.autodesk.com/view/fusion360/ENU/?guid=FMCP-OVERVIEW
- [R61] FreeCAD MCP, a community project: https://github.com/neka-nat/freecad-mcp/blob/main/docs/tools.md
- [R62] Onshape MCP, a community project: https://github.com/hedless/onshape-mcp
- [R63] CADCodeVerify: https://arxiv.org/abs/2410.05340
- [R64] Onshape, feature tools: https://cad.onshape.com/help/Content/PartStudio/feature_tools.htm
- [R65] Onshape, compare: https://cad.onshape.com/help/Content/Document/compare.htm
- [S66] SolidWorks, Compare Geometry: the help pages gave the navigation only.
- [R67] MDN, `If-Match`: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/If-Match
- [R68] GitHub, protected branches: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches
- [R69] Onshape, merging: https://cad.onshape.com/help/Content/Document/merging.htm
- [R70] Figma, branch review: https://help.figma.com/hc/en-us/articles/5691189138839/
- [R71] VS Code, review of the edits of an agent: https://code.visualstudio.com/docs/copilot/chat/review-code-edits

## 15 Not verified

- **The web sources.** Research agents read them; I did not. A page marked [R] was read through a summary, which can hold
  an error. The Shapr3D page, the ISO 273 values and the SolidWorks compare page are snippets only.
- **The growth of the numbers under many `Rotate` commands (section 4).** It is a deduction from ADR-0112 item 5 and from
  the type `Vector3R(Rational, Rational, Rational)`. I did not read whether the kernel rounds a vertex after a turn, and I
  ran no test. Decision A2 depends on it.
- **A proposal whose commands all hold the same expected version fails at its second command.** A deduction from
  `Proposals.cs` and `CommandBus.cs`; no test read.
- **Repository statements from the report of a research agent, not read by me:** the objectives and anti-objectives other
  than the kernel boundary and anti-objective 11; ADR-0071; ADR-0092 and its amendments; ADR-0130; ADR-0056; ADR-0059; the
  list of the routes; the result records of the queries; the register entries; the test counts.
- **The files of the owner.** `Bibliotheque120.3de` and the entry hall shelf are not in the repository. The count of 19
  bodies comes from `docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`.
- **The cost of a fork** (a copy of a Document or a replay of its log), and the cost of a pair query on 171 pairs.
- **How `Subtract` reaches the backend**, and so whether `Union` and `Intersect` change a contract.
- **That `GetFaces` gives the axis of a cylinder face** (card N8).
- I did not run a build or a test. This file changes no code.
