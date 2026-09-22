using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Hosting;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// POST /commands handler.
//
// Body shape:
//   {
//     "name": string,                    // required
//     "schemaVersion": integer,          // required
//     "parameters": object,              // required, may be {}
//     "commandId": string?,              // optional UUID; generated if omitted
//     "expectedDocumentVersion": long?   // optional
//   }
//
// Response: HTTP 200 with CommandResult JSON when the engine answers.
// HTTP 4xx only for transport-level problems. See TASK-0007 §3 + §5.
internal static class CommandsEndpoint
{
    public static async Task<IResult> Handle(HttpContext context, EngineHost host)
    {
        if (!context.Request.HasJsonContentType())
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        CommandRequest? body;
        try
        {
            body = await context.Request
                .ReadFromJsonAsync<CommandRequest>(ApiJson.Options, context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return ApiErrorEnvelope.BadRequest($"Malformed JSON: {ex.Message}");
        }

        if (body is null)
            return ApiErrorEnvelope.BadRequest("Request body is required.");

        if (string.IsNullOrEmpty(body.Name))
            return ApiErrorEnvelope.BadRequest("Required field missing: name.");

        if (body.SchemaVersion is null)
            return ApiErrorEnvelope.BadRequest("Required field missing: schemaVersion.");

        var commandId = body.CommandId ?? Guid.NewGuid();

        // Per ADR-0016: find the handler, bind, then let the handler build its
        // command. No command name appears in this file.
        if (!host.CommandRegistry.TryFind(body.Name, body.SchemaVersion.Value, out var handler))
        {
            // Same rationale as Engine.Cli/Cli.cs: Command is abstract and
            // sentinels are forbidden, so the API surfaces the existing
            // E-CMD-UNKNOWN diagnostic directly without dispatching.
            var unknown = new CommandResult(
                CommandId: commandId,
                CommandName: body.Name,
                Status: CommandStatus.Rejected,
                AppliedAtSeq: null,
                DocumentVersion: host.Document.Version,
                Outputs: Outputs.Empty,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: new ErrorDetail(
                    DiagnosticCodes.CommandUnknown,
                    $"No handler registered for command '{body.Name}'@{body.SchemaVersion}."),
                DurationMs: 0);
            return Results.Json(unknown, ApiJson.Options);
        }

        var bound = ParameterBinder.Bind(
            handler.Parameters,
            JsonParameters.AsRaw(body.Parameters));

        if (!bound.IsSuccess)
            return ApiErrorEnvelope.BadRequest(bound.FirstMessage);

        var command = handler.Create(
            new CommandInput(bound.Values!, commandId, body.ExpectedDocumentVersion));

        var result = await host.CommandBus
            .Apply(command, context.RequestAborted)
            .ConfigureAwait(false);

        return Results.Json(result, ApiJson.Options);
    }

    internal sealed record CommandRequest(
        string? Name,
        int? SchemaVersion,
        Dictionary<string, JsonElement>? Parameters,
        Guid? CommandId,
        long? ExpectedDocumentVersion);
}
