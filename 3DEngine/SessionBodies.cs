using System.Collections.Concurrent;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// One live body as the desktop holds it: its identifier, its kind, its name and its
// visibility (ADR-0028), for the panel and the selection.
internal sealed record LiveBody(Guid Id, string Kind, string? Name, bool Visible);

// The projection of the live bodies of the session into the scene of the desktop
// (roadmap phase R5, ADR-0009, ADR-0024). It reads the live bodies and subscribes to
// the events of the session in one read of the session, so it misses no body. The
// action of the subscription runs inside each commit and only puts the change in a
// queue; the render loop calls Apply, which fetches each new mesh with the query
// GetTessellation outside the commit, removes each consumed or removed body, and applies each
// change of a name or a visibility (body.changed, ADR-0028 item 7).
//
// A hidden body keeps its mesh and its triangles, but its entity leaves the scene, so the
// desktop does not draw it, cannot pick it, and leaves it out of the box that the camera
// frames (ADR-0029 item 3).
internal sealed class SessionBodies : IDisposable
{
    private enum Change
    {
        Created,
        Consumed,
        Changed,
    }

    private readonly EngineHost _host;
    private readonly ConcurrentQueue<(Change Change, Guid BodyId, string Kind, string? Name, bool Visible)> _changes = new();
    private readonly IDisposable _subscription;

    // Each live body, in the order of its creation, for the panel of the user interface
    // (ADR-0027 item 7). It holds a body also when its mesh failed.
    private readonly List<LiveBody> _live = new();

