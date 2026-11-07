---
id: 0272
title: The phone, the reader, the APK check, the gates and the roadmap hold their next forms
status: Done
phase: P0.112
opened: 2026-11-07
depends-on: [0271]
governed-by: [0002, 0004, 0007, 0016, 0017, 0018, 0019, 0023, 0025, 0029, 0061, 0072, 0073, 0075, 0076, 0077, 0079, 0080, 0081, 0082, 0083, 0085]
writes:
  create:
    - tasks/TASK-0272-phone-engine-and-gate-forms-of-the-twenty-ninth-review.md
  modify:
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.Android/3DEngine.Android.csproj
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.App/StlOpening.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - Engine.Cli/Cli.cs
    - Engine.Tests/Export/StlExportTests.cs
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - docs/adr/0079-the-activity-may-start-again.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - Engine.Core/Import/StlReader.cs
    - Engine.Tests/Import/StlImportTests.cs
    - eng/android/check-apk.sh
    - eng/android/check-platforms.js
    - .github/workflows/ci.yml
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/AffectsCitationGateTests.cs
    - docs/templates.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0272 — The phone, the reader, the APK check, the gates and the roadmap hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (b) found E175 and T168 (medium) and E176 to E179 and T169 to T173 (low). By
`docs/templates.md`, section 7, they come after the next product phase.

## Goal

A lost surface keeps a chosen STL, a stale session frees its host, the APK check fails when it cannot read, the reader
gives the codes of ADR-0083 item 3, the dialog of the phone starts free, the host of the phone treats CA1416 as an error,
the gates read the next forms, the two questions of TASK-0263 are in the register, and the roadmap lists its versions in
order.

## Scope (in)

1. E175 to E179.
2. T168 to T171 and T173. T172 is corrected in the entry of the review.
3. Two leads of Codex, checked on disk: a blank folder of the command line ends the program (v0.250), and a very large
   file can end the session of the phone before the limit of triangles (the review draft after v0.243, N2).

## Scope (out)

- The desktop findings (TASK-0271).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0112-phone-engine-gates-b) · v0.259

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  T168: the host makes CA1416 an error with WarningsAsErrors, not TreatWarningsAsErrors, because the Android
            SDK can give warnings that are not ours. E178: a first byte that is not text in the first 80 bytes and is
            not zero is a name that is not UTF-8. N2: the limit of the bytes before the read is 84 + 512 times the
            limit of triangles. E175, E176 and E179 change the phone and the window, which run on no computer here.
            The register stays full: the question of the affects rule for the ADRs before 0082 needs the owner to
            close an entry.
Weakest:    E175, E176 and E179 change the phone and the window, which run on no computer here, so no test holds them;
            the register stays full, so the question of the affects rule for the ADRs before 0082 has no entry.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07 (b).
- 2026-11-07: E175 to E179, T168 to T171, T173 and two leads of Codex. The tests of the reader, the blank folder and the
  absent ADR failed with the code of the base; the test of the size did not compile before. Injections: a SuppressMessage
  over two lines, TreatWarningsAsErrors under a Condition and a shader that cites ADR-0099 each fail their gate; a call
  that Android does not have in the host gives error CA1416; the APK check fails with no manifest and with no node.
- 2026-11-07: rebased on the close of P0.111; run 42367668204 passed, with the APK build and the step of the platforms.
  Closed at v0.259.
