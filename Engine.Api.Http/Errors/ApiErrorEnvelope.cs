using Engine.Api.Http.Json;
using Engine.Core;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Errors;

// Wire shape for transport-level errors. Engine-level errors (E-CMD-UNKNOWN
// etc.) flow through CommandResult.Error; this envelope is only for problems
// the API rejects before reaching the bus (malformed JSON, missing required
// fields, etc.). See TASK-0007 §5.
internal sealed record ApiErrorEnvelope(ApiErrorBody Error)
{
    public static IResult BadRequest(string message)
        => Results.Json(
            new ApiErrorEnvelope(new ApiErrorBody(DiagnosticCodes.ApiBadRequest, message)),
            ApiJson.Options,
            statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFound(string message)
        => Results.Json(
            new ApiErrorEnvelope(new ApiErrorBody(DiagnosticCodes.ApiBadRequest, message)),
            ApiJson.Options,
            statusCode: StatusCodes.Status404NotFound);

    // Finding E17 of the codebase review of 2026-09-30 (R-0028, TASK-0321): a request that is not JSON gets
    // HTTP 415 with the envelope and the code E-API-BAD-REQUEST, which docs/diagnostics.md gives for a wrong
    // content type. Until TASK-0321 the response had an empty body.
    public static IResult UnsupportedMediaType()
        => Results.Json(
            new ApiErrorEnvelope(new ApiErrorBody(DiagnosticCodes.ApiBadRequest, "The request body must be JSON, with the content type application/json.")),
            ApiJson.Options,
            statusCode: StatusCodes.Status415UnsupportedMediaType);
}

internal sealed record ApiErrorBody(string Code, string Message);
