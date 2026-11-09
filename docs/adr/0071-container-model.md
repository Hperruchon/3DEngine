---
id: 0071
title: The container model, a part or an assembly of references at a fixed version
status: Accepted
topic: Contracts, persistence
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0012', '0015', '0030', '0031', '0060']
amended-by: []
affects:
  - Engine.Contracts/Assembly/**
  - Engine.Core/Commands/DocumentKinds.cs
  - Engine.Core/Commands/AddPartReference*.cs
  - Engine.Core/Queries/GetPartReferences*.cs
enforced-by: Engine.Tests/Assembly/ContainerModelTests.cs
---

# ADR-0071 — The container model, a part or an assembly of references at a fixed version

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap row P8b is "The container model. One document holds one part. An assembly holds references at a fixed
version." On 2026-11-05 the owner ordered P8b in the engine lane of the two lanes, with a standing order that answers
each question with the recommended option. This record is Accepted under that standing order; the owner checks it on
2026-11-08.

Today a Document has no kind: each document is a part, with its bodies, sketches, images and datums. An assembly needs
a reference to another document, and a reference must not change when the part changes later. Four constraints remove
the obvious alternatives:

- A saved document must still open, and its log must not change (anti-objective 5; the order of the owner). So a part
  file keeps its bytes, and no migration rewrites a file.
- A replay must not need anything outside the log that can change (anti-objective 5). A path to a part file can name
  other bytes tomorrow, so a reference by path alone breaks the replay.
- The charter lists "Multi-Document in one runtime" and "Assemblies" as non-goals: assemblies arrive after the
  reference model operates. A reference that the engine resolves needs the part as a second Document in the same
  runtime. So this slice records the references and does not resolve them.
- The engine already has a store of bytes by their SHA-256 (ADR-0044), and an image names its bytes there without a
  read of the store in the handler (ADR-0045). A reference can use the same rule.

## Decision

1. **The kind.** `Document.Kind` is `part` or `assembly` (`Document.Part`, `Document.Assembly`), fixed when the
   Document is made, `part` by default. A part holds the bodies, the sketches, the images and the datums of one part.
   An assembly holds references to parts only.
2. **The commands of a kind.** A handler states the kinds of its command through `IDocumentKinds` in
   `Engine.Core/Commands/DocumentKinds.cs`, an interface of `Engine.Core` as `ICommandEncoder` is, so
   `ICommandHandler` does not change. A handler that does not implement it applies in a part only, so each command of a
   part needs no change. `Undo`, `Redo` and `NoOp` apply in each kind; `AddPartReference` applies in an assembly only.
   The bus refuses a command of another kind before its handler runs, with `E-CMD-KIND-REFUSED`; the version does not
   change, and the refusal gives `command.rejected`.
3. **The reference.** `Engine.Contracts.Assembly.PartReference(Id, Part, Version, X, Y, Z)`. `AddPartReference` takes
   `part`, the name of the bytes of the part file in the store of the assets (64 lower-case hexadecimal digits,
   ADR-0044 item 1): its header and its first `version` entries. `version` is an integer of 1 or more with the new unit
   kind `count` of ADR-0060, and `x`, `y` and
   `z` place the origin of the part in the length unit, 0 by default. The identifier of the reference is the identifier
   of the command, and the output is `referenceId`. The name of the bytes fixes the version: other bytes have another
   name, so the reference cannot change, and a moved file does not break it. As `PlaceImage`, the handler checks the
   form of the name and does not read the store, because a replay must not need the bytes (ADR-0044 item 6). A bad
   name, a version below 1 and a place that is not finite give `E-ASSEMBLY-REFERENCE-INVALID`; an identifier that the
   log holds gives `E-ASSEMBLY-ID-IN-USE`.
4. **The file.** The header of a part has no field `kind`, so a part file has the same bytes as before TASK-0202, and
   each file of an earlier version opens as a part. Since TASK-0224 the header of an assembly is
   `{"format":"3dengine-document","formatVersion":2,"lengthUnit":"mm","kind":"assembly"}`. An engine before TASK-0202
   reads the version 1 only, so it refuses each new assembly at its header and writes no part entry after it (finding
   E154 of the codebase review of 2026-11-06 (b), Q1 A). An assembly file of TASK-0202 to TASK-0224 has the version 1
   with the field `kind`, and it still opens as an assembly. A header of the version 2 that is not an assembly, another
   kind, and an open into a session of another kind each give `E-IO-LOAD-FAILED`. A host makes the Document with the kind of
   the header of its file (`DocumentFile.ReadKind`). `engine-api-http --kind assembly` makes a new document an
   assembly; a file that exists keeps its kind, and another `--kind` stops the start with the exit code 6. `engine
   apply` and `engine query` take `--kind assembly` for their one-shot Document. The desktop makes parts only.
5. **The bus.** `CommandHandlerResult.CreatedReferences` gives the references that a command creates. The commit adds
   each one and emits `reference.created` with each field of the record, after the datum events. A record never
   changes. A step records its references, an `Undo` removes them with `reference.removed`, and a `Redo` gives them
   again with `reference.created`. The bus refuses an identifier that it created before.
6. **The read.** The query `GetPartReferences` gives `{ kind, references }`, the references in the order of the log.
   `GET /document` gives `kind`. The snapshot of `subscription.reset` gives `kind` and `references`, each reference with
   each field of its record.
7. **Not in P8b1.** The resolution of a reference: a read of the part from the store, its replay, the bodies of the
   part in the assembly, and a check that the bytes hold `version` entries. Also a rotation of a reference (a matrix,
   `CLAUDE.md` determinism rule 2), a change of the version, a removal other than `Undo`, a reference to an assembly,
   and the desktop.

This record amends ADR-0012 §6 (the snapshot gives `kind` and `references`), ADR-0015 item 2 (the header of an
assembly), ADR-0030 item 4 (`GET /document` gives `kind`), ADR-0031 items 3 to 5 (a step records its references,
and the reference events come after the datum events) and ADR-0060 item 2 (the unit kind `count`).

## Consequences

**Good:** a document states what it is, and each saved document opens as before, with the same bytes. An assembly
records which part at which version it uses, by a name that no later change of the part can move, so a replay of the
assembly never depends on a file that changed. An agent reads the references through a query, the events and the
snapshot.

**Bad:** a reference is data only until the owner decides on the non-goal "Multi-Document in one runtime": the engine
does not check that the bytes exist or that they hold `version` entries, so a wrong reference is found only later. The
part must be in the store of the account, as an image is, so an assembly on another computer needs that store too.
Each later command of an assembly must state its kinds.

**Next:** the owner decides whether the engine may hold a second Document to resolve a reference; that decision opens
the next slice of P8b, with a fault for a reference whose bytes are absent or hold another count of entries.

## History

- 2026-11-06 · TASK-0202 · The record, under the standing order of 2026-11-05.
- 2026-11-06 · TASK-0224 · Item 4: the header of an assembly has the version 2 (finding E154, Q1 A). Until TASK-0224 the
  item said that an older engine reads no assembly as a part; it read an assembly with no reference as an empty part.
- 2026-11-09 · TASK-0317 · The owner allows a second Document in one runtime to resolve a reference (R-0055, answer B):
  the charter no longer lists the non-goal "Multi-Document in one runtime", and its own ADR and task give the design.
