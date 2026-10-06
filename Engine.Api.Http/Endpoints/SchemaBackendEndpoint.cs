using Engine.Api.Http.Json;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /schema/backend
//
// The name and the version of the geometry backend that the host composed, for
// example "manifold" and "3.5.2" (TASK-0036). A client sees which backend answers
// before it sends a command. The host reads both values from the class that it
// built, so Engine.Contracts does not change.
internal static class SchemaBackendEndpoint
{
    public static IResult Handle(EngineHost host)
        => Results.Json(new BackendSchema(host.BackendName, host.BackendVersion), ApiJson.Options);

    internal sealed record BackendSchema(string Name, string Version);
}
