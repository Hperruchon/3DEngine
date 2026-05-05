# TASK-0003 — Diagnostics registry CI gate (P2)

## Status

Ready

## Context

CLAUDE.md states: *"Any `E-`/`W-`/`I-` code added to code must land in `docs/diagnostics.md` in the same change. No exceptions."* Today this rule is enforced by review only. The four seed codes (`E-CMD-UNKNOWN`, `E-CMD-VERSION-STALE`, `E-CMD-BUS-BUSY`, `E-QRY-UNKNOWN`) happen to be in sync because the same change in TASK-0001 added them to both code and registry. Once any other contributor — or a future task — introduces a code, the rule depends on memory.

CLAUDE.md also lists what "the gate" runs: `dotnet build`, `dotnet test`, dependency direction check, **diagnostic codes registered**, replay determinism fixture. This task implements the third item.

The repo has no `.github/workflows/`. "CI gate" in this phase means an executable check that runs under `dotnet test`, so it is enforced today on every developer's machine and tomorrow on whatever CI lands in P5.

## Goal

Add a test in `Engine.Tests` that:

- Walks every `.cs` source file under `Engine.Contracts/`, `Engine.Core/`, `Engine.Cli/` (excluding `bin/` and `obj/`).
- Extracts every token matching the diagnostic-code shape (`<E|W|I>-<SUBSYSTEM>-<tag>`).
- Parses `docs/diagnostics.md` and extracts every code listed in the Active codes table.
- Fails if any code found in source is not present in the registry.
- Fails with a message that names the missing code(s) and the source file(s) where they appear.

## Scope (in)

1. **Scanner (test-internal)**
   - Regex over file content: `\b[EWI]-[A-Z][A-Z0-9]*-[A-Z][A-Z0-9-]*[A-Z0-9]\b`. Matches the existing codes and the format documented in `docs/diagnostics.md` (`<severity>-<subsystem>-<short-tag>`, kebab uppercase).
   - Tag must end on a letter or digit; subsystem is one or more uppercase letters/digits beginning with a letter.
   - The scanner reads file text raw — comments, string literals, anywhere. Comments mentioning unregistered codes are also caught; that is desirable (no orphan references).

2. **Registry parser (test-internal)**
   - Read `docs/diagnostics.md`. Extract every backtick-wrapped token matching the same regex.
   - Returns a `HashSet<string>` of registered codes.

3. **Repo-root resolution**
   - From `AppContext.BaseDirectory`, walk parents until a directory containing `3DEngine.sln` is found. Throw with a clear message if not found within 10 levels.

4. **Source enumeration**
   - For each of `Engine.Contracts`, `Engine.Core`, `Engine.Cli` (relative to repo root), enumerate `*.cs` recursively, skipping any path whose segments contain `bin` or `obj`.
   - `Engine.Tests` is excluded — it contains the scanner itself plus tests that reference codes verbatim for assertions.
   - Other projects (`3DEngine`, `3DEngine.Core`, `BlazorApp`, `Vortice.Vulkan.*`) are excluded — they are outside the authority graph (per CLAUDE.md "Do not touch") and the diagnostic-code rule does not apply.

5. **Tests in `Engine.Tests/Diagnostics/`**
   - `DiagnosticsRegistryGateTests.All_Diagnostic_Codes_In_Engine_Sources_Are_Registered` — runs the full scan against the live tree. On failure, the assertion message lists each unregistered code and the source file(s) where it appears.
   - `DiagnosticsRegistryGateTests.Registry_Parser_Extracts_All_Seed_Codes` — parses `docs/diagnostics.md` and asserts the four seed codes are present.
   - `DiagnosticsRegistryGateTests.Scanner_Extracts_Code_From_Sample_Source` — runs the regex against a string containing `"E-FOO-BAR"` and asserts the code is extracted. Hermetic.
   - `DiagnosticsRegistryGateTests.Scanner_Ignores_Tokens_That_Do_Not_Match_Code_Shape` — asserts `E-foo-bar` (lowercase), `EE-CMD-X`, `E--CMD-X`, plain `CMD-X`, and `E-CMD-` are NOT extracted.
   - `DiagnosticsRegistryGateTests.Source_Enumeration_Skips_Bin_And_Obj` — sanity-checks that the enumeration result contains `Engine.Core/CommandBus.cs` and contains no path with a `bin` or `obj` segment.

## Scope (out)

