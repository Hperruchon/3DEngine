## Summary

<!-- One paragraph: what changed and why. Reference the TASK or ADR being advanced. -->

## TASK

<!-- Path to tasks/TASK-####-*.md and current Status.
     A separate close commit flips Status from Ready to Done with the
     impl commit hash (see TASK-0001..0005 for the pattern). -->

## Boundaries (CLAUDE.md)

- [ ] `Engine.Contracts/**` unchanged — or — a new/updated ADR in this PR explains the change.
- [ ] No new `E-`/`W-`/`I-` diagnostic codes — or — `docs/diagnostics.md` updated in the same PR.
- [ ] No changes under `3DEngine/`, `BlazorApp/`, `Vortice.Vulkan.*` outside their stated purpose. (`3DEngine.Core/` is a peer render kernel per ADR-0009 and may be modified within its render-side scope.)
- [ ] Dependency direction holds: `Engine.Contracts` ← `Engine.Core` ← clients; `Engine.*` does not reference `3DEngine.Core` and vice versa.

## Verification

- [ ] `dotnet build` green
- [ ] `dotnet test` green
- [ ] `docs/CURRENT-STATE.md` reflects what shipped (if a milestone shipped)
- [ ] `docs/roadmap.md` updated (Pending → Shipped, if a phase shipped)
