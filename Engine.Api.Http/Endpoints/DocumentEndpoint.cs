using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Core;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// ADR-0030 item 4: the document file of the session, for an agent (ADR-0002).
//
//   GET  /document                     { name, lengthUnit, version }
//   POST /document/save { "name": n }  saves the file as n.3de in the document folder
//   POST /document/open { "name": n }  only on a host that can start itself again
internal static class DocumentEndpoint
{
    public static async Task<IResult> Get(EngineHost host, CancellationToken ct)
    {
        var (version, unit) = await host.Session.Read((document, _) => (document.Version, document.LengthUnit), ct);
        return Results.Json(new DocumentState(host.FileName, unit, version), ApiJson.Options);
    }

    public static async Task<IResult> Save(HttpRequest request, EngineHost host, DocumentFolder folder, CancellationToken ct)
    {
        var (path, refused) = await Target(request, folder, ct);
        if (refused is not null)
            return refused;

        var error = await host.Session.SaveAs(path!, ct);
        return error is null
            ? Results.Json(new DocumentState(Path.GetFileName(path!), host.Session.LengthUnit, null), ApiJson.Options)
            : Failure(error.Code, error.Message, StatusCodes.Status409Conflict);
    }

    public static async Task<IResult> Open(HttpRequest request, DocumentFolder folder, CancellationToken ct)
    {
        if (folder.Reopen is not { } reopen)
            return Failure(
                DiagnosticCodes.ApiNotSupported,
                "This host does not open a file over HTTP; start it with --file (ADR-0030 item 4).",
                StatusCodes.Status501NotImplemented);

        var (path, refused) = await Target(request, folder, ct);
        if (refused is not null)
            return refused;

        var error = await reopen(path!, ct);
        return error is null
            ? Results.Json(new DocumentState(Path.GetFileName(path!), null, null), ApiJson.Options, statusCode: StatusCodes.Status202Accepted)
            : Failure(error.Code, error.Message, StatusCodes.Status409Conflict);
    }

    // The path in the document folder of the name in the body, or the reply of a refusal.
    private static async Task<(string? Path, IResult? Refused)> Target(HttpRequest request, DocumentFolder folder, CancellationToken ct)
    {
        string? name;
        try
        {
            using var body = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct);
            name = body.RootElement.ValueKind == JsonValueKind.Object
                && body.RootElement.TryGetProperty("name", out var field)
                && field.ValueKind == JsonValueKind.String
                    ? field.GetString()
                    : null;
        }
        catch (JsonException ex)
        {
            return (null, ApiErrorEnvelope.BadRequest($"Malformed JSON: {ex.Message}"));
        }
        // A name that is not well-formed UTF-16 gave HTTP 500 until TASK-0099; this is the
        // form of E73 in these routes (finding E80 of the second codebase review of 2026-10-28).
        catch (InvalidOperationException)
        {
            return (null, ApiErrorEnvelope.BadRequest("The field name is not well-formed UTF-16 text."));
        }

        if (name is null)
            return (null, ApiErrorEnvelope.BadRequest("Required field missing: name, a JSON string."));

        var path = folder.Resolve(name, out var refusal);
        return path is null ? (null, ApiErrorEnvelope.BadRequest(refusal!)) : (path, null);
    }

    private static IResult Failure(string code, string message, int status)
        => Results.Json(new ApiErrorEnvelope(new ApiErrorBody(code, message)), ApiJson.Options, statusCode: status);

    internal sealed record DocumentState(string? Name, string? LengthUnit, long? Version);
}
