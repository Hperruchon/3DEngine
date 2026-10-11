---
date: 2026-10-12
commit: 4a4450d
ledger: v0.64
previous: 2026-10-11-codebase-review.md
---

# Codebase review — 2026-10-12

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the seventh codebase review. The next review must come before ledger entry v0.71: the build fails
when a seventh milestone follows v0.64. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds that
rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `4a4450d`, the merge of v0.64 |
| Branch of this review | `codebase-review-2026-10-12` |
| Tests | 353 on this computer |
| Pipeline | Run 38182903570 on `main`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** Four tasks changed code after `de35f51`, the commit of the last review: TASK-0061 (the
judge), TASK-0062 (the hosts), TASK-0064 (the gates as a program) and TASK-0028 (the tessellation).
Three review agents worked in parallel, each one in its own git worktree at `4a4450d`: the gates with
the judge and the pipeline, the engine with the hosts, and the research for R3. The agents ran probes
in scratch copies and removed each one. I read each line that this review cites, on disk, and I ran
again the two merges of T42 and T43 with the judge built from `4a4450d`, and the first half of T41. A
line that says "a review agent ran it" means that I read the cited code and did not repeat the run.

## Summary

- **Thirteen findings of the last review are fixed, and four more in part.** E48, E52 to E56, T36,
  T38, T39 and T40 in full, and E49, E50 and E51 for the case that they named. E13, E57, T31 and T37
  are fixed in part. **[Observed]**
- **The judge and the gates from `main` can still be replaced or skipped (T41, T42, T44, high).** A
  `global.json` of the judged checkout chooses the SDK that builds the judge and the gates of `main`,
  because the job runs `dotnet build` in that checkout. A merge can undo a fix of `main` with no
  judgement. Files that are not gate files can switch the gates to the build of the judged commit and
  control its packages. T41 is a defect of my own TASK-0064 step and of the older step of the judge.
  **[Observed]** in runs of a review agent and, for T41 in part and for T42, in my runs.
- **Three gates miss forms that compile (T45, T46) and the runner drops forms (T47).** **[Observed]** in
  runs of a review agent.
- **The tessellation of TASK-0028 holds.** A review agent ran 200,000 tessellations with no leak signal,
  the extreme sizes, and 16 tasks in parallel on one session, with no defect. **[Observed]**
- **The hosts have three small gaps (E58, E59, E60, low).** An open HTTP request holds the stop for 30
  s, a port that the system refuses ends a host with a crash, and the surface writes indented JSON.
  **[Observed]** in runs of a review agent.
- **R3 needs four decisions of the owner.** No shader tool runs in the pipeline, and no test project
  may reference the render side. Question Q2 gives the options.
- **Two owner tasks, no new register entry.** TASK-0066 holds the judge, the pipeline and the gates, and
  TASK-0067 holds the hosts. The register stays at 14 of 15.
- **Sixty-four findings are open: no critical, ten high, twenty-eight medium and twenty-six low.** Ten
  findings are new. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A
