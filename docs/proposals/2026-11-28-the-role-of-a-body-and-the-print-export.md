# The role of a body and the print export (P14)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-28. TASK-0494, phase P14. Written under the standing order of 2026-11-28.
- Status: a proposal. Not a rule. It changes no code, no contract and no ADR. The role is a new field of `BodyRecord`, so it changes
  the public shape of `Engine.Contracts`, and `CLAUDE.md` ("Stop and ask") asks the owner first. The ADR of P14 comes after the
  answers, in the same commit as the change of `Engine.Contracts`.

## 1 The question

Row P14: the role of a body (to print, context, excluded from an envelope) and the print export: one STL file for each chosen part,
in millimetres, with a check that its mesh is closed and has no degenerate triangle; 3MF later (ADR-0126 items 6 and 7). K13, the
envelope, comes after it and reads the roles.

## 2 The facts

Each fact was read in the repository on 2026-11-28.

1. **The body record.** `Engine.Contracts/BodyRecord.cs` holds the handle, the kind, the name, the visibility, the key of the
   geometry, the fault and the placement. Each optional field has a default, so a log of before gives the same record.
2. **The commands of a record.** `RenameBody` and the command of the visibility change one field of a record as one step, with
   `Undo` and `Redo` (ADR-0028).
3. **The export of today.** `Engine.Core/Export/StlExport.cs` writes each live body of a Document into one binary STL file, from
   the triangles of the backend with the placement (ADR-0048, ADR-0191 item 7). It checks no closed mesh. Its codes are
   `E-EXPORT-*`.
4. **The lamp case.** `Engine.Tests/Assembly/LampSupportCase.cs` has seven bodies: a crossbar, a bracket, an arm and a knob to
   print, and a lamp, a housing and a cable that are context.

## 3 The recommended model

1. **The role.** `BodyRecord` gets the optional field `Role`: `print`, `context` or `excluded`, and null for `print`, so each log of
   before gives the same records. `print` is a part to print; `context` is a volume that the design must clear, as the lamp, and is
   not printed; `excluded` is a body that the envelope of K13 does not take, and is not printed.
2. **The command.** `SetBodyRole` (the body and the role) is one step, as `RenameBody` is. The events `body.created` and
   `body.changed` carry the member `role`.
3. **The export.** A new export writes one binary STL file for each live body with the role `print`, in millimetres, with its
   placement. The command line writes the files into a folder (`engine export print <document> <folder>`); the HTTP surface gives
   one body for each request (`/export/stl?bodyId=…`), so an agent asks for each part.
4. **The check of a mesh.** Before it writes a file, the export checks the triangles in exact numbers of their doubles: each edge is
   in two triangles with opposite directions (closed and oriented), and no triangle has an area of zero. A failed check refuses
   the file of that part with a new code, and the other parts are written.
5. **The names of the files.** The name of the body, and its identifier for a body with no name or with a name that another body
   to print has.

## 4 What the model costs

- `Engine.Contracts` gets the field and the members of two events, so the ADR comes with it.
- A version of the app before P14 refuses a file that holds `SetBodyRole`.
- The check of a mesh reads each triangle once more for each part.

## 5 The questions for the owner

1. **Q1 (the record).** **A (recommended):** a field `Role` of `BodyRecord`, null for `print`. **B:** a list of roles in the
   Document beside the bodies.
2. **Q2 (the roles).** **A (recommended):** one of three, `print`, `context` and `excluded`, as ADR-0126 item 6 names them.
   **B:** two marks that can both be set: "print" and "in the envelope".
3. **Q3 (the delivery of the export).** **A (recommended):** a folder of files on the command line and one body for each HTTP
   request. **B:** one ZIP archive of the files on both surfaces.
4. **Q4 (a failed check).** **A (recommended):** the part with a mesh that is not closed or has a degenerate triangle is refused
   with its own code, and the other parts are written. **B:** the whole export is refused.
5. **Q5 (the order).** **A (recommended):** P14 is the role, the command and the export with its check, with the ADR and a test of
   each rule; 3MF is a later row. **B:** P14a the role, P14b the export.

## 6 Out of this document

- The envelope (K13), which reads the roles.
- The 3MF export.
