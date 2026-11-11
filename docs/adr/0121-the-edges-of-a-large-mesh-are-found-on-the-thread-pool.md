---
id: 0121
title: The edges of a mesh of more than 100 000 triangles are found on the thread pool
status: Accepted
topic: Rendering, clients
date: 2026-11-11
supersedes: []
superseded-by: []
amends: ['0109']
amended-by: []
affects:
  - 3DEngine.App/SessionBodies.cs
  - 3DEngine.Vulkan/EdgeLines.cs
  - 3DEngine.ImageTests/BodyEdgesImageTests.cs
enforced-by: 3DEngine.ImageTests/BodyEdgesImageTests.cs
---

# ADR-0121 — The edges of a mesh of more than 100 000 triangles are found on the thread pool

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0109 item 1 finds the feature edges of a mesh one time, when the mesh goes to the GPU. Item 5 drops the lines of a mesh
with more than 100 000 feature edges. The codebase review of 2026-11-11 gives finding V167: `SessionBodies.AddMesh` finds the
edges first and compares their count with the limit after. A probe of `EdgeLines.Feature` gave 270 ms for 980 000 triangles
and 620 ms for 2 000 000. The upload runs on the thread of the frame, so a large imported mesh stops the window for that
time, at each upload and at each rebuild, also when it draws no line.

The count of the feature edges is known only at the end of the search. A count of triangles does not bound it: a fine
cylinder of one million triangles has few feature edges and draws them. So a limit on the triangles before the search
would drop true lines. The review recommends the thread pool as the simplest correct form.

The same review gives finding V166: a triangle of zero area across a T-vertex adds its long side as an edge with one
triangle, so a flat face can show a false line.

## Decision

1. **The count of the upload.** A mesh with at most 100 000 triangles (`EdgeLines.MostTrianglesAtUpload`) finds its edges at
   the upload, as before. From the measure of the review, that search takes about 28 ms at the largest count.
2. **A larger mesh.** A mesh with more triangles goes to the GPU with no lines, and the search of its edges runs on the
   thread pool. At the start of a later frame, when the search is complete, the mesh goes to the GPU again with its lines,
   and the old mesh is disposed after the device is idle. A search that a frame starts gives its lines to a later frame,
   also when it is complete before the end of that frame. The limit of ADR-0109 item 5 applies to the result, as before.
3. **A rebuild or a removal.** When a body loses its mesh, the search of that mesh is dropped, and its result is not used.
   So a rebuild that finds the new edges at the upload does not get the lines of the old mesh.
4. **The camera and the scene.** The second upload changes no entity, no box and no count of meshes, so the camera does not
   frame again (ADR-0029 item 8).
5. **A triangle of zero area (finding V166).** A triangle whose normal is zero adds no edge. A sliver along a T-vertex has
   three different corners, but its long side is not an edge of the surface; before this record it was a false line.
6. **No contract change, no engine change.** The lines are presentation (ADR-0007). The result of the search does not depend
   on the thread, so each platform draws the same lines.

## Consequences

**Good:** a large mesh does not stop the window for the search of its edges. A mesh that draws no lines no longer costs a
search on the thread of the frame.

**Bad:** a large mesh shows its lines some frames after its faces. The second upload copies the triangles to the GPU again
and waits for an idle device. The search still runs, now away from the frame; a mesh with more than 100 000 feature edges
spends that time and draws no line. The time of the upload in the window and on the phone is not measured (R-0061).

## Alternatives considered

- **A limit on the triangles before the search.** It is fast, but it drops the true lines of a fine mesh with few creases.
- **A faster search on the thread of the frame.** A search with less allocation can be some times faster, but a mesh of
  some millions of triangles still stops the window.
- **A buffer of lines that a mesh gets later.** It saves the second copy of the triangles, but it changes `GpuMesh` and the
  draw of each body for a case that is rare.

## History

- 2026-11-11 · TASK-0345 · The record, from finding V167 of the codebase review of 2026-11-11 and the judgement of the
  session.
- 2026-11-11 · TASK-0345 · Shipped at v0.332.
