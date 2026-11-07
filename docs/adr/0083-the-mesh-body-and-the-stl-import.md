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
   redesign.
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
- 2026-11-07 · TASK-0258 · The engine part. The measure of item 6 on this computer: a binary file of 300,000 triangles
  (15,000,084 bytes) reads in 32 ms, imports in 34 ms and tessellates in 7 ms, with 55 MiB allocated; the emulator and
  the S23 are slower, and the run of the owner measures the phone. The probe of ADR-0074 holds the export and the import
  of the bracket, so the four platforms compare the reader.
