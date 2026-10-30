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
// visibility (ADR-0028), for the panel and the selection, and the fault of its last
// rebuild, or null (ADR-0037 item 6).
internal sealed record LiveBody(Guid Id, string Kind, string? Name, bool Visible, BodyFault? Fault = null);

// The projection of the live bodies of the session into the scene of the desktop
// (roadmap phase R5, ADR-0009, ADR-0024). It reads the live bodies and subscribes to
// the events of the session in one read of the session, so it misses no body. The
// action of the subscription runs inside each commit and only puts the change in a
// queue; the render loop calls Apply, which fetches each new mesh with the query
// GetTessellation outside the commit, removes each consumed or removed body, and applies each
// change of a name or a visibility (body.changed, ADR-0028 item 7).
//
// ADR-0037 item 8 (K0c, TASK-0109): body.changed also gives the geometry key and the fault
// of the body. When the key differs from the key of the mesh that the desktop holds, a
// rebuild or an undo gave the body another solid, and Apply fetches its mesh again.
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

    // One change of the queue, from an event or from the first read of the session.
    private readonly record struct Pending(
        Change Change, Guid BodyId, string Kind, string? Name, bool Visible, Guid Geometry, BodyFault? Fault);

    private readonly EngineHost _host;
    private readonly ConcurrentQueue<Pending> _changes = new();
    private readonly IDisposable _subscription;

    // Each live body, in the order of its creation, for the panel of the user interface
    // (ADR-0027 item 7). It holds a body also when its mesh failed.
    private readonly List<LiveBody> _live = new();

    // The geometry key of the mesh that the desktop holds for each live body.
    private readonly Dictionary<Guid, Guid> _geometry = new();

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
                _changes.Enqueue(new Pending(Change.Created, body.Handle.Id, body.Kind, body.Name, body.Visible, body.GeometryKey, body.Fault));
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
        var geometry = payload.GetValueOrDefault("geometry") is Guid key ? key : bodyId;
        var fault = payload.GetValueOrDefault("fault") is IReadOnlyDictionary<string, object?> fields
            && fields.GetValueOrDefault("code") is string code
            ? new BodyFault(code, fields.GetValueOrDefault("message") as string ?? string.Empty)
            : null;
        switch (record.Kind)
        {
            case "body.created":
                _changes.Enqueue(new Pending(Change.Created, bodyId, payload.GetValueOrDefault("kind") as string ?? string.Empty, name, visible, geometry, fault));
                break;
            // ADR-0031 item 11: a body that an Undo removed leaves the scene as a consumed body does.
            case "body.consumed":
            case "body.removed":
                _changes.Enqueue(new Pending(Change.Consumed, bodyId, string.Empty, null, false, bodyId, null));
                break;
            case "body.changed":
                _changes.Enqueue(new Pending(Change.Changed, bodyId, string.Empty, name, visible, geometry, fault));
                break;
        }
    }

    // The count of meshes that Apply uploaded, for a test of V48 (TASK-0118).
    public int Meshes { get; private set; }

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
        // Finding V48 of the codebase review of 2026-10-30 (TASK-0118): twenty changes of one body
        // in one frame gave twenty meshes. Each body.changed carries the whole state of the body, so
        // a change that a later change of the same body follows, with no other event of it between,
        // is skipped.
        var queued = new List<Pending>();
        while (_changes.TryDequeue(out var next))
            queued.Add(next);

        for (var i = 0; i < queued.Count; i++)
        {
            var change = queued[i];
            if (change.Change == Change.Changed && Superseded(queued, i))
                continue;
            // Finding V55 of the codebase review of 2026-10-30 (b) (TASK-0124): a creation and then
            // changes of the same body in one frame gave two meshes. The creation takes the state of
            // the last change, so the change that follows it has the key of the mesh already.
            if (change.Change == Change.Created && LastChange(queued, i) is { } last)
                change = change with { Name = last.Name, Visible = last.Visible, Geometry = last.Geometry, Fault = last.Fault };
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

    // True when the next event of the same body after the change at the index is a change too.
    private static bool Superseded(List<Pending> queued, int index)
    {
        for (var j = index + 1; j < queued.Count; j++)
        {
            if (queued[j].BodyId == queued[index].BodyId)
                return queued[j].Change == Change.Changed;
        }
        return false;
    }

    // The last change of the body of the event at the index in the run of changes that follows it,
    // or null when the next event of the same body is not a change.
    private static Pending? LastChange(List<Pending> queued, int index)
    {
        Pending? last = null;
        for (var j = index + 1; j < queued.Count; j++)
        {
            if (queued[j].BodyId != queued[index].BodyId)
                continue;
            if (queued[j].Change != Change.Changed)
                break;
            last = queued[j];
        }
        return last;
    }

    // One change of the queue. It returns true when the scene changed.
    private bool ApplyOne(Pending change, Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var meshId = change.BodyId.ToString();
        switch (change.Change)
        {
            case Change.Consumed:
                _live.RemoveAll(body => body.Id == change.BodyId);
                _geometry.Remove(change.BodyId);
                _visibility.Remove(meshId);
                return RemoveMesh(change.BodyId, scene, meshes, device);

            case Change.Changed:
                var index = _live.FindIndex(body => body.Id == change.BodyId);
                if (index < 0)
                    return false;
                _live[index] = _live[index] with { Name = change.Name, Visible = change.Visible, Fault = change.Fault };
                _visibility.Set(meshId, change.Visible);

                // ADR-0037 item 8: another key is another solid, so the mesh comes again.
                if (_geometry.GetValueOrDefault(change.BodyId) != change.Geometry)
                {
                    var removed = RemoveMesh(change.BodyId, scene, meshes, device);
                    _geometry[change.BodyId] = change.Geometry;
                    return AddMesh(change, scene, meshes, device) | removed;
                }

                return Show(meshId, scene, change.Visible);
        }

        _live.Add(new LiveBody(change.BodyId, change.Kind, change.Name, change.Visible, change.Fault));
        _geometry[change.BodyId] = change.Geometry;
        _visibility.Set(meshId, change.Visible);
        return AddMesh(change, scene, meshes, device);
    }

    // Fetches the triangles of the body and puts its mesh and its entity in place.
    private bool AddMesh(Pending change, Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var meshId = change.BodyId.ToString();
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
        Meshes++;
        _entities[meshId] = new Entity { Name = $"Body {change.BodyId}", MeshId = meshId, Origin = centre };
        _boxes[meshId] = Box(tessellation.Positions);
        _triangles[change.BodyId] = (tessellation.Positions, tessellation.Indices);
        _visibility.SetMesh(meshId);
        return Show(meshId, scene, change.Visible);
    }

    // Takes the mesh, the entity, the box and the triangles of a body away. It returns true
    // when the scene changed.
    private bool RemoveMesh(Guid bodyId, Scene scene, IDictionary<string, GpuMesh> meshes, GraphicsDevice device)
    {
        var meshId = bodyId.ToString();
        _triangles.Remove(bodyId);
        _boxes.Remove(meshId);
        _visibility.RemoveMesh(meshId);
        var changed = _entities.Remove(meshId, out var gone) && scene.Entities.Remove(gone);
        if (!meshes.Remove(meshId, out var mesh))
            return changed;

        // A frame in flight can still read the buffers of the mesh.
        device.WaitIdle();
        mesh.Dispose();
        return true;
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

    // ADR-0038 item 7: the pivot of the gizmo, the centre of the box of a visible live body with a
    // mesh, or null.
    public (double X, double Y, double Z)? Pivot(Guid bodyId)
        => _entities.TryGetValue(bodyId.ToString(), out var entity) && IsVisible(bodyId.ToString()) ? entity.Origin : null;

    // ADR-0038 item 2: the drawing of a body moves by the offset of a drag. The engine does not
    // change, and an offset of zero gives the drawing back.
    public void Preview(Guid bodyId, System.Numerics.Vector3 offset)
    {
        if (_entities.TryGetValue(bodyId.ToString(), out var entity))
            entity.Transform = entity.Transform with { Position = offset };
    }

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
