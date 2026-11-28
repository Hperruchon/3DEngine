using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Hosting;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// ADR-0092 item 2 (TASK-0326): the routes of the proposals of an agent.
//
//   POST   /proposals       { "summary": string, "commands": [envelope, ...] }  201 with the proposal
//   GET    /proposals       the open proposals, the oldest first
//   GET    /proposals/{id}  one open or kept proposal
//   DELETE /proposals/{id}  withdraws an open proposal
//   GET    /proposals/{id}/preview  the preview of the proposal on a fork of the Document (ADR-0185, TASK-0456)
//
// An envelope is the body of POST /commands. The post checks each envelope as POST /commands does: a known name and version,
// and parameters that bind. Nothing enters the log at the post. No route accepts or rejects: only the person in the window
// does, so an agent cannot accept its own proposal.
internal static class ProposalsEndpoint
{
    public static async Task<IResult> Post(HttpContext context, EngineHost host, DocumentFolder folder)
    {
        // ADR-0030 item 4: after an accepted open, the old document takes no proposal, as it takes no command.
        if (folder.OpenAccepted)
            return DocumentEndpoint.OpenPending();

        // Finding E17 of the codebase review of 2026-09-30, part 3 (R-0028, TASK-0481): an unknown charset gets HTTP 415.
        if (JsonRequest.Refusal(context.Request) is { } refused)
            return refused;

        ProposalRequest? body;
        try
        {
            body = await context.Request
                .ReadFromJsonAsync<ProposalRequest>(ApiJson.Options, context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return ApiErrorEnvelope.BadRequest(JsonRequest.Malformed("Malformed JSON", ex));
        }
        catch (InvalidOperationException)
        {
            return ApiErrorEnvelope.BadRequest("The body holds text that is not well-formed UTF-16.");
        }

        if (body is null)
            return ApiErrorEnvelope.BadRequest("Request body is required.");

        // Item 2: the summary is required and has at most 120 characters. The window shows it on one line, so it has no
        // control character.
        if (string.IsNullOrEmpty(body.Summary))
            return ApiErrorEnvelope.BadRequest("Required field missing: summary, a JSON string of 1 to 120 characters.");
        if (body.Summary.Length > Proposals.MostSummary)
            return ApiErrorEnvelope.BadRequest($"The summary has {body.Summary.Length} characters; the limit is {Proposals.MostSummary}.");
        if (body.Summary.Any(char.IsControl))
            return ApiErrorEnvelope.BadRequest("The summary holds a control character; the window shows it on one line.");

        if (body.Commands is not { } envelopes)
            return ApiErrorEnvelope.BadRequest("Required field missing: commands, a JSON array of 1 to 16 envelopes.");
        if (envelopes.Count is 0 or > Proposals.MostCommands)
            return ApiErrorEnvelope.BadRequest($"The proposal has {envelopes.Count} envelopes; a proposal has from 1 to {Proposals.MostCommands}.");

        // Finding E198 of the codebase review of 2026-11-09 (b) (TASK-0331): a command identifier that an earlier envelope of
        // the proposal holds is refused. Until TASK-0331 the bus gave the cached result of the first one for the second one,
        // and the proposal counted two applied commands for one step of undo.
        var commands = new List<(ProposedCommand, Command)>(envelopes.Count);
        var first = new Dictionary<Guid, int>();
        for (var i = 0; i < envelopes.Count; i++)
        {
            var (built, refusal) = Build(host, envelopes[i]);
            if (refusal is not null)
                return Refused(i, refusal);
            var commandId = built!.Value.Item1.CommandId;
            if (!first.TryAdd(commandId, i))
                return Refused(i, $"The commandId {commandId} is the commandId of commands[{first[commandId]}]; each command of a proposal has its own identifier.");
            commands.Add(built.Value);
        }

        // Item 4: the proposal keeps the version of the Document at the post. It enters the queue inside the read, so each
        // later commit marks it (Proposals.OnEvent). The function of the read takes only the lock of the queue. ADR-0185
        // item 3: the queue copies the log there for the first preview.
        var summary = body.Summary;
        var added = await host.Session
            .Read((document, _) => host.Proposals.Add(summary, commands, document), context.RequestAborted)
            .ConfigureAwait(false);
        if (added is null)
            return Failure(
                DiagnosticCodes.ApiProposalQueueFull,
                $"The queue holds {Proposals.MostOpen} open proposals. Wait for the person, or withdraw a proposal.",
                StatusCodes.Status409Conflict);

        context.Response.Headers.Location = $"/proposals/{added.Id}";
        return Results.Json(added, ApiJson.Options, statusCode: StatusCodes.Status201Created);
    }

    public static IResult List(EngineHost host) => Results.Json(host.Proposals.Waiting(), ApiJson.Options);

    public static IResult Get(string id, EngineHost host)
        => Guid.TryParse(id, out var key) && host.Proposals.Find(key) is { } proposal
            ? Results.Json(proposal, ApiJson.Options)
            : NotFound(id);

    // ADR-0185 item 3 (TASK-0456): the preview of an open or kept proposal. The route waits for a preview of the version of the
    // Document at the request, or for the last preview of a closed proposal. A host with no backend for a fork gives HTTP 501.
    // TASK-0461 (finding E240 of the codebase review of 2026-11-23): a proposal that leaves the queue while the request waits
    // gives HTTP 404, and a stop of the host gives HTTP 503 with E-API-HOST-STOPPED. Until TASK-0461 both escaped the route
    // as an exception with no error envelope.
    public static async Task<IResult> Preview(string id, EngineHost host, HttpContext context)
    {
        if (!host.Proposals.MakesPreviews)
            return Failure(
                DiagnosticCodes.ApiNotSupported,
                "This host makes no fork of the Document, so it gives no preview of a proposal (ADR-0185 item 3).",
                StatusCodes.Status501NotImplemented);
        if (!Guid.TryParse(id, out var key))
            return NotFound(id);
        ProposalPreview? preview;
        try
        {
            preview = await host.Proposals.Preview(key, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            return Failure(
                DiagnosticCodes.ApiHostStopped,
                $"The host stopped while the request waited for the preview of the proposal {key}.",
                StatusCodes.Status503ServiceUnavailable);
        }

        return preview is null ? NotFound(id) : Results.Json(preview, ApiJson.Options);
    }

    public static IResult Withdraw(string id, EngineHost host)
    {
        if (!Guid.TryParse(id, out var key))
            return NotFound(id);
        var (withdrawn, state) = host.Proposals.Withdraw(key);
        if (state is null)
            return NotFound(id);
        // Finding E199 of the codebase review of 2026-11-09 (b) (TASK-0331): the refusal names the state of the proposal. Until
        // TASK-0331 the text of a proposal that applies said that it was closed.
        return withdrawn is null
            ? Failure(
                DiagnosticCodes.ApiProposalClosed,
                state == ProposalStates.Applying
                    ? $"The proposal {key} is not open: its state is applying, because the person accepted it and its commands apply now. Read it with GET /proposals/{key}."
                    : $"The proposal {key} is not open: its state is {state}. Read it with GET /proposals/{key}.",
                StatusCodes.Status409Conflict)
            : Results.Json(withdrawn, ApiJson.Options);
    }

    // One envelope as POST /commands checks it: the envelope, its handler, and the bind of its parameters (ADR-0016). The
    // handler builds the command at the post, so an accept applies the command that the post checked.
    private static ((ProposedCommand, Command)? Built, string? Refusal) Build(EngineHost host, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return (null, "The envelope is not a JSON object.");

        CommandsEndpoint.CommandRequest? envelope;
        try
        {
            envelope = element.Deserialize<CommandsEndpoint.CommandRequest>(ApiJson.Options);
        }
        catch (JsonException ex)
        {
            return (null, JsonRequest.Malformed("Malformed envelope", ex));
        }
        catch (InvalidOperationException)
        {
            return (null, "The envelope holds text that is not well-formed UTF-16.");
        }

        if (envelope is null)
            return (null, "The envelope is null.");
        if (string.IsNullOrEmpty(envelope.Name))
            return (null, "Required field missing: name.");
        if (envelope.SchemaVersion is not { } version)
            return (null, "Required field missing: schemaVersion.");
        if (!host.CommandRegistry.TryFind(envelope.Name, version, out var handler))
            return (null, $"No handler registered for command '{envelope.Name}'@{version}.");

        var bound = ParameterBinder.Bind(handler.Parameters, JsonParameters.AsRaw(envelope.Parameters));
        if (!bound.IsSuccess)
            return (null, bound.FirstMessage);

        var commandId = envelope.CommandId ?? Guid.NewGuid();
        var command = handler.Create(new CommandInput(bound.Values!, commandId, envelope.ExpectedDocumentVersion));
        var parameters = envelope.Parameters ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        return ((new ProposedCommand(handler.CommandName, handler.SchemaVersion, parameters, commandId, envelope.ExpectedDocumentVersion), command), null);
    }

    // Item 2: a refused envelope gives HTTP 400 with E-API-BAD-REQUEST and the index of the envelope.
    private static IResult Refused(int index, string message)
        => Results.Json(
            new ProposalErrorEnvelope(new ProposalErrorBody(DiagnosticCodes.ApiBadRequest, $"commands[{index}]: {message}", index)),
            ApiJson.Options,
            statusCode: StatusCodes.Status400BadRequest);

    private static IResult NotFound(string id)
        => Failure(DiagnosticCodes.ApiProposalNotFound, $"No open or kept proposal has the identifier '{id}'.", StatusCodes.Status404NotFound);

    private static IResult Failure(string code, string message, int status)
        => Results.Json(new ApiErrorEnvelope(new ApiErrorBody(code, message)), ApiJson.Options, statusCode: status);

    // A member that the body does not name is refused, as CommandRequest refuses one (finding E16).
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed record ProposalRequest(string? Summary, List<JsonElement>? Commands);

    // The error of a refused envelope: the envelope of ApiErrorEnvelope with the index of the envelope.
    internal sealed record ProposalErrorEnvelope(ProposalErrorBody Error);

    internal sealed record ProposalErrorBody(string Code, string Message, int Index);
}
