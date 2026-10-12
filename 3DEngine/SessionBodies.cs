using System.Collections.Concurrent;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// The projection of the live bodies of the session into the scene of the desktop
// (roadmap phase R5, ADR-0009, ADR-0024). It reads the live bodies and subscribes to
// the events of the session in one read of the session, so it misses no body. The
// action of the subscription runs inside each commit and only puts the body in a
// queue; the render loop calls Apply, which fetches each new mesh with the query
// GetTessellation outside the commit and removes each consumed body.
internal sealed class SessionBodies : IDisposable
{
    private readonly EngineHost _host;
    private readonly ConcurrentQueue<(bool Created, Guid BodyId)> _changes = new();
    private readonly IDisposable _subscription;

    public SessionBodies(EngineHost host)
    {
        _host = host;

        var (live, subscription) = host.Session.Read((document, _) =>
            (document.Bodies.Select(body => body.Handle.Id).ToArray(), host.Subscribe(OnEvent)))
            .GetAwaiter().GetResult();
        _subscription = subscription;
        foreach (var bodyId in live)
            _changes.Enqueue((true, bodyId));
    }

    // Inside the commit: no call of the session, and no wait.
    private void OnEvent(EventRecord record)
    {
        if (record.Kind is not ("body.created" or "body.consumed")
            || record.Payload is not IReadOnlyDictionary<string, object?> payload
            || payload.GetValueOrDefault("bodyId") is not Guid bodyId)
            return;

        _changes.Enqueue((record.Kind == "body.created", bodyId));
    }

    // On the render loop: one entity and one mesh for each live body. The mesh is
    // narrowed around the centre of its box, and that centre is the origin of the
    // entity, in double precision (ADR-0024 item 2).
    public void Apply(Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        while (_changes.TryDequeue(out var change))
        {
            var meshId = change.BodyId.ToString();
            if (change.Created)
            {
                var result = _host.Session.Query<Tessellation>(new GetTessellationQuery { BodyId = change.BodyId })
                    .GetAwaiter().GetResult();
                if (result.Error is { } error)
                {
                    Console.WriteLine($"[WARN] No mesh for body {change.BodyId}: {error.Code} {error.Message}");
                    continue;
                }

                var tessellation = result.Result!;
                if (tessellation.TriangleCount == 0)
                    continue;

                var centre = MeshVertex.Centre(tessellation.Positions);
                meshes[meshId] = new GpuMesh(device, MeshVertex.Narrow(tessellation.Positions, centre, 1.0), tessellation.Indices);
                scene.Entities.Add(new Entity { Name = $"Body {change.BodyId}", MeshId = meshId, Origin = centre });
            }
            else if (meshes.Remove(meshId, out var mesh))
            {
                var entity = scene.Entities.FirstOrDefault(candidate => candidate.MeshId == meshId);
                if (entity is not null)
                    scene.Entities.Remove(entity);

                // A frame in flight can still read the buffers of the mesh.
                device.WaitIdle();
                mesh.Dispose();
            }
        }
    }

    public void Dispose() => _subscription.Dispose();
}
