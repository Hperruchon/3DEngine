using Engine.Contracts;
using Engine.Contracts.Geometry;

namespace Engine.Core.Geometry;

// V1.x first backend per ADR-0012 §7. In-process, fully managed, no native
// interop. Sufficient to wire CreateBox end-to-end and to demonstrate the
// replay-against-fresh-backend story. Manifold lands in a follow-on TASK
// (P7b) behind the same capability interfaces.
public sealed class InProcessMeshBackend : IGeometryBackend, IMeshOps, IGeometryQuery
{
    private readonly Dictionary<Guid, BoxRecord> _boxes = new();

    public BackendCapabilities Capabilities
        => BackendCapabilities.Mesh | BackendCapabilities.Query;

    public T? TryGet<T>() where T : class => this as T;

    // IMeshOps. Stores the box under the given handle. Throws on duplicate
    // because the bus computes handles from CommandId and the idempotency
    // cache short-circuits duplicate commands — a true collision here would
    // be a programmer error worth surfacing loudly.
    public void CreateBox(BodyHandle handle, BoxParameters parameters)
    {
        if (_boxes.ContainsKey(handle.Id))
            throw new InvalidOperationException(
                $"Backend already holds a body under handle '{handle.Id}'.");
        _boxes[handle.Id] = new BoxRecord(handle, parameters);
    }

    // IGeometryQuery. Axis-aligned box centered at origin per ADR-0012 §7.
    // No transforms in V1.
    public Aabb GetBoundingBox(BodyHandle handle)
    {
        if (!_boxes.TryGetValue(handle.Id, out var record))
            throw new KeyNotFoundException(
                $"Backend has no body under handle '{handle.Id}'.");
        var halfX = record.Parameters.SizeX / 2.0;
        var halfY = record.Parameters.SizeY / 2.0;
        var halfZ = record.Parameters.SizeZ / 2.0;
        return new Aabb(
            -halfX, -halfY, -halfZ,
            +halfX, +halfY, +halfZ);
    }

    public bool Contains(BodyHandle handle) => _boxes.ContainsKey(handle.Id);

    public int BodyCount => _boxes.Count;

    private sealed record BoxRecord(BodyHandle Handle, BoxParameters Parameters);
}