source line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives
6,503 production lines in 105 files at `de35f51`, which are the values of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 12 | `.csproj` lines in `3DEngine.sln`. The last review gave 10; TASK-0064 added `RepoGates` and `WriteSetCheck`. Git tracks 13 project files: the native package is not in the solution. |
| Production source lines | 7,018 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, 110 files. The last review gave 6,503 in 105 files. |
| Test source lines | 8,739 | `Engine.Tests`, 69 files. The last review gave 8,124 in 65 files. |
| Tests | 353 | `dotnet test 3DEngine.sln --no-build`: 353 passed, 0 failed, 0 skipped. The last review gave 321. |
| Gate classes | 17 | Test classes whose name ends in `GateTests`. Fourteen of them also run in `eng/repo-gates`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`. This review adds none. The last review gave 13; TASK-0062 added R-0039. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19 (12 accepted, 7 amended). |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 10 | The same method: V1, V2, V3, V5, T1, T4, T31, T41, T42, T44. The last review gave 8. |
| Findings: medium | 28 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, V4, V6, T2, T5, T10, T16, T17, T28, T32, T37, T43, T45, T46. The last review gave 29. |
| Findings: low | 26 | The same method: E17, E24, E26, E29 to E32, E34 to E39, E46, E57, E58, E59, E60, V7, T7, T13, T23, T25, T47, C1, P1. The last review gave 30. |

## Findings of the previous review

Each line gives the state at `4a4450d` and the evidence. "No change" means that no file that the
finding cites changed after `de35f51`, the commit of the previous review.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. The backend choice moved into `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs`; each stops when the native library does not load.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:44` is unchanged. Register entry R-0027.
- **E6** — Open. No change. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. E27, E28 and E30 stay open.
- **E8** — Fixed. TASK-0044, v0.37. TASK-0059 changed the method of its test, and the defect still fails it.
- **E9** — Fixed. TASK-0034, v0.39. No change.
- **E10** — Fixed. TASK-0035, v0.43. E26 stays open.
- **E11** — Open, and wider. `Engine.Core/Hosting/ParameterBinder.cs:67-76` is unchanged. A review agent applied `CreateBox` with `sizeX` "Infinity" over HTTP: Applied; `GetBoundingBox` on that body then gave HTTP 500, because the JSON writer cannot write an infinity, and `GetTessellation` gave 0 triangles and no error. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Fixed in part. TASK-0028, v0.64: both hosts read each result as an object, and a failed query gives `"result": null` over HTTP and on the command line (a review agent ran both). A typed caller in the same process still gets an `Aabb` of six zeros for an absent body, and the desktop uses typed calls. Register entry R-0028.
- **E14** — Open. `Engine.Geometry.Manifold/Native/ManifoldSolidHandle.cs` and `GetBoundingBox` still give memory of `Marshal.AllocHGlobal` to a C++ `delete`. TASK-0028 used the destructor and `Marshal.FreeHGlobal` for the new mesh, and `manifold_destruct_manifold` and `manifold_destruct_box` are exported, so the fix needs no native build. A review agent read the import table of `manifoldc.dll` (the C runtime heap) and ran 100,000 create and dispose cycles with no crash. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. TASK-0058 and TASK-0038 moved and extended the checks.
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. TASK-0062 changed `Engine.Api.Http/Endpoints/EventsEndpoint.cs` but not the cited parts: its comment still says 1003 (line 18), and it sends 1007 (line 130). Register entry R-0028.
- **E18** — Open. `Engine.Core/CommandBus.cs` still promises a reset in the comment of the commit. Register entry R-0033.
- **E19** — Open. No change. Register entry R-0033.
- **E20** — Fixed. TASK-0051, v0.48. TASK-0058 gave its two limits in the comment (E42, E43).
- **E21** — Open. No change. Register entry R-0033.
- **E22** — Open. No change. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. TASK-0058 closed E40 and E41.
- **E24** — Open in seven parts of nine. TASK-0062 added `Subscriber.Stop` and changed no cited part; the constructor still starts the heartbeat. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs` still registers the bus of a replay. Register entry R-0036.
- **E26** — Open. `Engine.Contracts/Document.cs` still returns the list itself. Register entry R-0036.
- **E27** — Open. No change. Register entry R-0036.
- **E28** — Open. No change. Register entry R-0036.
- **E29** — Open. No change. Register entry R-0036.
- **E30** — Open. No change. Register entry R-0036.
- **E31** — Open. Each cited comment is still present. Register entry R-0036.
- **E32** — Open, and wider. No change in the cited code, and `Engine.Core/Queries/GetTessellationQueryHandler.cs` also has no catch: a `ManifoldNativeException` from the backend leaves the bus (reading only). Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open. No change. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Open in part. Exit code 4 is documented (`Engine.Api.Http.Server/Program.cs:15-17`, `3DEngine/Documentation/Running.md`), but other failures of the start still end each host with a crash (E59). Register entry R-0036.
- **E37** — Open. No change. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open, and worse. The backend choice now exists three times: `Engine.Cli/Cli.cs:214`, `Engine.Api.Http.Server/ServerEngine.cs:33` and `3DEngine/DesktopSurface.cs:80`. Register entry R-0036.
- **E40** — Fixed. The start check reads each address with `BindingAddress.Parse`, the parser of Kestrel, and the two forms with a user part failed the test before the correction. TASK-0058, v0.56.
- **E41** — Fixed. The endpoint configuration does not reload, and a process test that writes `appsettings.json` after the start failed before the correction. TASK-0058, v0.56.
- **E42** — Fixed as recommended. The comment in `Engine.Core/DocumentSession.cs` gives the limit and `ExecutionContext.SuppressFlow`. TASK-0058, v0.56.
- **E43** — Fixed as recommended. The same comment gives the limit and the rule for a sink; no code holds the rule. TASK-0058, v0.56.
- **E44** — Fixed. The bus refuses a created handle that it created before or that the same command consumes, and the three cases failed before the correction. TASK-0058, v0.56.
- **E45** — Fixed. The three texts agree with the code. TASK-0058, v0.56.
- **E46** — Open. TASK-0055 (P0.22).
- **E47** — Fixed. The glossary term "live body" says that the order is not history. TASK-0058, v0.56.
- **V1** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V2** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V3** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V4** — Open in each of its five parts. `3DEngine/DesktopEngineHost.cs` still writes a window failure to standard output and returns 0, and the desktop still draws the fixed sample scene. Register entry R-0027.
- **V5** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V6** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V7** — Open in each part. The frame loop still uses `DateTime.UtcNow`, and no source calls `SDL_DestroyWindow` or `SDL_Quit`. A review agent searched. Register entry R-0030.
- **T1** — Open in one part of three. No gate compares the native output of two platforms. R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. TASK-0057 kept the rules in the judge.
- **T4** — Open in three parts of four. The dispatch gate still misses the desktop host (`Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20`), the marker gate accepts the identifier of a closed register entry, and the determinism part is open again (T45). Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. The job of T6 no longer exists (TASK-0057).
- **T7** — Open in four parts of five. No change. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. T16 stays open, and T41 and T44 show other inputs of a build outside the gate files.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51.
- **T12** — Fixed. The heartbeat test counts frames, the deadlock test starts its clock after the host is up, and the collection "real host" runs alone; injected waits failed the old tests and passed the new ones. TASK-0059, v0.55. Register entry R-0034 is closed.
- **T13** — Open in part. The part about `git diff-tree --cc` is fixed, because TASK-0057 removed `--cc` from the judge. `eng/report-failed-tests.js:54` still reads `UnitTestResult` only. R-0031.
- **T14** — Fixed. TASK-0053, v0.51 and v0.52. T41 is a new path around "the judge comes from `main`".
- **T15** — Fixed. TASK-0050, v0.47.
- **T16** — Open, and wider (T46). `Engine.Tests/Governance/DependencyDirectionGateTests.cs:112` still skips each import with `$(`. Register entry R-0031.
- **T17** — Open. No change. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46.
- **T19** — Fixed. TASK-0049, v0.46.
- **T20** — Fixed. TASK-0049, v0.46.
- **T21** — Fixed. TASK-0049, v0.46.
- **T22** — Fixed. TASK-0049, v0.46.
- **T23** — Open. No change. Register entry R-0031.
- **T24** — Fixed. The remaining part, the statement about `--cc`, went with `--cc` (TASK-0057).
- **T25** — Open in part. With no git the judge now fails (`eng/write-set-check/Program.cs:20-23`), so that part is fixed. The check before a commit still reads the index and HEAD, and a change that is not staged gives a misleading message. R-0031.
- **T26** — Fixed. The contract rule is a rule of the judge for each commit, and the job "Contract-touched-needs-ADR" is gone; a test of a scratch repository failed before the correction. TASK-0057, v0.54.
- **T27** — Fixed. The process test asserts that no address was bound, and a probe that bound before an injected refusal failed it. TASK-0058, v0.56.
- **T28** — Fixed in part. The condition of the workflow file is accepted (R-0038). T41 lets a file of the judged commit choose the SDK that builds the judge, with no change to the workflow. R-0031.
- **T29** — Fixed. The judge runs git by a full path from `PATH` outside the checkout, with `-C`. TASK-0057, v0.54.
- **T30** — Fixed. A commit with no parent fails, and a merge is judged against its first parent; two tests of a scratch repository failed before the correction. TASK-0057, v0.54.
- **T31** — Fixed in part. TASK-0064, v0.63: the fourteen gate classes that read only files run in `eng/repo-gates` from a build of `main` (a review agent saw 70 runs of 65 checks on the checkout). The three gates that run engine code stay in the test process: a review agent put a module initializer into `Engine.Core` and `DispatchSurfaceGateTests` passed with `"CreateBox"` in `Engine.Cli`; the four texts are read only under `Engine.Tests/`. R-0031.
- **T32** — Fixed in part. A branch is judged from its merge base with `main`. On `main` the job judges the commits of the push only (`.github/workflows/ci.yml:199`), so a green later push still hides a red earlier push there. R-0031.
- **T33** — Fixed. The judge reads the task files at the commit. TASK-0057, v0.54.
- **T34** — Fixed. The documented check joins its two commands with `&&`. TASK-0057, v0.54.
- **T35** — Fixed. A failed git command is a failure, the judge reads the paths with `-z`, `WriteSetRangeTests` covers the steps, a task file with no front matter at the commit fails, and `docs/templates.md` describes the new steps. TASK-0057, v0.54.
- **C1** — Open in two parts. `IBRepOps.cs` and `IFeatureIdMap.cs` remain, and no production code reads `BackendCapabilities`; TASK-0028 added a fifth flag. Register entry R-0027.
- **P1** — Open. Nothing is measured in the repository. TASK-0028 records one measure in its task file: 90,012 triangles in 26.1 ms and 4.4 MB of JSON. Register entry R-0027.
- **E48** — Fixed. TASK-0062, v0.62. A review agent compared 49 forms with the parser of Kestrel: no form that the check accepts binds a foreign address or each interface.
- **E49** — Fixed for a WebSocket subscriber. TASK-0062, v0.62: a review agent measured 0.00 s before the subscribe frame and 0.02 s for the desktop with one subscriber. An open HTTP request still holds the stop (E58).
- **E50** — Fixed by reading. TASK-0062, v0.62: the loop of `3DEngine/DesktopEngineHost.cs` ends on `Stopping`. Nobody sent Ctrl+C to a desktop process.
- **E51** — Fixed for a port in use. TASK-0062, v0.62: exit code 4 from each host (a review agent ran both). A port that the system refuses is E59.
- **E52** — Fixed. TASK-0062, v0.62. A review agent saw the backend disposed after `Dispose` with no request.
- **E53** — Fixed. TASK-0062, v0.62. The clause is gone (`Engine.Core/CommandBus.cs:184`), and a test gives one handle two times.
- **E54** — Fixed. TASK-0062, v0.62. A review agent ran `--port`, `--port=` and `Desktop__Port`; the variable `PORT` has no effect.
- **E55** — Fixed. TASK-0062, v0.62. The messages say "The surface".
- **E56** — Fixed. TASK-0062, v0.62. `EngineHost.Backend` is internal.
- **E57** — Open in part. TASK-0062 corrected the text of ADR-0019 and the comment of the test. No test calls `DesktopSurface`; register entry R-0039.
- **T36** — Fixed. TASK-0061, v0.61. A review agent removed the union and two tests failed. T42 is a nearby form.
- **T37** — Open in part. TASK-0061 added the tests of the staged check and of unrelated history. A failed read of a test file and the steps of the workflow still have no test (a review agent turned the failure into a skip, and no test failed). R-0031.
- **T38** — Fixed. TASK-0063, v0.60: the progress line of R-0031, and R-0038 is accepted.
- **T39** — Fixed. TASK-0061, v0.61. The fallback left `.github/workflows/ci.yml`.
- **T40** — Fixed. TASK-0061, v0.61. The test gap of a failed read of a test file is in T37.

## 1 Current architecture

**[Observed]** The topology of ADR-0019 is unchanged: the surface library `Engine.Api.Http`, mounted by
the program `engine-api-http` and by the desktop host `3DEngine`. A client reads the triangles of a body
from the query `GetTessellation`, which only the native backend implements (ADR-0018). Both hosts hold
each query result as an object.

**[Observed]** Two programs judge each commit from a build of `main`: the judge of the write set
`eng/write-set-check`, and `eng/repo-gates`, which compiles the fourteen gate classes that read only
files and runs each check on the judged checkout. The three gates that run engine code stay tests.

**[Observed]** The desktop still draws the fixed sample scene. No pipeline, no shader and no vertex
buffer exist in `3DEngine.Vulkan`; the frame clears to green in a render pass with no draw.

## 2 Patterns in use

- **Each test fails first, or an injection shows that it can fail.** Each task of this period records
  it. **[Observed]**
- **A check from `main` that the judged checkout still feeds.** T41 and T44 are each an input of the
  build (the SDK, the packages) that the checkout gives to a program of `main`. TASK-0053 and TASK-0064
  moved the code of the checks; they did not move the inputs of the build. **[Observed]**
- **A gate that reads text where the compiler reads structure.** T45 (calls) and T46 (imports) are the
  fourth and fifth time that a gate of text misses a form that compiles (T4, T16). **[Observed]**

## 3 Review of the code

The earlier findings keep their headings and their evidence in the earlier reviews. The section
"Findings of the previous review" gives the state of each one. This section gives the new findings.

### The hosts and the engine

#### E58 · Low · An open HTTP request holds the stop of a host for 30 s · Confirmed by a run

No host sets `HostOptions.ShutdownTimeout` (no match in `Engine.Api.Http`, `Engine.Api.Http.Server` or
`3DEngine`), so Kestrel waits for an open request until its default limit. A review agent mounted the
surface with the steps of the desktop: a client that sent part of its headers held `StopAsync` for
30.01 s, and a POST with no body for 30.00 s; a subscriber before its first frame took 0.00 s. Any local
client can therefore keep the desktop process alive for 30 s after the window closes (inferred for the
process). **[Observed]** **[Recommended]** A short limit in both hosts, and a test with a partial
request. Owner: TASK-0067.

#### E59 · Low · A port that the system refuses ends each host with a crash · Confirmed by a run

`Engine.Api.Http.Server/Program.cs:54` and `3DEngine/DesktopSurface.cs:94` catch only an `IOException`
whose inner exception is `AddressInUseException`, and `3DEngine/DesktopEngineHost.cs:16` has no `try`
around the start. A review agent started `engine-api-http` on port 6006, which Windows excludes on this
computer: exit code -532462766 and an unhandled `SocketException` (10013). A `Kestrel:Endpoints` value
with the name `localhost` and port 0 makes the start of the desktop throw. A port under 1024 on Linux or
macOS gives the same crash (inferred). **[Observed]** **[Recommended]** Catch each failure of the start,
give it a documented exit code, and dispose the app. Owner: TASK-0067.

#### E60 · Low · The surface writes indented JSON, which makes a tessellation about 2.4 times larger · Confirmed by a run

`Engine.Api.Http/Json/ApiJson.cs:16` sets `WriteIndented = true`, so each number of an array gets a line.
A review agent tessellated a body of 25,612 triangles: 1,199,168 bytes with the options of the surface,
and 507,578 bytes compact. ADR-0018 accepts a JSON array for version 1; it does not ask for the indent.
**[Observed]** **[Recommended]** Compact JSON for each HTTP response; the command line keeps its own
indented output. Owner: TASK-0067.

### Tests, gates and the pipeline

#### T41 · High · A `global.json` of the judged checkout chooses the SDK that builds the judge and the gates of main · Confirmed by a run

- **Evidence.** `.github/workflows/ci.yml:172` and `:174` run `dotnet build` on the projects of the
  worktree of `main` with no `working-directory`, so the current folder is the judged checkout. The
  `dotnet` command reads `global.json` of the current folder to find the SDK, and in .NET 10 its field
  `sdk.paths` can name a folder inside the checkout. I put a `global.json` with `paths` and
  `errorMessage` into an empty folder and ran `dotnet build` on the judge project from there: the
  command printed my message and did not build. A review agent went further: a planted `dotnet.dll` in
  that folder ran in place of the SDK, with the arguments `build ... -o out`, and could therefore write
  any program into the output folder of the judge.
- **Impact.** A commit with a `global.json` and a small folder replaces the judge and the gates of
  `main`. `global.json` is not a gate file, so the write set does not stop it. This is a new path
  around T14. On the Linux runner the planted folder also needs a link to the shared frameworks
  (inferred).
- **[Recommended]** Run each build and each run of a program of `main` with a `working-directory`
  outside the checkout, and make `global.json` a gate file. Owner: TASK-0066.

#### T42 · High · A merge can undo a change of its first parent in a file that its second parent changed · Confirmed by a run

`eng/write-set-check/Judge.cs:245-266` judges a merge on the union of its `--cc` set and its own change
minus the files that its second parent brought. When the merge takes the version of the second parent
for a file that both parents changed, the file leaves both sets: `--cc` drops it because the result
equals one parent, and the second set drops it because the second parent changed it. A review agent
made such a merge: `main` corrected line 6 of a file, the side changed line 1, and the merge took the
side version with no task. I ran the script again with the judge built from `4a4450d`: "pass ... (a
merge with no change of its own)", exit code 0, and the correction of `main` is gone. A careless "take
theirs" does the same, so no deliberate act is needed. **[Observed]** **[Recommended]** Compute the
automatic merge (`git merge-tree --write-tree`) and judge each file where the merge differs from it,
and each file with a conflict. Owner: TASK-0066.

#### T43 · Medium · A clean merge after `main` moved fails the judge · Confirmed by a run

When both parents changed one file, `--cc` lists it although the merge holds no edit of its own, and
each task changes `docs/CURRENT-STATE.md`. The merge then needs a governing task, and the task that it
names must change its file (`Judge.cs:257-266`). A review agent made such a merge; I ran it again with
the judge of `4a4450d`: "FAIL ... The commit names TASK-0002 and does not change its file", exit code
1. No merge of the history has this form yet, because `main` did not move while a branch was open. The
merge of a pull request would fail in the same way (inferred). **[Observed]** **[Recommended]** The
remedy of T42. Owner: TASK-0066.

#### T44 · High · Files that are not gate files switch the gates to the judged commit and control its packages · Confirmed by a run

The switch of the step "Run the repository gates" (`.github/workflows/ci.yml:229`) matches each path that
contains `/Directory.Build.` or `/Directory.Packages.`, for example `docs/Directory.Packages.txt`, and
`Judge.IsGateFile` (`eng/write-set-check/Judge.cs:90-91`) accepts only the real names. A change of such a
file therefore needs no exact permit and makes the job build the gates from the judged commit. That
build reads `nuget.config`, `global.json` and the folder `nuget/`, which are not gate files: a review
agent set a `globalPackagesFolder` inside the checkout in `nuget.config`, and the restore of
`eng/repo-gates` took `xunit.assert` from that folder. A planted `xunit.assert` whose `Assert` never
fails then passes each gate (inferred; the agent did not build that assembly). **[Observed]**
**[Recommended]** One list of the inputs of a build (`global.json`, `nuget.config`, `nuget/**`, each
`Directory.*` file), read by `IsGateFile` and by the switch. Owner: TASK-0066.

#### T45 · Medium · The determinism gate misses eight forms that compile · Confirmed by a run

`Engine.Tests/Governance/DeterminismCallGateTests.cs:33-43` matches a call as text: `Math.` and a name
and `(`, after it removes each `//` to the end of a line. A review agent added a file to `Engine.Core`
with `Double.Sin`, `double.SinPi`, `Math . Cos`, an alias `M.Exp`, `Math.@Tan`,
`double.ReciprocalSqrtEstimate`, the method group `Math.Log`, and `Math.Atan` after a string that holds
`//`. The copy built, and the gate from the program of `main` passed; `Math.Sin(x)` failed it.
**[Observed]** **[Recommended]** Read the called methods from the compiled assemblies of the log path,
with no load, against a list of forbidden methods. Owner: TASK-0066.

#### T46 · Medium · The dependency gate misses an import with a wildcard and a `.csproj.user` file · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:109-116` follows an import only when the file
exists by its exact name, and it never reads a `.csproj.user` file; MSBuild imports both. A review agent
added `<Import Project="build*.props"/>` to `Engine.Core` and a `.csproj.user` file to
`Engine.Contracts`, each with a reference to `3DEngine.Core`: the gates passed, and `dotnet msbuild
-getItem:ProjectReference` listed the reference for both projects. **[Observed]** **[Recommended]** Read
the evaluated references, or refuse each import that the gate cannot resolve. Owner: TASK-0066.

#### T47 · Low · The runner of the gates skips forms with no failure · Confirmed by a run

`eng/repo-gates/Program.cs:35` reads public instance methods that a public class declares, `:57-59`
counts a skip as no failure, `:66` reads `[InlineData]` rows only when the theory has one, and `:82`
awaits a `Task` and not a `ValueTask`. A review agent built the program with probe gates: a static
check, a check in a nested class or an internal class, an inherited check and a row of `[MemberData]`
did not run, and a failing `ValueTask` gave "pass". Each gate of today has the supported form, so no
check is lost now. **[Observed]** **[Recommended]** Fail on each form that the runner cannot run, and
count a skip as a failure. Owner: TASK-0066.

## 4 The next planned task

**[Observed]** The roadmap gives R3: "A pipeline. A triangle, then an indexed mesh." No task file for R3
exists; `tasks/TASK-0027-first-party-vulkan-layer.md` names R3 only as later work. **[Recommended]**
TASK-0066 comes first as P0.26 (question Q1), because T41 and T44 let a commit replace the checks that
each later task of the batch relies on.

## 5 Research for that task

**[Observed]** `3DEngine.Vulkan` has an instance (API 1.3), a device that needs a window surface, a
swapchain with a render pass of one colour attachment and no depth, one command buffer and one fence
for each frame in flight, and a hook `RenderFrame`. It has no pipeline, no shader module, no vertex
buffer and no helper for a memory type; ADR-0017 says that R3 writes those helpers again.
`Vortice.Vulkan` 3.2.3 holds the calls that R3 needs.

**[Observed]** The pipeline installs no Vulkan SDK, no driver and no shader compiler on the three
runners. This computer has the Vulkan SDK 1.3.290 (`glslc`, `spirv-val`) and two GPUs, and no software
driver. No test can draw a triangle in the pipeline today.

**[Observed]** No test project may reference the render side: `DependencyDirectionGateTests` refuses a
reference from each `Engine.*` project to `3DEngine.Core` (lines 163-173) and from each project other
than `3DEngine` to `3DEngine.Vulkan` (lines 318-328). ADR-0009, line 68, permits `Engine.Tests` to
reference `3DEngine.Core` for a test of the render kernel. The decision and the gate disagree; no such
test exists, so nothing fails today. Question Q2 asks the owner which one is correct.

**[Observed]** Register entries R-0022 and R-0030 hold the defects of the Vulkan layer on Linux, on
macOS and on a second computer (V1 to V3, V5 to V7). They do not stop a first triangle on the computer
of the owner. **[Inferred]**

## 6 Recommended approach

1. **[Recommended]** TASK-0066: T41 first (a `working-directory` outside the checkout, and `global.json`
   a gate file), then T44 (one list of the inputs of a build), then T42 and T43 (the automatic merge),
   then T45 to T47.
2. The task of R3, with the answers of the owner to Q2. Its first commit writes the decisions as an
   amendment of ADR-0017, and of ADR-0009 if the owner chooses option A of Q2 (c).
3. TASK-0067 (E58 to E60) when a host task opens, or after R6. Each of its findings is low.

## Questions for the owner

**Q1. Must TASK-0066 come before R3?** T41 and T44 let a commit replace the judge or the gates of
`main`, and T42 lets a merge undo a fix of `main`. Option A: TASK-0066 first, as P0.26, about 2 to 3
evenings. Option B: R3 to R6 first, and TASK-0066 after R6. Recommendation: option A, because each task
of the batch R2 to R6 relies on the checks, and the fix of T41 is a few lines.

**Q2. Four decisions for R3.** Each one goes into an amendment of ADR-0017 in the first commit of R3.

- (a) **How the SPIR-V of a shader is made.** Option A: commit the GLSL source and the compiled
  SPIR-V, and a test checks the header of the SPIR-V and a hash of the source, so that a change of the
  source with no new compile fails. Option B: compile at run time with a shaderc package, which adds a
  native dependency on three platforms. Option C: install a compiler on each runner. Recommendation:
  option A.
- (b) **Render pass or dynamic rendering.** Option A: keep the render pass that exists. Option B:
  dynamic rendering, which needs a feature that the device does not turn on today. Recommendation:
  option A, the smaller change.
- (c) **Where the tests of the render side live.** Option A: a new test project `3DEngine.Tests` with no
  GPU, which may reference `3DEngine.Core` and `3DEngine.Vulkan`, in a new class of the dependency gate;
  ADR-0009 line 68 then names that project, and `Engine.Tests` stays free of the render side, as the gate
  says. Option B: correct the gate to permit `Engine.Tests` to reference `3DEngine.Core`, as ADR-0009
  says. Recommendation: option A.
- (d) **Which platform R3 shows.** Option A: R3 shows the triangle and the mesh on the computer of the
  owner, who looks at the window; the defects of R-0022 and R-0030 wait for a run on Linux or macOS.
  Option B: R3 also corrects those defects. Recommendation: option A, because the roadmap puts the
  image test in R6.
