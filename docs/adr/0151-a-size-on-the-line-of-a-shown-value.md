---
id: 0151
title: A free size or a reference on the line of a value that the view shows does not show again
status: Accepted
topic: Clients, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0148', '0149']
amended-by: []
affects:
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.ImageTests/FreeSizeTests.cs
enforced-by: 3DEngine.ImageTests/FreeSizeTests.cs
---

# ADR-0151 — A free size or a reference on the line of a value that the view shows does not show again

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R22c (ADR-0149, v0.369) shows a length that no constraint fixes as "not fixed". The picture of the Bracket from the front,
taken on 2026-11-14 after R22c, showed "60.000 mm not fixed" inside the plate: the free far side of the plate lies behind its
near side, and from the front the two edges are one line, whose value 60.000 shows above the plate. The same holds for two
free sides on one line, and for a reference of a chain (ADR-0148).

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **One line, one number (amends ADR-0148 item 2 and ADR-0149 item 2).** Two edges are one line on the view when their ends
   differ only along the direction of the view, within 1e-6 of the length. A free size or a reference whose edge is one line
   with the edge of a value that shows in the view, or with the edge of a free size or a reference that the view already
   gives, does not show. The order of the lines of the sketch decides which one of two free sizes shows.
2. **No contract change.** The values are presentation (ADR-0007).

## Consequences

**Good:** a flat view gives one number for each line that it shows.

**Bad:** a free side behind a valued side shows only in a view where the two are not one line.

## History

- 2026-11-14 · TASK-0387 · The record, from the picture of the Bracket from the front after R22c, under the order of the
  owner of 2026-11-14.
