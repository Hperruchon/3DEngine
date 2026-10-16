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

    // The box of each live body that has a mesh, by the identifier of its mesh.
    private readonly Dictionary<string, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)> _boxes = new(StringComparer.Ordinal);

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
    // entity, in double precision (ADR-0024 item 2). It returns true when a body came
    // or went, so that the host can frame the bodies again (ADR-0025 item 6).
    public bool Apply(Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var changed = false;
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
                _boxes[meshId] = Box(tessellation.Positions);
                changed = true;
            }
            else if (meshes.Remove(meshId, out var mesh))
            {
                _boxes.Remove(meshId);
                changed = true;
                var entity = scene.Entities.FirstOrDefault(candidate => candidate.MeshId == meshId);
                if (entity is not null)
                    scene.Entities.Remove(entity);

                // A frame in flight can still read the buffers of the mesh.
                device.WaitIdle();
                mesh.Dispose();
            }
        }

        return changed;
    }

    // The box around each live body that has a mesh, or null when none has one.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Bounds
    {
        get
        {
            if (_boxes.Count == 0)
                return null;

            var boxes = _boxes.Values;
            return (
                (boxes.Min(b => b.Min.X), boxes.Min(b => b.Min.Y), boxes.Min(b => b.Min.Z)),
                (boxes.Max(b => b.Max.X), boxes.Max(b => b.Max.Y), boxes.Max(b => b.Max.Z)));
        }
    }

    private static ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Box(double[] positions)
    {
        (double X, double Y, double Z) min = (double.MaxValue, double.MaxValue, double.MaxValue);
        (double X, double Y, double Z) max = (double.MinValue, double.MinValue, double.MinValue);
        for (var index = 0; index + 2 < positions.Length; index += 3)
        {
            min = (Math.Min(min.X, positions[index]), Math.Min(min.Y, positions[index + 1]), Math.Min(min.Z, positions[index + 2]));
            max = (Math.Max(max.X, positions[index]), Math.Max(max.Y, positions[index + 1]), Math.Max(max.Z, positions[index + 2]));
        }

        return (min, max);
    }

    public void Dispose() => _subscription.Dispose();
}
