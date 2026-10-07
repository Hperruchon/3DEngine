---
id: 0051
title: A call back into the session fails at once, and the HTTP host refuses a foreign address from each source
status: Done
phase: governance
opened: 2026-10-08
depends-on: [0050]
governed-by: [0004, 0011, 0019]
writes:
  create:
    - tasks/TASK-0051-session-reentry-and-bound-addresses.md
    - Engine.Tests/DocumentSessionReentryTests.cs
  modify:
    - Engine.Core/DocumentSession.cs
    - Engine.Api.Http/Program.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - tasks/TASK-0038-hybrid-topology.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Geometry.Manifold/**
    - Engine.Cli/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0051 — A call back into the session fails at once, and the HTTP host refuses a foreign address from each source

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-04 found two defects that TASK-0038 must not ship with (register entry
R-0033). The review of 2026-10-08 confirmed both, and the owner put their correction before TASK-0037
in the order of 2026-10-08.

- **E20.** `DocumentSession` has one serial section and no check for a second entry. A sink or a read
  function that calls the session waits for the section that its own flow holds, and each other
  client waits behind it. TASK-0038 projects events in the desktop host, and a projection can ask a
  query when a body arrives.
- **E23.** `Engine.Api.Http/Program.cs` checks the key `urls` only. An address from
  `Kestrel:Endpoints` or from `ASPNETCORE_HTTP_PORTS` gets no refusal, and the surface has no
  authentication.

## Goal

A call into the session from inside its own section throws `InvalidOperationException` at once. The
HTTP host refuses a foreign address from each source of the configuration before it binds, and it
checks the bound addresses after the start.

## Scope (in)

1. `DocumentSession` refuses a call from the flow that holds the section, with a message that names
   the rule. The other callers wait as before.
2. `Program.ConfigurationRefusal` reads `urls`, each `Kestrel:Endpoints:<name>:Url`, `http_ports` and
   `https_ports`. A port key binds each interface, so a value in it is refused.
3. After the start, the host reads `IServerAddressesFeature` and stops with exit code 1 when a bound
   address is not a loopback address.
4. Each new test fails before its correction.
5. TASK-0038 depends on this task, so that the order is in a field that a gate reads.

## Scope (out)

- No change to `Engine.Contracts`, to a handler or to the lock order.
- No authentication on the surface. ADR-0019 section 5 keeps the surface on the loopback address.
- No new diagnostic code. A refusal of an address is a message and exit code 1, as in TASK-0044.

## Acceptance criteria

- [x] A sink that calls the session gets `InvalidOperationException`, and the next command and read
      run at once.
- [x] A read function that starts a query or a command gets `InvalidOperationException`, and nothing
      is applied.
- [x] The real host exits with code 1 and the loopback message for `--Kestrel:Endpoints:E1:Url=` with a
      foreign address and for `ASPNETCORE_HTTP_PORTS`.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.

## Outcome

Status: Done · v0.48 · the commit that carries this block.

## Method

**Mechanical.** `governed-by` comes from `TaskGovernanceGateTests` with the task open: ADR-0004,
ADR-0011 and ADR-0019. The change agrees with each one: ADR-0019 section 5 requires the loopback
address, which E23 enforces, and the session change holds no topology (ADR-0011 section 3). The tests
first. `ConfigurationRefusal` first kept the old behaviour (the key `urls`
only), so the first run showed the defect and not a compile error. Then the corrections, and the same
tests again.

**Judgement.** A token for each hold of the section, in an `AsyncLocal`, and not a flag. A flag that
flows into a task that the sink starts would refuse that task also after the section ends. With the
token, the task is refused only while the section is held, which is the time in which it would wait
for its own flow. Both checks of the host, before and after the start: the first one never binds a
foreign address from a known source, and the second one covers a source that the list does not know.
A port key is refused and not translated to a loopback address, because a quiet change of an address
hides a configuration error.

**Weakest.** No test reaches the check after the start with a real foreign bind, because that needs a
bind on each interface of the computer. An injection proved the path: with the bound address changed
from 127.0.0.1 to 192.0.2.1 in the check, the real host exited with code 1 and the loopback message,
and the real-host WebSocket test failed. The process case for `ASPNETCORE_HTTP_PORTS` did not run
before the correction, because that run binds each interface; its configuration case failed first. A
task that code in the section starts, and that waits for the section on purpose while the section is
held, is refused; the comment in `DocumentSession` gives the rule.

One local run of the full suite, directly after a clean build, ended with "test run aborted", and its
output is lost because the command sent it into `tail`. Twenty later runs passed, thirteen of them
with `--blame-crash`, and no run made a crash dump. The cause is not known. The new process tests are
the first candidates, because they start a host. R-0034 records it.

## Progress

- 2026-10-08: the task is open. The first run gave 8 failures in the 12 new tests that ran (the port
  case of the process test did not run, see "Weakest"). The four that passed test the parts that
  were already correct: the forms of a bound address, loopback in each source, and `urls`. The sink ran into its
  limit of two seconds (`OperationCanceledException`), each call that a read function started ran
  after the read, four configuration cases gave no refusal, and the host with a `Kestrel:Endpoints`
  address died with an unhandled exception (exit code `0xE0434352`) when it tried to bind.
- 2026-10-08: closed. The corrections are in. 273 tests pass, with zero warnings in a clean build.
