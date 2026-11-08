---
id: 0083
title: An STL file opens as a mesh body of a new part, through one import command
status: Accepted
topic: Geometry, contract, clients
date: 2026-11-07
supersedes: []
superseded-by: []
amends: ['0044', '0045', '0048']
amended-by: []
affects:
  - Engine.Contracts/Geometry/IMeshImportOps.cs
  - Engine.Core/Commands/ImportStlCommand.cs
  - Engine.Core/Commands/ImportStlCommandHandler.cs
  - Engine.Core/Import/StlReader.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
  - Engine.Core/Geometry/InProcessMeshBackend.cs
  - 3DEngine.Vulkan/Window.FileDialog.cs
  - 3DEngine.App/StlOpening.cs
  - 3DEngine.App/Ui/DesktopUi.Home.cs
  - 3DEngine/DesktopDocument.cs
  - 3DEngine/DesktopSession.cs
  - 3DEngine.Android/PhoneHost.cs
  - 3DEngine.App/ISessionHost.cs
  - 3DEngine.App/LocalSessionHost.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/PhoneFiles.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - eng/android/check-apk.sh
  - Engine.Api.Http/EngineHost.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - Engine.Api.Http.Server/ServerEngine.cs
  - Engine.Cli/Cli.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Core/Assets/IAssetSource.cs
  - Engine.Core/Commands/AddSketchReferenceCommandHandler.cs
  - Engine.Core/Commands/CreateDatumCommandHandler.cs
  - Engine.Core/Commands/SubtractCommandHandler.cs
  - Engine.Core/Commands/TranslateCommandHandler.cs
  - Engine.Core/Determinism/CrossPlatformProbe.cs
  - Engine.Core/Export/StepExport.cs
  - Engine.Core/Hosting/EngineHosting.cs
  - Engine.Core/Persistence/DocumentFile.cs
  - Engine.Core/Queries/GetFaceAtQueryHandler.cs
  - Engine.Core/Queries/GetFacesQueryHandler.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
enforced-by: Engine.Tests/Import/StlImportTests.cs (TASK-0258 creates it)
---

