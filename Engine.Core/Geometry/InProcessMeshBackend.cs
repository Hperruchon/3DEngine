using Engine.Contracts;
using Engine.Contracts.Geometry;

namespace Engine.Core.Geometry;

// V1.x first backend per ADR-0012 §7. In-process, fully managed, no native
// interop. Sufficient to wire CreateBox end-to-end and to demonstrate the
// replay-against-fresh-backend story. Manifold lands in a follow-on TASK
// (P7b) behind the same capability interfaces.
public sealed class InProcessMeshBackend : IGeometryBackend, IMeshOps, IGeometryQuery, IMeshImportOps
{
    // ADR-0083 item 8 (TASK-0258): the triangles of each mesh body, as the STL file gave them.
    private readonly Dictionary<Guid, float[]> _meshes = new();

    public void ImportMesh(BodyHandle handle, ReadOnlySpan<float> triangles)
    {
        if (triangles.Length == 0 || triangles.Length % 9 != 0)
            throw new ArgumentException("A mesh holds nine floats for each triangle, and at least one triangle.", nameof(triangles));
        if (_boxes.ContainsKey(handle.Id) || _meshes.ContainsKey(handle.Id))
            throw new InvalidOperationException($"Backend already holds a body under handle '{handle.Id}'.");
        _meshes[handle.Id] = triangles.ToArray();
    }

    private readonly Dictionary<Guid, BoxRecord> _boxes = new();

    // ADR-0015 item 5. The hosts call this backend "managed" (ServerEngine).
    public BackendIdentity Identity { get; } = new("managed", "1");

    public BackendCapabilities Capabilities
        => BackendCapabilities.Mesh | BackendCapabilities.Query;

    public T? TryGet<T>() where T : class => this as T;

    // IMeshOps. Stores the box under the given handle. Throws on duplicate,
    // because the bus computes handles from CommandId. The idempotency cache of
    // a live bus stops a command that comes again only while the cache holds its
    // result: the cache keeps the last 1,024 results by default
    // (IdempotencyCache.DefaultCapacity), and it keeps no cancelled result. An
    // older command that comes again reaches this method, and the handler gives
    // E-GEOM-NATIVE-OP. An open does not look in the cache (ApplyEntry), and a
    // replay has none, so the handler gives E-GEOM-NATIVE-OP for the second entry
    // too (E31; E110 corrected this comment; finding E24 of the codebase review of
    // 2026-10-04, R-0033, TASK-0321, gave the limit of the cache).
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
        // ADR-0083 item 8: the box of a mesh body, from comparisons of its floats only.
        if (_meshes.TryGetValue(handle.Id, out var triangles))
        {
            var (min, max) = ((float[])[triangles[0], triangles[1], triangles[2]], (float[])[triangles[0], triangles[1], triangles[2]]);
            for (var i = 0; i < triangles.Length; i++)
                (min[i % 3], max[i % 3]) = (Math.Min(min[i % 3], triangles[i]), Math.Max(max[i % 3], triangles[i]));
            return new Aabb(min[0], min[1], min[2], max[0], max[1], max[2]);
        }

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

    public bool Contains(BodyHandle handle) => _boxes.ContainsKey(handle.Id) || _meshes.ContainsKey(handle.Id);

    public int BodyCount => _boxes.Count + _meshes.Count;

    private sealed record BoxRecord(BodyHandle Handle, BoxParameters Parameters);
}
