---
id: 0481
title: Four parts of finding E17, the text of a refused body, two closes of the event stream and an option as a name
status: Active
phase: P0.195
opened: 2026-11-28
depends-on: [0480]
governed-by: [0002, 0005, 0010, 0016, 0018, 0019, 0020, 0083, 0092, 0111, 0159, 0160, 0161, 0185]
writes:
  create:
    - tasks/TASK-0481-four-parts-of-e17.md
    - Engine.Api.Http/Json/JsonRequest.cs
    - Engine.Tests/Http/RequestTextTests.cs
    - Engine.Tests/Http/EventsEndpointKestrelTests.cs
  modify:
    - Engine.Api.Http/Errors/ApiErrorEnvelope.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - Engine.Api.Http/Endpoints/QueriesEndpoint.cs
    - Engine.Api.Http/Endpoints/ProposalsEndpoint.cs
    - Engine.Api.Http/Endpoints/EventsEndpoint.cs
    - Engine.Api.Http/WebSockets/Subscriber.cs
    - Engine.Api.Http/WebSockets/SubscriberOptions.cs
    - Engine.Cli/Cli.cs
    - Engine.Tests/Cli/CliUsageTests.cs
    - docs/adr/0005-event-stream-and-replay.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0481 — Four parts of finding E17, the text of a refused body, two closes of the event stream and an option as a name

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

After P0.194 the next rows of the roadmap wait for the owner: R34 and R35 for a check on the phone, and P11a for the answers to
its proposal (TASK-0479). Register entry R-0028 holds finding E17 of the codebase review of 2026-09-30. Four of its open parts
need no new diagnostic code and no change of `Engine.Contracts`: part 3 (the text of a JSON exception and an unknown charset),
part 5 (the pump stops before the close frame), part 6 (no time limit on the subscribe frame) and part 9 (a flag read as a
command name). The review marked parts 5, 6 and 9 "reported by a review agent, not checked". The standing order of 2026-11-28
runs until 09:00 on 2026-11-29, so this task does them.

## Goal

A refused body names no internal type, an unknown charset gets HTTP 415, a lagged client of Kestrel reads its close, a missing
subscribe frame gets a close, and an option in the place of a name is a usage error.

## Scope (in)

1. Part 3: one check of the content type and its charset, and one text of a refused body, for `/commands`, `/queries` and
   `/proposals`.
2. Part 5: the pump of a subscriber sends the close frame before it cancels the receive loop.
3. Part 6: the subscribe frame has a time limit of 10 seconds (ADR-0005 section 5), and a missing frame gets the close of a
   malformed frame.
4. Part 9: `engine apply` and `engine query` refuse an option in the place of the name.

## Scope (out)

- E17 parts 1 and 2, which need new diagnostic codes, and E13 of R-0028, which needs a change of `Engine.Contracts`.
- `DocumentEndpoint`, whose reader (`JsonDocument`) gives a text that names no .NET type.
- The stop of the host (`Subscriber.Stop`), which cancels before the pump ends; finding E49 has its own test.

## Acceptance criteria

- [x] Each part has a test that fails when its correction is undone.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0195-host-findings) · v0.464

## Method

Mechanical: the text of each part and the code on disk.
Judgement:  the time limit is 10 seconds, because a client sends the frame at once after the upgrade; a missing frame takes the
            close of a malformed frame, because `E-API-WS-INVALID-SUBSCRIBE` already says "missing", so no code is new; the
            endpoint does not cancel the receive at the time limit, because a cancelled receive aborts a socket of Kestrel.
Weakest:    the time limit is a new rule of ADR-0005 section 5 that the owner did not see; a client that is slow for 10 seconds
            before its frame now gets a close. The wait of at most 2 seconds for the answer of the client to a close has no
            test that fails without it: one full run failed with no wait, and 25 runs of the test alone passed with no wait.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after P0.194. `JsonRequest.Refusal` gives HTTP 415 for an unknown
  charset and `JsonRequest.Malformed` gives the place of the fault and no type; the pump closes first and cancels second;
  `SubscriberOptions.SubscribeTimeout` is 10 seconds; an option in the place of a name exits with 2. A run of the old close
  order on Kestrel gave "The remote party closed the WebSocket connection without completing the close handshake", so part 5
  was a fault. Each of five injections made its tests fail. The first form cancelled at once after the close frame, and one of
  two full runs failed the lag test of Kestrel with the same broken connection: on Windows an abort can discard a close frame
  that the client did not read. The pump and the endpoint now wait for the answer of the client, at most 2 seconds
  (`Subscriber.CloseWait`); 25 runs of the socket tests and three full runs passed. Ledger v0.464.
