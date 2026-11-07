---
id: 0257
title: An STL file opens as a mesh body of a new part, through one import command (the record)
status: Active
phase: P9a
opened: 2026-11-07
depends-on: [0254]
governed-by: [0001, 0008, 0012, 0018, 0036, 0083]
writes:
  create:
    - tasks/TASK-0257-the-mesh-body-and-the-stl-import-record.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - Engine.Contracts/Geometry/IMeshImportOps.cs
    - tasks/TASK-0258-the-stl-reader-and-the-mesh-body.md
  modify:
    - docs/adr/0044-asset-store.md
    - docs/adr/0045-image-on-a-datum.md
    - docs/adr/0048-stl-export.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Core/**
---

# TASK-0257 — An STL file opens as a mesh body of a new part, through one import command (the record)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-07 the owner added the demonstration "Open the same STL on desktop and Android" to the standing order, with
its page (https://claude.ai/artifact/NJVBcQYiheLRCcRgx9YQad) and the decisions D1 to D6, each the recommended option.
The decisions answer the "stop and ask" of `CLAUDE.md` for this change of the contract, and the record of them goes
into the same commit as the change. The owner also found that the row R1 of the roadmap reads as open, although R1
shipped at v0.28; this task corrects it.

## Goal

ADR-0083 records the mesh body, the command `ImportStl`, the reader, the codes, the refused operations, the asset at a
replay and the limit; the contract gets the capability `IMeshImportOps`; the roadmap gets the rows P9a to P9e.

## Scope (in)

1. ADR-0083, Accepted with D1 to D6.
2. `Engine.Contracts/Geometry/IMeshImportOps.cs`.
3. The rows P9a to P9e, and the correction of the row R1.

## Scope (out)

- The reader, the handler and the backends (P9b, TASK-0258), the desktop (P9c) and the phone (P9d).

## Acceptance criteria

- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the record from the page of the demonstration and the decisions of the owner.
Judgement:  the interpretation of D2 (the name and the visibility are a part of the view), the limit of 300,000
            triangles from the bound of the store, and the asset at a replay, which amends ADR-0045 item 4.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by the addition of the owner to the standing order.
