using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Core;
using Engine.Core.Assets;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// ADR-0044 item 4 (phase V1b, TASK-0147): the store of the assets of the account, for an agent
// (ADR-0002). The routes do not enter the document session: an asset is not a command, and the log
// does not change.
//
//   PUT /assets         the bytes of the body    { asset, size }
//   GET /assets/{name}  the bytes of the asset, application/octet-stream
internal static class AssetsEndpoint
{
    public static async Task<IResult> Put(HttpRequest request, AssetStore store, CancellationToken ct)
    {
        var (name, size, error) = await store.Put(request.Body, ct);
        return error is null
            ? Results.Json(new AssetState(name!, size), ApiJson.Options)
            : Failure(error.Code, error.Message);
    }

    public static async Task<IResult> Get(string name, AssetStore store, CancellationToken ct)
    {
        var (content, error) = await store.Read(name, ct);
        return error is null
            ? Results.Bytes(content!, "application/octet-stream")
            : Failure(error.Code, error.Message);
    }

    private static IResult Failure(string code, string message)
        => Results.Json(new ApiErrorEnvelope(new ApiErrorBody(code, message)), ApiJson.Options, statusCode: code switch
        {
            DiagnosticCodes.AssetTooLarge => StatusCodes.Status413PayloadTooLarge,
            DiagnosticCodes.AssetBadName => StatusCodes.Status400BadRequest,
            DiagnosticCodes.AssetNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        });

    internal sealed record AssetState(string Asset, long Size);
}