# ADR-0083 — An STL file opens as a mesh body of a new part, through one import command

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-07 the owner added a demonstration to the standing order: a person opens the same STL file of a small
bracket on the desktop and on the Galaxy S23 Ultra, sees the whole part, turns and zooms it, and a damaged file gives a
clear message and a second try. The page of the demonstration (https://claude.ai/artifact/NJVBcQYiheLRCcRgx9YQad)
holds the script, the pass criteria, the gap analysis and six decisions. The owner chose the recommendation of each one,
in chat, and that choice answers the "stop and ask" of `CLAUDE.md` for this change of the contract.

Today STL is an export only (ADR-0048). Five rules shape the import:

- No client keeps the state of the Document, and no client passes the command bus (anti-objectives 1 and 2). The open
  goes through a command, so a replay and an agent see the same part.
- No automatic conversion between a mesh and a B-Rep, and no silent fallback (anti-objective 9). The STL is a body of
  its own kind.
- No healing of a shape (anti-objective 13). A damaged file is refused with a code.
- No tessellation in the Document (`CLAUDE.md`, determinism rule 6; ADR-0007). The triangles of an STL are input, not a
  tessellation that the engine made: their bytes go to the store of the assets under their SHA-256 (ADR-0044), as the
  bytes of an image do (ADR-0045), and the command records the name of the asset.
- A replay gives the same result on each platform (anti-objective 16). A binary STL holds 32-bit floats, so the reader
  keeps their exact values and does no arithmetic on them.

No handler reads the store today; ADR-0045 item 4 says that a replay must not need the bytes of an image. A mesh body
must exist in the backend at each apply and at each replay, because `GetTessellation` and the STL export read it there.
So the handler of the import reads the bytes of its asset.

### The decisions of the owner (2026-11-07, each the recommended option)

- **D1.** An open of an STL makes a new part document with one import command. No viewer outside the engine.
- **D2.** A mesh body can be viewed, framed and exported as STL only. Each other operation on it is refused with a code.
- **D3.** The reader accepts binary and text STL. Each fault has its own code.
- **D4.** The unit is millimetres, shown as "mm (assumed)".
- **D5.** A limit of triangles, with a code; this record proposes the number.
- **D6.** A small home screen now: "Open file", "New part" and the recent files, in the theme of R16a.

## Decision

1. **The command (D1).** `ImportStl`@1 has the parameters `asset` (a name of ADR-0044 item 1) and `name` (optional, the
   name of the body, for example the name of the file). The output is `bodyId`, the identifier of the command. The
   handler reads the bytes of the asset, reads the STL (item 3), and stores its triangles in the backend under the handle
   of the body (`IMeshImportOps.ImportMesh`, new). The body has the kind `Mesh`, and it is a feature with no input, as a
   box is. The client puts the bytes of the file into the store, opens a new part document, and applies one `ImportStl`.
2. **The asset at a replay.** The host gives the store to the handler when it composes the engine. At an apply, an open
   of a document file and a replay, the handler reads the asset; a host with no store, a name that the store does not
   hold, and an asset that does not read refuse the command with `E-STL-ASSET`. This record amends ADR-0045 item 4 for
   this command only: the replay of an import needs its bytes. A document file still does not carry its assets (ADR-0044
   item 6), so a document with a mesh body opens only where the store holds its asset; a pack of a document with its
   assets stays a later phase.
3. **The reader (D3).** `StlReader` takes the bytes and gives the triangles as 32-bit floats, nine for each triangle,
   in the order of the file, or one code.
   - **Binary:** an 80-byte header, a 32-bit count of triangles, and 50 bytes for each triangle (a normal, three
     vertices, two bytes of attributes), little-endian. A file whose length is 84 plus 50 times its count is binary. The
     reader copies the bits of each vertex coordinate, with no arithmetic, and does not read the normal.
   - **Text:** `solid`, then for each triangle `facet normal`, `outer loop`, three `vertex x y z`, `endloop` and
     `endfacet`, then `endsolid`. Each number is read as a 32-bit float with the invariant culture, which rounds to the
     nearest value in the same way on each platform; the normal is not used.
   - **The faults, each with its code:** a file that is neither form, or a binary file whose length is not 84 plus 50
     times its count (`E-STL-TRUNCATED`, as `bracket-damaged.stl`); a word or a number of a text file that does not
     read, or a text file that ends before `endsolid` (`E-STL-SYNTAX`); a coordinate that is not finite
     (`E-STL-NOT-FINITE`); no triangle (`E-STL-EMPTY`); more triangles than the limit (`E-STL-TOO-LARGE`, item 6).
   - **A change of the reader (E173 A, the standing order, 2026-11-07).** A recorded import reads its asset again at each
     replay (item 2). So a change of the reader keeps the result of each file that an earlier reader accepted, and it
     may only accept a file that an earlier reader refused, because a rejected command writes no entry. A change that
     cannot keep a result takes a new schema version of `ImportStl`, whose handler keeps the earlier reader for version 1.
     A test compares the forms that an earlier reader read with its result.
   - **The order of the rules (E180 to E182, TASK-0280; Q1 A under the standing order, 2026-11-07).** Each file gets the
     result of the first of three rules that accepts it: the rule of v0.243, the rule of v0.257 and later, then the rule
     of v0.250. The rule of v0.254 gives the result of v0.257 for each file that it accepted. Two classes of files had two
     earlier results: a whole facet on the line of `solid`, which v0.243 and v0.250 read, and `endsolid` with a whole
     facet on that line, which v0.250 and v0.254 read. Such a file gets the result of v0.243, or else of v0.254, so a
     document of v0.250 to v0.253 with such a file replays to that result. No exporter writes such a file. The owner
     checks this answer on 2026-11-08.
4. **What a mesh body permits (D2).** The view: `GetTessellation`, `GetBoundingBox`, `GetBodyInfo`, the frame of the
   camera, and the STL export (ADR-0048 amended: the export holds the triangles of a mesh body as they are). The
   visibility and the name are a part of the view and stay permitted. Each other operation refuses a mesh body with
   `E-MESH-UNSUPPORTED`: `Translate`, `Subtract` (as the body and as the tool), a datum and a reference of a sketch on its
   faces, `GetFaces`, `GetFaceAt`, and the STEP export. No silent result and no conversion.
5. **The unit (D4).** The coordinates are millimetres. Each Document of the engine has the length unit millimetres
   (ADR-0015 item 7), so the import needs no check of the unit, and no conversion multiplies the floats. The client shows
   the size with "mm (assumed)".
6. **The limit (D5).** At most 300,000 triangles. The store holds at most 16 MiB in one asset (ADR-0044), which bounds a
   binary file to 335,542 triangles; 300,000 is below that bound, and the engine row (P9b) measures the read and the
   first frame of such a file on this computer and on the emulator. The run of the owner on the S23 can lower it with a
   new entry of this record.
7. **The home screen (D6).** The desktop and the phone start on a small screen with "Open file", "New part" and the
   recent files, in the theme of ADR-0078. The rows P9c and P9d give it; its layout can change in a later slice of the
   redesign. A start of the desktop with the setting `file` opens that file and shows no home screen (the owner,
   2026-11-08, TASK-0290).
8. **The contract.** `Engine.Contracts/Geometry/IMeshImportOps.cs` is new: a capability of a backend that stores the
   triangles of a mesh under a handle and gives them back in `Tessellate` with the exact values of the floats. The
   event of a new body is `body.created`, with the kind `Mesh`; no event kind is new.

## Consequences

**Good:** a file of another program opens as a part of the engine, through the bus, with the same result on each
platform; an agent reads it as it reads each body.

**Bad:** a document with a mesh body opens only where the store holds its asset. A mesh body takes no part in a cut, a
move or a sketch until a later decision of the kernel. The handler of the import is the first handler that reads the
store.

**Next:** the engine row P9b (the reader, the handler, the backends, the codes and the test on four platforms), the
desktop row P9c, the phone row P9d, and the test sheet P9e.

## History

- 2026-11-07 · TASK-0257 · The record, Accepted with the decisions D1 to D6 of the owner of 2026-11-07.
- 2026-11-07 · TASK-0257 · Shipped at v0.242.
- 2026-11-07 · TASK-0258 · The engine part. The measure of item 6 on this computer: a binary file of 300,000 triangles
  (15,000,084 bytes) reads in 32 ms, imports in 34 ms and tessellates in 7 ms, with 55 MiB allocated; the emulator and
  the S23 are slower, and the run of the owner measures the phone. The probe of ADR-0074 holds the export and the import
  of the bracket, so the four platforms compare the reader.
- 2026-11-07 · TASK-0258 · Shipped at v0.243.
- 2026-11-07 · TASK-0262 · Item 2: the check of an open on the desktop reads the asset from the store of the desktop too
  (finding E167 of the codebase review of 2026-11-07).
- 2026-11-07 · TASK-0259 · The desktop part: the home screen of item 7 at the first session of the program; "Open file"
  opens the file dialog of the system through SDL; the client checks the chosen file with the reader of the engine and
  puts it into the store, so a fault gives its code, a message and "Choose another file", and no new document; a good file
  asks for a new part document whose session applies the one import first; the camera frames the first body; the size of
  a mesh body says "mm (assumed)"; "Frame" and "Home" are on the row of the anchors of ADR-0080.
- 2026-11-07 · TASK-0260 · The phone part: the same home screen with no recent file (the phone has no document folder yet),
  the document picker of Android through the dialog of SDL with no permission of storage (the APK check of the pipeline
  holds it), the store in the folder of the application, and a new part as a new session in the same window.
- 2026-11-07 · TASK-0263 · The affects field names the 26 code files of TASK-0258 to TASK-0260 that cite this ADR: the
  refusals of item 4, the store of item 2, the codes, the hosts and the clients (finding T167 of the codebase review of
  2026-11-07). `AffectsCitationGateTests` holds it.
- 2026-11-07 · TASK-0269 · Item 3: a change of the reader keeps the result of each file that an earlier reader
  accepted (finding E173 of the codebase review of 2026-11-07 (b), answer A under the standing order). The name of the
  solid ends at a word "facet" that "normal" and a number follow, or at "facet" or "endsolid" after its line, so each
  file that v0.243 read gives the result of v0.243.
- 2026-11-07 · TASK-0270 · D1 and D6: a request of a new part takes the place of an open, so an open of another file in
  the same session gets a refusal, and the import reaches only its new part; a new part that does not start drops its
  import; an accepted open of a file ends the home screen (findings V125 and V126 of the codebase review of 2026-11-07
  (b)).
- 2026-11-07 · TASK-0274 · Item 3: the name of the solid ends at the start of a whole facet ("facet normal", three
  numbers and "outer"), so a name that the reader of v0.250 read gives its result again (the review of Codex of v0.254).
  A whole facet on the line of "solid" had two earlier results; the reader gives the result of v0.243.
- 2026-11-07 · TASK-0271 · D6: the home screen gives the scene no sample and no key, and no gizmo shows behind it; a
  refusal of an open from a recent row and a dialog that fails give a message there (findings V127, V130 and V131 of the
  codebase review of 2026-11-07 (b)). Item 4: a mesh body shows no gizmo and no Move on its ring; the affects field names
  3DEngine.App/Ui/DesktopUi.Ring.cs.
- 2026-11-07 · TASK-0272 · Item 3: a text file whose name is not UTF-8 gives `E-STL-SYNTAX`, and a cut binary file of fewer
  than 84 bytes gives `E-STL-TRUNCATED` (finding E178 of the codebase review of 2026-11-07 (b)); each was a refusal
  before and stays one. D3: the client refuses a file of more than 84 + 512 times the limit of triangles bytes with
  `E-STL-TOO-LARGE` before it reads it (finding N2 of the review of Codex after v0.243).
- 2026-11-07 · TASK-0280 · Item 3: the order of the rules of the earlier readers (findings E180, E181 and E182 of the
  codebase review of 2026-11-07 (c)); a text file with a zero byte, which v0.243 read, and a name that only v0.250 read get
  their earlier results again. Q1 A under the standing order.
- 2026-11-07 · TASK-0281 · D3: the client refuses a file of more bytes than the store keeps (16 MiB), and its read stops at
  that limit also when the size is not known (finding E183 of the codebase review of 2026-11-07 (c)); the limit of
  TASK-0272 was nine times larger.
- 2026-11-08 · TASK-0290 · Item 7: a start with the setting `file` shows no home screen (the answer of the owner in the
  form of decisions).
- 2026-11-08 · TASK-0295 · D3: the client gives `E-ASSET-TOO-LARGE` for a file of more than 16 MiB, also when its read stops
  at the limit; `E-STL-TOO-LARGE` keeps the meaning of item 6 (finding E190 of the codebase review of 2026-11-08).
