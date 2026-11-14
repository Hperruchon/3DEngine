# Architectural Decision Records

This index is the entry point for ADRs. Read the ones that the field `governed-by` of your task
names. Do not read all of them.

## Status legend

- **Accepted** — in force. The text agrees with the code.
- **Amended** — a later ADR gives the reason for a change, see "Amended by". This record holds the current decision.
- **Superseded** — replaced by a later ADR; do not apply. The record is in the archive.
- **Proposed** — not yet accepted; informational.
- **Withdrawn** — the author took a proposal back, or the owner took back a decision that no later ADR replaces; do not apply. The record is in the archive.
- **Rejected** — the project examined the proposal and refused it; do not apply.

## Index

| # | Title | Status | Topic | Amends | Amended by |
|---|---|---|---|---|---|
| [0001](0001-kernel-abstraction-capabilities.md) | Geometry kernel abstraction by capabilities | Accepted | Boundary, geometry | — | — |
| [0002](0002-headless-first-cli-as-canonical-client.md) | Headless-first; CLI as canonical client | Accepted | Workflow, clients | — | — |
| [0004](0004-engine-runtime-is-authority.md) | Engine Runtime is the authoritative controller | Accepted | Boundary, authority | — | — |
| [0005](0005-event-stream-and-replay.md) | Event stream and replay protocol | Accepted | Contracts, observability | — | — |
| [0006](0006-command-execution-model.md) | Command execution model | Amended | Contracts, runtime | — | 0008, 0020, 0031 |
| [0007](0007-ui-ephemeral-state-boundary.md) | UI ephemeral state boundary | Accepted | Boundary, clients | — | — |
| [0008](0008-command-query-event-triad.md) | Command, query and event triad with structured results | Amended | Contracts | 0006 | 0020 |
| [0009](0009-3dengine-core-peer-render-kernel.md) | 3DEngine.Core is a peer render kernel | Amended | Boundary, clients | — | 0022, 0025 |
| [0010](0010-subscription-reset-snapshot-format.md) | subscription.reset snapshot format | Amended | Contracts, observability | — | 0020, 0015 |
| [0012](0012-geometry-backend-wiring.md) | Geometry backend wiring | Amended | Boundary, geometry, contracts | — | 0018, 0021, 0028, 0015, 0033, 0037, 0045, 0051, 0071, 0112 |
| [0013](0013-command-query-schema-declaration.md) | Command and query schema declaration | Amended | Contracts, observability | — | 0060 |
| [0014](0014-manifold-native-interop.md) | Manifold backend native-interop posture | Accepted | Boundary, geometry, native-interop | — | — |
| [0015](0015-command-log-persistence.md) | The document file | Amended | Persistence, contracts | 0010, 0012 | 0071 |
| [0016](0016-handler-declared-construction.md) | Handler-declared command and query construction | Accepted | Contracts, clients | — | — |
| [0017](0017-first-party-vulkan-layer.md) | The first-party Vulkan layer | Amended | Boundary, rendering | — | 0022, 0025, 0061 |
| [0018](0018-tessellation-capability.md) | The tessellation capability | Accepted | Geometry, contracts, rendering | 0012 | — |
| [0019](0019-hybrid-deployment-topology.md) | The hybrid deployment topology | Amended | Clients, deployment | — | 0024 |
| [0020](0020-document-version-meaning.md) | The meaning of the document version | Accepted | Contracts, runtime, observability | 0006, 0008, 0010 | — |
| [0021](0021-operand-consumption.md) | Operand consumption and the live body set | Amended | Contracts, geometry, observability | 0012 | 0028, 0031, 0033, 0037 |
| [0022](0022-first-render-pipeline.md) | The first render pipeline | Amended | Rendering, boundary | 0009, 0017 | 0025 |
| [0023](0023-camera-depth-and-draw-list.md) | The camera, the depth and the draw list of the renderer | Amended | Rendering | — | 0024, 0026, 0096, 0137 |
| [0024](0024-in-process-events-and-body-origin.md) | The events of a session in its host, and the origin of a body in double precision | Accepted | Rendering, clients | 0019, 0023 | — |
| [0025](0025-observe-a-cut.md) | The first demonstration, a device with no window, and an image test | Amended | Rendering, boundary | 0009, 0017, 0022 | 0029, 0094 |
| [0026](0026-intent-layer.md) | Device-tagged pointer samples, an intent layer, and a parity test of touch and mouse | Amended | Rendering, clients | 0023 | 0027, 0029, 0032, 0034, 0038, 0041 |
| [0027](0027-text-and-panels.md) | Text and panels through Dear ImGui, drawn by a first-party renderer | Amended | Rendering, clients | 0026 | 0029, 0061, 0072, 0091 |
| [0028](0028-body-name-and-visibility.md) | The name and the visibility of a body | Amended | Contracts, geometry | 0012, 0021 | 0037, 0049 |
| [0029](0029-selection-and-hide-gestures.md) | The selection, the hide gestures and the frame of the camera in the desktop | Amended | Rendering, clients | 0025, 0026, 0027 | 0034, 0053, 0075, 0109, 0124 |
| [0030](0030-document-file-in-the-desktop.md) | The document file in the desktop and for an agent | Amended | Persistence, deployment | — | 0061, 0071, 0080, 0090, 0102 |
| [0031](0031-undo-and-redo.md) | Undo and redo as commands of the log | Amended | Contracts, runtime | 0006, 0021 | 0032, 0033, 0037, 0045, 0051, 0071, 0080 |
| [0032](0032-numeric-pad-and-evaluator.md) | The numeric pad, the expression evaluator and the keys of undo and redo | Amended | Clients, rendering | 0026, 0031 | 0034, 0076, 0098 |
| [0033](0033-sketch-model.md) | The sketch model in the engine | Amended | Contracts, sketch | 0012, 0021, 0031 | 0035, 0045, 0051, 0052 |
| [0034](0034-sketch-tools.md) | The sketch tools of the desktop | Amended | Clients, sketch | 0026, 0029, 0032 | 0041, 0053, 0084, 0085, 0088, 0096, 0097 |
| [0035](0035-sketch-solver.md) | The constraint solver of a sketch | Amended | Sketch, determinism | 0033 | 0052 |
| [0036](0036-extrude-of-a-sketch-profile.md) | The extrude of a closed profile of a sketch | Amended | Contracts, geometry, sketch | — | 0037, 0040, 0050, 0051, 0052 |
| [0037](0037-rebuild-of-an-extrude.md) | The rebuild of an extrude after an edit of its sketch | Amended | Contracts, geometry, sketch | 0012, 0021, 0028, 0031, 0036 | 0039, 0049, 0055 |
| [0038](0038-translate-gizmo.md) | The translate gizmo of the desktop | Accepted | Rendering, clients | 0026 | — |
| [0039](0039-chain-of-a-consumed-extrude.md) | The rebuild of the chain of a consumed extrude | Amended | Geometry, sketch | 0037 | 0049, 0051, 0054, 0055, 0112 |
| [0040](0040-extrude-of-curves.md) | The extrude of a profile with arcs and circles | Amended | Geometry, sketch | 0036 | 0050 |
| [0041](0041-snap-of-a-sketch-tap.md) | The snap of a tap of the sketch | Accepted | Clients, sketch | 0026, 0034 | — |
| [0042](0042-vision-library.md) | The vision library and its place | Amended | Vision, boundary | — | 0047, 0103 |
| [0043](0043-textured-quad.md) | An image on a flat rectangle of the scene | Accepted | Rendering, vision | — | — |
| [0044](0044-asset-store.md) | The store of the assets of an account | Amended | Vision, persistence | — | 0083 |
| [0045](0045-image-on-a-datum.md) | An image on a datum, and its calibration | Amended | Contracts, vision | 0012, 0031, 0033 | 0083 |
| [0046](0046-image-in-the-desktop.md) | The image of a datum in the desktop | Accepted | Rendering, vision, clients | — | — |
| [0047](0047-contour-proposal.md) | The proposal of a contour and its accept as a sketch | Accepted | Vision, clients | 0042 | — |
| [0048](0048-stl-export.md) | The STL export of the live bodies | Amended | Persistence, clients | — | 0083 |
| [0049](0049-edit-of-an-extrude-distance.md) | The edit of the distance of an extrude | Accepted | Geometry, feature graph, clients | 0028, 0037, 0039 | — |
| [0050](0050-exact-predicates.md) | The exact predicates of the profile of a sketch | Accepted | Geometry, sketch, determinism | 0036, 0040 | — |
| [0051](0051-references-and-datum-on-a-face.md) | The references to the elements of a feature, and a datum on a face | Amended | Contracts, feature graph, sketch | 0012, 0031, 0033, 0036, 0039 | 0052, 0053, 0059, 0112 |
| [0052](0052-edge-and-vertex-references-in-a-sketch.md) | The edges and the vertices of a feature as references in a sketch | Accepted | Contracts, feature graph, sketch | 0033, 0035, 0036, 0051 | — |
| [0053](0053-desktop-sketch-on-a-face.md) | The desktop starts a sketch on a face, and draws a sketch on a datum of the Document | Amended | Desktop, sketch, feature graph | 0029, 0034, 0051 | 0059 |
| [0054](0054-ordered-feature-list.md) | The ordered feature list, a fold of the log | Amended | Feature graph, runtime | 0039 | 0055 |
| [0055](0055-regeneration.md) | The regeneration of the features after an edit | Amended | Feature graph, geometry, runtime | 0037, 0039, 0054 | 0059 |
| [0056](0056-dimension-overlay.md) | The dimension overlay of the desktop | Amended | Desktop, renderer | — | 0125 |
| [0057](0057-planar-subtract-in-the-owned-kernel.md) | The exact planar subtract of the owned kernel, with the provenance of each face | Amended | Kernel, geometry, determinism | — | 0058 |
| [0058](0058-the-engine-reads-the-owned-kernel.md) | The engine reads the owned kernel for the faces of a body | Amended | Kernel, geometry, queries | 0057 | 0059, 0112 |
| [0059](0059-references-by-the-identity-of-a-face.md) | The references by the identity of a face, and a datum on a face that a cut removed | Amended | Kernel, feature graph, sketch | 0051, 0053, 0055, 0058 | 0112 |
| [0060](0060-unit-kind-in-the-command-schema.md) | The unit kind of each number parameter in the command schema | Amended | Contracts, clients | 0013 | 0071, 0112 |
| [0061](0061-android-platform.md) | Android is a platform of the program, with the person on the phone and no surface | Accepted | Platform, clients, rendering | 0017, 0027, 0030 | — |
| [0070](0070-step-export-of-the-owned-kernel.md) | The STEP export of the planar faces of the owned kernel | Accepted | Kernel, persistence, clients | — | — |
| [0071](0071-container-model.md) | The container model, a part or an assembly of references at a fixed version | Accepted | Contracts, persistence | 0012, 0015, 0030, 0031, 0060 | — |
| [0072](0072-the-drawing-code-is-a-library.md) | The drawing code of a host is a library with no surface | Accepted | Rendering, clients, platform | 0027 | — |
| [0073](0073-the-phone-cuts-and-keeps-its-document.md) | The person cuts a body in the panel, and the phone keeps its document through the background | Amended | Clients, platform | — | 0079 |
| [0074](0074-the-probe-of-determinism-is-in-the-engine.md) | The probe of the determinism hash is a part of the engine, and the phone runs it | Accepted | Determinism, platform | — | — |
| [0075](0075-a-second-tap-selects-the-face.md) | A second tap on the selected body selects the face under the point | Amended | Clients, selection | 0029 | 0077, 0100 |
| [0076](0076-the-cylinder.md) | The cylinder is a primitive of plane facets | Accepted | Geometry, kernel | 0032 | — |
| [0077](0077-a-tap-near-an-edge-selects-the-edge.md) | An edge is named by its two faces, and a tap near an edge of the selected face selects it | Amended | Clients, selection, kernel | 0075 | 0100 |
| [0078](0078-the-theme-of-the-redesign.md) | The desktop and the phone take the theme "Proposed (harmonized)" of the redesign | Amended | Clients, rendering | — | 0091, 0108 |
| [0079](0079-the-activity-may-start-again.md) | The activity of the phone may start again in its process | Accepted | Clients, platform | 0073 | — |
| [0080](0080-the-fixed-controls-of-the-redesign.md) | The title of the document, Undo, Redo and the status line are fixed controls of the redesign | Amended | Clients, rendering | 0030, 0031 | 0093 |
| [0081](0081-the-tool-ring-of-the-redesign.md) | The tools of the selected body are the segments of a ring that follows it | Amended | Clients, rendering | — | 0084, 0086, 0095, 0101, 0108, 0113, 0116, 0119, 0132 |
| [0082](0082-the-values-on-the-edges-of-the-redesign.md) | A value that a command changes sits on the edge of the selected body that it measures | Amended | Clients, rendering | — | 0084, 0115 |
| [0083](0083-the-mesh-body-and-the-stl-import.md) | An STL file opens as a mesh body of a new part, through one import command | Amended | Geometry, contract, clients | 0044, 0045, 0048 | 0102, 0127 |
| [0084](0084-the-rows-of-the-selected-body.md) | The data of the selected body is in rows with no panel at the upper right | Amended | Clients, rendering | 0034, 0081, 0082 | 0098, 0099, 0117, 0131 |
| [0085](0085-the-sketch-under-its-body.md) | The active sketch of a live extrude moves out of its body, and the camera stays | Amended | Clients, rendering | 0034 | 0120 |
| [0086](0086-the-tools-of-the-sketch-on-the-ring.md) | While a sketch is active, the ring holds the tools of the sketch | Amended | Clients, rendering | 0081 | 0087, 0095, 0097, 0120 |
| [0087](0087-constrain-on-the-ring-of-a-sketch.md) | "Constrain" on the ring of a sketch opens the constraints that fit the selection | Amended | Clients, sketch | 0086 | 0097 |
| [0088](0088-the-window-of-a-sketch-beside-its-ring.md) | While the ring of a sketch shows, the window of the sketch does not repeat it | Accepted | Clients, sketch | 0034 | — |
| [0089](0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md) | The cone, the sphere and the torus are closed meshes that the engine makes | Accepted | Geometry, kernel | — | — |
| [0090](0090-the-answer-of-a-save-lists-its-warnings.md) | The HTTP answer of a save lists its warnings | Accepted | Surface, persistence | 0030 | — |
| [0091](0091-the-fonts-of-the-redesign.md) | The interface draws with Instrument Sans, and a value on an edge with JetBrains Mono | Amended | Clients, rendering | 0027, 0078 | 0117 |
| [0092](0092-an-agent-proposes-and-the-person-accepts.md) | An agent proposes commands, and the person accepts or rejects them | Amended | Surface, clients | — | 0107, 0108, 0110, 0111, 0113, 0130, 0133, 0134, 0135, 0136 |
| [0093](0093-the-layout-of-the-mix-has-no-panel-chrome.md) | The layout of the mix has a top bar, a panel with no chrome and a status line at the lower right | Amended | Clients, rendering | 0080 | 0099, 0100, 0113, 0124, 0127 |
| [0094](0094-a-framed-part-fills-45-percent-of-the-picture.md) | A framed part fills 45 % of the half picture, as in the render of the mix | Accepted | Clients, rendering | 0025 | — |
| [0095](0095-the-look-of-the-orbit-ring.md) | The ring has the look of the orbit of the render, with a guide, icons and a caption | Amended | Clients, rendering | 0081, 0086 | 0108 |
| [0096](0096-the-camera-of-the-session-has-z-up.md) | The camera of the session has the z axis up, as the render of the mix draws a part | Accepted | Clients, rendering | 0023, 0034 | — |
| [0097](0097-the-rows-of-a-sketch-take-the-place-of-the-panel.md) | The rows of an active sketch take the place of the panel, and its ring is on the right | Amended | Clients, sketch | 0034, 0086, 0087 | 0120 |
| [0098](0098-the-command-line.md) | The command line at the upper right runs the commands of the state with Control and K | Amended | Clients, rendering | 0032, 0084 | 0099, 0101, 0108, 0113, 0119, 0120, 0127, 0128 |
| [0099](0099-the-rows-of-a-selected-body-take-the-place-of-the-panel.md) | The rows of a selected body take the place of the panel at the upper left | Amended | Clients, rendering | 0084, 0093, 0098 | 0100, 0117, 0131 |
| [0100](0100-a-path-line-from-the-document-to-the-selection.md) | A path line from the document to the selection, and the rows of the document | Amended | Clients, selection | 0075, 0077, 0093, 0099, 0102 | 0101, 0138 |
| [0101](0101-a-focus-on-one-body.md) | A focus on one body is a state of the client that draws one body and writes nothing to the log | Accepted | Clients, selection | 0081, 0098, 0100 | — |
| [0102](0102-a-project-in-any-folder.md) | The window opens and saves a project in any folder, through the dialogs of the system | Amended | Persistence, clients | 0030, 0083 | 0100, 0106 |
| [0103](0103-the-vision-library-stays-in-the-spike-on-opencvsharp5.md) | The vision library stays in the spike, on OpenCvSharp5 | Accepted | Vision | 0042 | — |
| [0104](0104-the-history-arc.md) | The history arc shows the steps that made the selected body | Amended | Clients, rendering | — | 0116, 0119, 0123, 0130 |
| [0105](0105-the-values-on-the-edges-of-an-active-sketch.md) | The values of an active sketch show on its edges | Amended | Clients, rendering | — | 0115 |
| [0106](0106-open-project-shows-an-stl-file-too.md) | "Open project..." shows an STL file too, and an STL file goes to the import | Accepted | Persistence, clients | 0102 | — |
| [0107](0107-the-arc-of-a-proposal-is-in-the-free-room.md) | With no ring of a body, the arc of a proposal is in the room right of the panel | Accepted | Clients | 0092 | — |
| [0108](0108-the-look-of-the-render.md) | The window has the look of the render, an almost black ground and outlined controls with no fill | Accepted | Clients, rendering | 0078, 0081, 0092, 0095, 0098, 0113 | — |
| [0109](0109-the-edges-of-the-body.md) | The edges of a body are lines, and the selected body is transparent with dashed hidden edges | Amended | Rendering, clients | 0029 | 0121, 0129 |
| [0110](0110-a-proposal-shows-each-value-on-its-chip.md) | A proposal shows each value that it changes on the chip of that value | Amended | Clients, surface | 0092 | 0114, 0115, 0130 |
| [0111](0111-a-proposal-takes-its-own-taps-and-commands.md) | A proposal takes its own taps, its own command identifiers and its own steps | Amended | Surface, clients | 0092 | 0114, 0135 |
| [0112](0112-a-command-turns-a-body.md) | A command turns a body by a quaternion about the origin of the Document | Amended | Contracts, geometry | 0012, 0039, 0051, 0058, 0059, 0060 | 0114 |
| [0113](0113-the-layout-at-each-phone-size-and-small-window.md) | The layout keeps each control in the display and free for a tap at each phone size and in a small window | Amended | Clients, layout | 0081, 0092, 0093, 0098 | 0108, 0114 |
| [0114](0114-the-findings-of-the-review-of-2026-11-10.md) | A turn refuses a quaternion of very small parts, a proposal shows an angle in degrees, and an accept holds less | Accepted | Geometry, clients | 0110, 0111, 0112, 0113 | — |
| [0115](0115-the-values-as-in-the-render.md) | A value shows three decimals on a pill on its dimension line, outside the outline of the body | Accepted | Clients, rendering | 0082, 0105, 0110 | — |
| [0116](0116-the-ring-takes-a-tap-only-on-its-labels.md) | Only the icon and the label of a segment of a body ring take a tap, and the ring fades while the view moves | Accepted | Clients, input | 0081, 0104 | — |
| [0117](0117-the-rows-of-a-body-as-in-the-render.md) | The rows of a body show its name larger, the sketch that made it, and the direction and the constraint of each value | Amended | Clients, rendering | 0084, 0091, 0099 | 0123 |
| [0118](0118-the-2d-drawing-of-a-part.md) | The 2D drawing of a part has a date, and its work starts with a flat view | Accepted | Charter, drawing | — | — |
| [0119](0119-the-ring-and-the-history-arc-as-in-the-render.md) | The ring gives Edit sketch and Rename in the span of four segments, and the history arc is a bright line with its nodes on it | Amended | Clients, rendering | 0081, 0098, 0104 | 0122, 0123 |
| [0120](0120-the-sketch-as-in-the-render.md) | The rows of a sketch are its constraints and the body it makes, its ring holds Next point and Extrude, and its plate has a label | Accepted | Clients, sketch | 0085, 0086, 0097, 0098 | — |
| [0121](0121-the-edges-of-a-large-mesh-are-found-on-the-thread-pool.md) | The edges of a mesh of more than 100 000 triangles are found on the thread pool | Accepted | Rendering, clients | 0109 | — |
| [0122](0122-dimensions-is-a-segment-of-the-ring.md) | Dimensions is a segment of the ring of a body | Accepted | Clients, rendering | 0119 | — |
| [0123](0123-the-sketch-that-made-a-body-is-found-along-the-first-input.md) | The sketch that made a body is found along the first input, and the history keeps it after the six newest steps | Accepted | Clients, rendering | 0104, 0117, 0119 | — |
| [0124](0124-the-selection-after-a-hide-and-the-hidden-bodies-in-the-status-line.md) | The selection after a hide, and the hidden bodies in the status line | Amended | Clients, rendering | 0029, 0093 | 0129 |
| [0125](0125-each-dimension-says-what-it-measures.md) | Each dimension says what it measures, on a plate outside its box, and no control covers it | Amended | Clients, rendering | 0056 | 0129 |
| [0126](0126-assemblies-with-joints-and-geometric-checks.md) | Assemblies with joints and their geometric checks have a date after P10c, and a load check follows them | Accepted | Charter, assembly | — | — |
| [0127](0127-the-fixed-controls-and-the-status-as-in-the-render.md) | The row of the anchors holds Undo, Redo and the step of Undo, and the status gives the log, the agent and the save | Amended | Clients, rendering | 0083, 0093, 0098 | 0129, 0138 |
| [0128](0128-a-value-typed-in-the-command-line-goes-to-its-pad.md) | A value typed in the command line goes to the pad of its value, through the evaluator | Accepted | Clients, rendering | 0098 | — |
| [0129](0129-the-findings-of-the-review-of-2026-11-14.md) | The list of the hidden bodies fits the display, a stuck plate stays at its place, and a step text follows its command | Accepted | Clients, rendering | 0109, 0124, 0125, 0127 | — |
| [0130](0130-the-proposal-in-the-window-as-in-the-render.md) | A proposal shows its values in the rows, a pending node on the history arc, a glow on the edges and Undo after an accept | Accepted | Clients, rendering | 0092, 0104, 0110 | — |
| [0131](0131-the-rows-under-the-part-on-a-portrait-display.md) | On a portrait display the rows of a selected body are under the part, and the panel has no controls of a selected body | Amended | Clients, rendering | 0084, 0099 | 0135 |
| [0132](0132-the-labels-of-the-corner-ring.md) | A label of the corner ring is drawn smaller until it lies on no other label and fits inside the display | Accepted | Clients, rendering | 0081 | — |
| [0133](0133-the-caption-of-a-proposal-on-its-arc.md) | The caption of a proposal is on the band of its arc, and the command lines under the arc lie on no chip | Amended | Clients, rendering | 0092 | 0134, 0136 |
| [0134](0134-the-arc-of-a-proposal-right-of-the-rows.md) | The arc of a proposal moves right of the rows of the selected body, and its lines keep off the tools of the ring | Accepted | Clients, rendering | 0092, 0133 | — |
| [0135](0135-the-arc-of-a-proposal-on-a-portrait-display.md) | On a portrait display the arc of a proposal is at the top, and a sheet of its lines is under it | Accepted | Clients, rendering | 0092, 0111, 0131 | — |
| [0136](0136-the-arc-ends-before-the-tools-and-the-caption-wraps.md) | The arc of a proposal ends before each tool of the ring, and the whole caption under it wraps | Amended | Clients, rendering | 0092, 0133 | 0139 |
| [0137](0137-the-flat-views-of-the-camera.md) | The camera has six flat views with no perspective, and the command line gives each one | Amended | Rendering, clients | 0023 | 0138, 0139 |
| [0138](0138-the-controls-of-the-flat-views.md) | A button "Views" opens a net of the six flat views and the 3D view, and the keys 1, 3, 7 and 0 give them | Accepted | Clients | 0100, 0127, 0137 | — |
| [0139](0139-a-sketch-leaves-a-flat-view-and-no-tool-hides-under-the-arc.md) | A sketch leaves a flat view, and a tool under an end of the arc of a proposal hides while the proposal waits | Accepted | Clients, rendering | 0136, 0137 | — |

