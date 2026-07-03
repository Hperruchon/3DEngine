using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
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
        Command? command;
        IResult? parameterError;

        switch (body.Name)
        {
            case "NoOp":
                command = BuildNoOp(body, commandId, out parameterError);
                break;
            case "CreateBox":
                command = BuildCreateBox(body, commandId, out parameterError);
                break;
            default:
                command = null;
                parameterError = null;
                break;
        }

        if (parameterError is not null)
            return parameterError;

        CommandResult result;
        if (command is not null)
        {
            result = await host.CommandBus.Apply(command, context.RequestAborted).ConfigureAwait(false);
        }
        else
        {
            // Same rationale as Engine.Cli/Cli.cs §Apply: Command is abstract
            // and sentinels are forbidden, so the API surfaces the existing
            // E-CMD-UNKNOWN diagnostic directly without dispatching.
            result = new CommandResult(
                CommandId: commandId,
                CommandName: body.Name,
                Status: CommandStatus.Rejected,
                AppliedAtSeq: null,
                DocumentVersion: host.Document.Version,
                Outputs: Outputs.Empty,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: new ErrorDetail(
                    DiagnosticCodes.CommandUnknown,
                    $"No handler registered for command '{body.Name}'."),
                DurationMs: 0);
        }

        return Results.Json(result, ApiJson.Options);
    }

    private static Command? BuildNoOp(CommandRequest body, Guid commandId, out IResult? error)
    {
        if (body.Parameters is null
            || !body.Parameters.TryGetValue("echo", out var echoElement))
        {
            error = ApiErrorEnvelope.BadRequest("NoOp requires parameters.echo.");
            return null;
        }

        if (echoElement.ValueKind != JsonValueKind.String)
        {
            error = ApiErrorEnvelope.BadRequest("NoOp parameter 'echo' must be a string.");
            return null;
        }

        error = null;
        return new NoOpCommand
        {
            CommandId = commandId,
            ExpectedDocumentVersion = body.ExpectedDocumentVersion,
            Echo = echoElement.GetString()!,
        };
    }

    private static Command? BuildCreateBox(CommandRequest body, Guid commandId, out IResult? error)
    {
        if (body.Parameters is null)
        {
            error = ApiErrorEnvelope.BadRequest("CreateBox requires parameters.sizeX, sizeY, sizeZ.");
            return null;
        }

        if (!TryReadDouble(body.Parameters, "sizeX", out var sx, out error)) return null;
        if (!TryReadDouble(body.Parameters, "sizeY", out var sy, out error)) return null;
        if (!TryReadDouble(body.Parameters, "sizeZ", out var sz, out error)) return null;

        error = null;
        return new CreateBoxCommand
        {
            CommandId = commandId,
            ExpectedDocumentVersion = body.ExpectedDocumentVersion,
            SizeX = sx,
            SizeY = sy,
            SizeZ = sz,
        };
    }

    private static bool TryReadDouble(
        Dictionary<string, JsonElement> parameters,
        string key,
        out double value,
        out IResult? error)
    {
        value = 0;
        if (!parameters.TryGetValue(key, out var element))
        {
            error = ApiErrorEnvelope.BadRequest($"Required parameter missing: {key}.");
            return false;
        }
        if (element.ValueKind != JsonValueKind.Number)
        {
            error = ApiErrorEnvelope.BadRequest($"Parameter '{key}' must be a number.");
            return false;
        }
        if (!element.TryGetDouble(out value))
        {
            error = ApiErrorEnvelope.BadRequest($"Parameter '{key}' is not a valid number.");
            return false;
        }
        error = null;
        return true;
    }

    internal sealed record CommandRequest(
        string? Name,
        int? SchemaVersion,
        Dictionary<string, JsonElement>? Parameters,
        Guid? CommandId,
        long? ExpectedDocumentVersion);
}
