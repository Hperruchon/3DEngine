# TASK-0004 — Adopt ADR-0009 (`3DEngine.Core` as peer render kernel)

## Status

Ready

## Context

ADR-0009 declares `3DEngine.Core` a peer render kernel to `Engine.Core`. The ADR's §5 ("Migration") states: *"No code moves. The decision is documentation."* This task is the sized work unit for P3 (per `docs/roadmap.md`) and lands the doc-only changes that adopt the decision across CLAUDE.md, the ADR index, the current-state ledger, and the roadmap itself.

## Goal

Promote ADR-0009 to Accepted (date stamp) and update every doc that referenced the prior "pre-architecture / pending" framing of `3DEngine.Core` to reflect the new peer-kernel role. No code, test, or csproj changes.

## Scope (in)

1. **Flip ADR-0009 Status** from Proposed to Accepted with today's date.

2. **CLAUDE.md**:
   - Expand the Authority diagram to show `3DEngine.Core` as a peer render kernel alongside `Engine.Core`.
   - Replace the "`3DEngine.Core` is **pre-architecture**…" dependency-rule bullet with ADR-0009 §2's reference rules: zero project references for `3DEngine.Core`; `Engine.*` MUST NOT reference it; it MUST NOT reference `Engine.*`; render-capable clients additionally reference it.
   - Remove `3DEngine.Core/` from the "Do not touch" list.
   - Update the "No concrete geometry backend" V1 clamp to drop its stale reference to ADR-0009. The anchor for that clamp is a future ADR (target P7), not this one.
   - Leave the anti-pattern "Treating `3DEngine.Core` POCOs as canonical scene state" intact. ADR-0009 §3 (design truth vs render state) makes that anti-pattern more precise, not obsolete.

3. **`docs/adr/README.md`**:
   - Add the ADR-0009 row to the index (Status: Accepted; Topic: Boundary, clients).
   - Empty the Pending list (ADR-0009 was the only entry).

4. **`docs/CURRENT-STATE.md`**:
   - Remove the "Pending decisions: ADR-0009" line from the v0.1 entry.
   - Add a v0.4 entry referencing this task and ADR-0009.

5. **`docs/roadmap.md`**:
   - Move P3 from Pending V1 to Shipped (`P3 — 3DEngine.Core peer render kernel (ADR-0009). v0.4, TASK-0004.`).

## Scope (out)

- Any code change. Per ADR-0009 §5: no code moves.
- Any csproj change. `3DEngine.Core.csproj` is untouched; every `Engine.*.csproj` is untouched.
- Any test change. The existing test suite is unaffected; no new test is added in this phase.
- A migration TASK. Per the P3 roadmap entry, a migration TASK is required only "if folding follows" — folding is explicitly rejected by ADR-0009.
- Renaming `3DEngine.Core`. Out of scope per ADR-0009 Non-goals.
- Wiring the project-reference check that would mechanically enforce ADR-0009 §2. ADR-0009 itself notes that enforcement is out of phase scope; the existing dependency-direction gate slot (CLAUDE.md gates §3) covers it when a future task wires it.

## Inputs

- ADR-0009 — the decision being adopted.
- CLAUDE.md — sections targeted for update.
- `docs/adr/README.md`, `docs/CURRENT-STATE.md`, `docs/roadmap.md`.

## Outputs

- ADR-0009 Status: Accepted with today's date.
- CLAUDE.md no longer describes `3DEngine.Core` as pre-architecture / do-not-touch / pending; its authority diagram includes the peer kernel.
- ADR index lists ADR-0009; Pending list is empty.
- `docs/CURRENT-STATE.md` v0.1 entry no longer lists ADR-0009 as pending; v0.4 entry summarises this task.
- `docs/roadmap.md` lists P3 under Shipped.

## Files

**Created:**
- `docs/adr/0009-3dengine-core-peer-render-kernel.md` (the ADR — created in the same change).
- `tasks/TASK-0004-adopt-adr-0009.md` (this file).

**Modified:**
- `CLAUDE.md` — authority diagram, dependency rules, do-not-touch, V1 clamp.
- `docs/adr/README.md` — index row, Pending list.
- `docs/CURRENT-STATE.md` — drop pending decision from v0.1, add v0.4 entry.
- `docs/roadmap.md` — move P3 from Pending V1 to Shipped.

**Do not touch:**
- Any `.cs` file. The phase is doc-only.
- Any `.csproj` file. Reference rules in ADR-0009 §2 are documented; mechanical enforcement is a separate task.
- `docs/diagnostics.md`. No new codes.
- ADRs 0001–0008.

## Tests

No new tests. The existing 31-test suite continues to pass; `dotnet build` and `dotnet test` are run after the doc changes to confirm doc edits did not touch code by accident.

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes (31 tests, unchanged from v0.3).
3. ADR-0009 Status reads `Accepted — 2026-05-11`.
4. CLAUDE.md no longer contains the string "pre-architecture" or the "ADR-0009 (pending)" framing for `3DEngine.Core`.
5. CLAUDE.md "Do not touch" no longer lists `3DEngine.Core/`.
6. `docs/adr/README.md` lists ADR-0009 in the table with Status Accepted; the Pending section is empty.
7. `docs/CURRENT-STATE.md` v0.1 entry no longer says "Pending decisions: ADR-0009"; a v0.4 entry references this task and ADR-0009.
8. `docs/roadmap.md` lists P3 under Shipped, removed from V1 Pending.

## Notes for the implementer

- **Mechanical.** Every change is documentation. The architectural debate is resolved in the ADR; this task is execution.
- **Two-commit pattern.** Implementation commit lands all doc updates with this file at Status: Ready. Close commit flips Status to Done with the impl hash — matching the close pattern from v0.1 / v0.2 / v0.3.
- **The V1 clamp anchor.** The "No concrete geometry backend" clamp used to point at ADR-0009 because that ADR was the placeholder for "the next big architectural call." Now that ADR-0009 is settled on `3DEngine.Core`'s role (and not on the geometry backend), rephrase the clamp to anchor to the future ADR that will introduce a concrete backend (P7's territory).