## Topics

- **Contracts** — 0005, 0006, 0008, 0012, 0013, 0015, 0018, 0020, 0021, 0028, 0033, 0045, 0060, 0071, 0112. Read these before changing `Engine.Contracts/**`.
- **Boundary** — 0001, 0004, 0007, 0012, 0014, 0017, 0022, 0025. Read these before changing project references or adding clients.
- **Workflow** — 0002. Read before adding a client or skipping CLI.
- **Clients** — 0002, 0007, 0019, 0024, 0026, 0027, 0029, 0030, 0032, 0034, 0060, 0061, 0072. Read before changing UI/CLI/host code.
- **Deployment** — 0004, 0019, 0030, 0061. Read these before changing where a session runs or which transport a host uses.
- **Geometry** — 0001, 0012, 0014, 0018, 0021, 0028, 0076, 0112. Read before adding a capability or backend.
- **Observability** — 0005, 0010, 0013, 0020, 0021. Read before changing event ordering, retention, subscription, or schema endpoints.
- **Rendering** — 0009, 0017, 0018, 0022, 0023, 0024, 0025, 0026, 0027, 0029, 0061, 0072. Read these before changing `3DEngine.Core`, `3DEngine.Vulkan`, `3DEngine.App` or `3DEngine`.

## Adding an ADR

1. Use the form in `docs/templates.md`, section 1.
2. Number sequentially. The status is `Proposed` until the owner accepts the record.
3. If the record amends an earlier ADR, add the entry in its "Amended by" column above, and change
   the text of the earlier ADR in the same commit (`docs/templates.md`, section 1, "An ADR agrees
   with the code").
4. Update this index in the same commit. `AdrGateTests` fails when a row and a record disagree.

## Archived

A withdrawn or superseded record moves to `docs/adr/archive/`, and its number stays in this list so
that the number is never used again. Do not apply an archived record. `AdrGateTests` fails when a
number in this list still has a file in `docs/adr/`, and when a record in `docs/adr/` is withdrawn or
superseded.

| # | Title | Status | Replaced by | Archived |
|---|---|---|---|---|
| [0003](archive/0003-blazor-as-thin-viewer.md) | Blazor as thin viewer | Withdrawn | — | 2026-10-27, TASK-0040 |
| [0011](archive/0011-server-default-deployment-topology.md) | Server-default deployment, embed for offline | Superseded | 0019 | 2026-10-27, TASK-0040 |