- Any change to `Engine.Contracts/**`, `Engine.Core/**`, or `Engine.Cli/**`. The four existing codes already comply.
- Any new diagnostic code.
- A standalone scanner CLI (`dotnet run`-able). The test is the gate.
- Format checking on the registry itself (e.g. validating that codes follow the convention) — out of phase. The phase enforces direction *code → registry*.
- Detecting orphan codes (registered but unused). The append-only registry policy permits orphans.
- GitHub Actions workflow. Phase P5 covers CI infrastructure.
- Roslyn-based parsing. The regex is sufficient for the documented shape.
- Walking other projects (`3DEngine`, `3DEngine.Core`, `BlazorApp`, `Vortice.Vulkan.*`). They are not subject to the rule.
- Discovering codes in non-`.cs` files. The rule applies to source.

## Inputs

- CLAUDE.md — diagnostic codes rule, gate list, "do not touch" list, dependency direction.
- ADR-0008 §4 — diagnostic code stability and append-only policy.
- `docs/diagnostics.md` — current registry, format conventions, "Adding a code" steps.
- TASK-0001 / TASK-0002 — existing codes and where they are referenced.

## Outputs

- `dotnet test` runs the new test class as part of the existing suite.
- With the current tree, the gate test passes (the four seed codes are all registered).
- If a contributor adds `"E-NEW-CODE"` to any `Engine.Contracts`, `Engine.Core`, or `Engine.Cli` `.cs` file without updating `docs/diagnostics.md`, `dotnet test` fails with a message naming the code and the file(s) where it appears.
- `docs/CURRENT-STATE.md` v0.3 entry referencing this task.
- `docs/roadmap.md` updated: P2 moved from Pending to Shipped.

## Files

**Created:**
- `Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs`
- `Engine.Tests/Diagnostics/DiagnosticsScanner.cs` — internal static helpers: scanner, registry parser, repo-root finder, source enumerator.

**Modified:**
- `docs/CURRENT-STATE.md` — add v0.3 entry.
- `docs/roadmap.md` — move P2 from Pending to Shipped.
- `tasks/TASK-0003-diagnostics-registry-gate.md` — flip Status to Done on commit.

**Do not touch:**
- `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`.
- `docs/diagnostics.md` (no new codes in this task).
- `3DEngine/`, `3DEngine.Core/`, `BlazorApp/`, `Vortice.Vulkan.*`.
- ADRs.

## Tests

- `DiagnosticsRegistryGateTests.All_Diagnostic_Codes_In_Engine_Sources_Are_Registered`
- `DiagnosticsRegistryGateTests.Registry_Parser_Extracts_All_Seed_Codes`
- `DiagnosticsRegistryGateTests.Scanner_Extracts_Code_From_Sample_Source`
- `DiagnosticsRegistryGateTests.Scanner_Ignores_Tokens_That_Do_Not_Match_Code_Shape`
- `DiagnosticsRegistryGateTests.Source_Enumeration_Skips_Bin_And_Obj`

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — including all listed new tests, the existing TASK-0001 suite, and the existing TASK-0002 CLI suite.
3. The scanner walks only `Engine.Contracts/`, `Engine.Core/`, `Engine.Cli/`. No `bin/` or `obj/` paths in the enumeration result.
4. No file under `Engine.Contracts/**`, `Engine.Core/**`, `Engine.Cli/**`, or `docs/diagnostics.md` is modified.
5. No new diagnostic code is introduced.
6. `docs/CURRENT-STATE.md` lists v0.3 with this task id.
7. `docs/roadmap.md` lists P2 under Shipped, removed from V1 Pending.

## Notes for the implementer

- **Repo-root walker.** Tests run from `Engine.Tests/bin/Debug/net10.0/`. Walk parents until `3DEngine.sln` is found. Throw if not found within 10 levels — clearer failure than a `DirectoryNotFoundException` later.
- **Cross-platform paths.** Use `Path.DirectorySeparatorChar` and `Path.Combine`. Skip `bin`/`obj` by path-segment equality on `path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)`, not by substring match — a class named `binding.cs` should not be skipped.
- **Failure message ergonomics.** When the gate fails, list each unregistered code on its own line, followed by the source file(s) where it appears (relative to repo root). Example:
  ```
  Diagnostic code(s) used in source but not registered in docs/diagnostics.md:
    E-NEW-CODE
      Engine.Core/CommandBus.cs
      Engine.Cli/Cli.cs
  Add the code to docs/diagnostics.md before merging.
  ```
- **No Roslyn.** A regex on raw text is sufficient for the documented shape and is dependency-free. Comments and strings both match; that is intentional — the rule is "no orphan references".
- **Registry parser is regex-driven too.** Match `` `<code>` `` (backtick-wrapped) where `<code>` matches the code regex. The active-codes table is the only place codes appear in the registry; any backtick-wrapped match is a registration.
- **Hermeticity.** Three of the four scanner-shape tests run on in-memory strings. Only the gate test reads disk. Runtime impact on `dotnet test` is negligible.
- **`TreatWarningsAsErrors` is on.** Suppress no warnings in the new files.