    // The entity, the box and the triangles of each live body that has a mesh, by the
    // identifier of its mesh. The entity of a hidden body is not in the scene.
    private readonly Dictionary<string, Entity> _entities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)> _boxes = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, (double[] Positions, uint[] Indices)> _triangles = new();
    private readonly BodyVisibility _visibility = new();

    public SessionBodies(EngineHost host)
    {
        _host = host;

        // The live bodies enter the queue inside the read, before the subscription, so
        // each later event comes after them. Until TASK-0074 they entered after the
        // read, behind newer events (finding E61 of the codebase review of 2026-10-13).
        _subscription = host.Session.Read((document, _) =>
        {
            foreach (var body in document.Bodies)
                _changes.Enqueue((Change.Created, body.Handle.Id, body.Kind, body.Name, body.Visible));
            return host.Subscribe(OnEvent);
        }).GetAwaiter().GetResult();
    }

    // Inside the commit: no call of the session, and no wait.
    private void OnEvent(EventRecord record)
    {
        if (record.Payload is not IReadOnlyDictionary<string, object?> payload
            || payload.GetValueOrDefault("bodyId") is not Guid bodyId)
            return;

        var name = payload.GetValueOrDefault("name") as string;
        var visible = payload.GetValueOrDefault("visible") is not false;
        switch (record.Kind)
        {
            case "body.created":
                _changes.Enqueue((Change.Created, bodyId, payload.GetValueOrDefault("kind") as string ?? string.Empty, name, visible));
                break;
            // ADR-0031 item 11: a body that an Undo removed leaves the scene as a consumed body does.
            case "body.consumed":
            case "body.removed":
                _changes.Enqueue((Change.Consumed, bodyId, string.Empty, null, false));
                break;
            case "body.changed":
                _changes.Enqueue((Change.Changed, bodyId, string.Empty, name, visible));
                break;
        }
    }

    // Each live body, as the last call of Apply left them.
    public IReadOnlyList<LiveBody> Live => _live;

    // The count of live bodies that are visible and have a mesh.
    public int VisibleCount => _visibility.VisibleCount;

    // The triangles of each live body that has a mesh, with its visibility, for a pick.
    public IEnumerable<PickBody> PickBodies
        => _triangles.Select(body => new PickBody(body.Key, body.Value.Positions, body.Value.Indices, IsVisible(body.Key.ToString())));

    // On the render loop: one entity and one mesh for each live body. The mesh is
    // narrowed around the centre of its box, and that centre is the origin of the
    // entity, in double precision (ADR-0024 item 2). It returns true when the scene changed.
    //
    // A failure of one body is written as a warning, and the next body continues.
    // Until TASK-0074 a throw ended the window and the session (E65).
    public bool Apply(Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var changed = false;
        while (_changes.TryDequeue(out var change))
        {
            try
            {
                changed |= ApplyOne(change, scene, meshes, device);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[WARN] The body {change.BodyId} was not {change.Change switch { Change.Created => "drawn", Change.Consumed => "removed", _ => "changed" }}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        return changed;
    }

    // One change of the queue. It returns true when the scene changed.
    private bool ApplyOne((Change Change, Guid BodyId, string Kind, string? Name, bool Visible) change, Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var meshId = change.BodyId.ToString();
        switch (change.Change)
        {
            case Change.Consumed:
                _live.RemoveAll(body => body.Id == change.BodyId);
                _visibility.Remove(meshId);
                _triangles.Remove(change.BodyId);
                _boxes.Remove(meshId);
                if (_entities.Remove(meshId, out var gone))
                    scene.Entities.Remove(gone);
                if (!meshes.Remove(meshId, out var mesh))
                    return false;

                // A frame in flight can still read the buffers of the mesh.
                device.WaitIdle();
                mesh.Dispose();
                return true;

            case Change.Changed:
                var index = _live.FindIndex(body => body.Id == change.BodyId);
                if (index < 0)
                    return false;
                _live[index] = _live[index] with { Name = change.Name, Visible = change.Visible };
                _visibility.Set(meshId, change.Visible);
                return Show(meshId, scene, change.Visible);
        }

        _live.Add(new LiveBody(change.BodyId, change.Kind, change.Name, change.Visible));
        _visibility.Set(meshId, change.Visible);

        var result = _host.Session.Query<Tessellation>(new GetTessellationQuery { BodyId = change.BodyId })
            .GetAwaiter().GetResult();

        // A command can consume a body before the next frame; its consumption follows
        // in the queue, so the missing body is not a defect (E61).
        if (result.Error is { Code: DiagnosticCodes.GeomBodyNotFound })
            return false;

        if (result.Error is { } error)
        {
            Console.WriteLine($"[WARN] No mesh for body {change.BodyId}: {error.Code} {error.Message}");
            return false;
        }

        var tessellation = result.Result!;
        if (tessellation.TriangleCount == 0)
            return false;

        var centre = MeshVertex.Centre(tessellation.Positions);
        meshes[meshId] = new GpuMesh(device, MeshVertex.Narrow(tessellation.Positions, centre, 1.0), tessellation.Indices);
        _entities[meshId] = new Entity { Name = $"Body {change.BodyId}", MeshId = meshId, Origin = centre };
        _boxes[meshId] = Box(tessellation.Positions);
        _triangles[change.BodyId] = (tessellation.Positions, tessellation.Indices);
        _visibility.SetMesh(meshId);
        return Show(meshId, scene, change.Visible);
    }

    // Puts the entity of a mesh in the scene or takes it out. It returns true when the scene
    // changed.
    private bool Show(string meshId, Scene scene, bool visible)
    {
        if (!_entities.TryGetValue(meshId, out var entity))
            return false;

        var shown = scene.Entities.Contains(entity);
        if (visible == shown)
            return false;

        if (visible)
            scene.Entities.Add(entity);
        else
            scene.Entities.Remove(entity);
        return true;
    }

    private bool IsVisible(string meshId) => _visibility.IsVisible(meshId);

    // The box around each visible live body that has a mesh, or null when none has one.
    public ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? Bounds
    {
        get
        {
            var boxes = _boxes.Where(box => IsVisible(box.Key)).Select(box => box.Value).ToList();
            if (boxes.Count == 0)
                return null;

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
