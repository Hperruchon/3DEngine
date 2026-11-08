---
id: 0090
title: The HTTP answer of a save lists its warnings
status: Accepted
topic: Surface, persistence
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0030']
amended-by: []
affects:
  - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
enforced-by: Engine.Tests/Http/DocumentEndpointTests.cs
---

# ADR-0090 — The HTTP answer of a save lists its warnings

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding E164 of the codebase review of 2026-11-06 (e): a save to a new name moves the document, and when the delete of the
old file fails, the old file stays and opens as a second document. TASK-0251 made the save give the warning
`W-IO-OLD-FILE-KEPT` in a list of the caller, and the desktop shows it on the status line. The HTTP answer of
`POST /document/save` (ADR-0030 item 4) has no field for a warning, so an agent saw a plain success. A new field changes the
shape of the answer on the wire, so the question went to the owner.

On 2026-11-08 the owner answered: add the field, with an ADR.

### Q1. The shape of the field

- **A.** The answer of a save has the list `warnings`: for each warning its `code`, its `message` and its `path` (or
  null). The list is empty when the save gives no warning. Only the answer of a save changes; `GET /document` and the open
  keep their shape.
- **B.** One field `warning`, null or one warning.

A gives each later warning of a save its place with no new change of the shape. **Recommended: A.**

## Decision

The owner asked for the field; the recommended shape A follows the form of `Diagnostic`.

1. **The answer (Q1 A).** `POST /document/save` gives `{ "name", "lengthUnit", "version", "kind", "warnings": [ { "code",
   "message", "path" } ] }` with HTTP 200 when the save succeeds. The list is empty when the save gives no warning. This
   amends ADR-0030 item 4.
2. **The warnings.** The list holds each warning that `DocumentSession.SaveAs` gives, today only `W-IO-OLD-FILE-KEPT`.
3. **No change of `Engine.Contracts`.** The record of the answer is in `Engine.Api.Http`.

## Consequences

**Good:** an agent sees the old file that stays, as the person sees it on the desktop.

**Bad:** a client that reads the answer of a save field by field gets one more field.

## History

- 2026-11-08 · TASK-0291 · The record, Accepted by the answer of the owner of 2026-11-08 (E164).
- 2026-11-08 · TASK-0291 · Shipped at v0.275.
