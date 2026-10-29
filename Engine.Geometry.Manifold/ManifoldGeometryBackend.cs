using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold.Native;

// The tests read the volume of a solid to measure the winding of a tessellation
// (ADR-0018 item 9, TASK-0028).
[assembly: InternalsVisibleTo("Engine.Tests")]

namespace Engine.Geometry.Manifold;

// Native geometry backend (ADR-0014), backed by the native Manifold kernel via manifoldc.
// P7b shipped the mesh + query surface (parity with the managed InProcessMeshBackend);
// P7c (ADR-0012 Amendment 1) adds transform (Translate) and boolean (Subtract) ops that
// the managed stub deliberately does not implement — real solids the stub can't represent.
// R2 (ADR-0018, TASK-0028) adds the tessellation, which the stub does not implement either.
// K0a (ADR-0036, TASK-0103) adds the extrude of one closed loop, which the stub does not
// implement either.
//
// Threading (ADR-0014 §3): single-threaded from the engine's view. Each call from a
// command and each call from a query runs inside the serial section of the
// DocumentSession of the host (TASK-0034), so no internal lock is taken. A caller
// that uses a bus with no session must keep its calls in series itself.
//
// Lifecycle (ADR-0014 §2): owns native handles; Dispose() releases them. Native object
// + its caller-allocated buffer are owned as a unit by ManifoldSolidHandle.
//
// Host-wired in P7b. Since TASK-0036 each host requires this backend: when the native
// library does not load, the host stops with E-GEOM-BACKEND-INIT and the sentence of
// UnavailableReason, and it does not take the managed backend, which holds boxes only
// (anti-objective 9 refuses a silent fallback). Only a test selects the managed backend,
// with an explicit option of the host. Native failures still throw
// plain exceptions rather than E-GEOM-* codes (TASK-0012 §5, pending). It mirrors
// InProcessMeshBackend's exception contract (InvalidOperationException on duplicate,
// KeyNotFoundException on missing) so the existing handlers behave the same.
public sealed class ManifoldGeometryBackend
    : IGeometryBackend, IMeshOps, IGeometryQuery, ITransformOps, IBooleanOps, ITessellationOps, IExtrudeOps, IDisposable
{
    private readonly Dictionary<Guid, ManifoldSolidHandle> _solids = new();
    private bool _disposed;

    // ADR-0015 item 5: the name and the version of the native library.
    public BackendIdentity Identity { get; } = new("manifold", NativeVersion);

    public BackendCapabilities Capabilities
        => BackendCapabilities.Mesh | BackendCapabilities.Query
         | BackendCapabilities.Transform | BackendCapabilities.Booleans
         | BackendCapabilities.Tessellation | BackendCapabilities.Extrude;

    public T? TryGet<T>() where T : class => this as T;

    // The name of the native library and the version of Manifold that it is built from.
    // The version is the version of the package Engine.Geometry.Manifold.Native in the
    // project file, and a test holds the two equal. /schema/backend gives both.
    public const string NativeLibraryName = "manifoldc";
    public const string NativeVersion = "3.5.2";

    // Whether the native library can be loaded on this platform. A host that cannot load
    // it stops (TASK-0036). The canonical replay gate stays on the managed backend, so
    // this platform-dependent check does not affect core determinism.
    public static bool IsNativeAvailable()
    {
        try
        {
            if (NativeLibrary.TryLoad(
                    NativeLibraryName, typeof(ManifoldGeometryBackend).Assembly, null, out var handle))
            {
                NativeLibrary.Free(handle);
                return true;
            }
        }
        catch
        {
            // Treat any probe failure as "unavailable".
        }
        return false;
    }

    // Until TASK-0048 a method here loaded libmanifold first by its full path, because
    // the package of 2026-09 stored the build folder of a runner as the search path of
    // that dependency, and the library loaded on no Linux or macOS computer (register
    // entry R-0035). The package 3.5.2.1 stores $ORIGIN and @loader_path, so the
    // system loader finds the dependency in the folder of libmanifoldc by itself.

    // The sentence that a host gives when the native library does not load: the library,
    // the platform and the platforms that have a payload (register entry R-0020 holds the
    // others). The host adds the code E-GEOM-BACKEND-INIT.
    public static string UnavailableReason()
        => $"The native geometry library '{NativeLibraryName}' (Manifold {NativeVersion}) did not load "
         + $"on the platform {RuntimeInformation.RuntimeIdentifier}. The native package holds the "
         + "platforms win-x64, linux-x64 and osx-arm64. The managed backend holds boxes only and "
         + "cannot move or cut a solid, therefore the host does not start with it.";

    // IMeshOps. Builds a native, origin-centered Manifold cube and stores it under the
    // handle. Throws on duplicate (mirrors the stub) — the bus derives handles from
    // CommandId and the idempotency cache short-circuits duplicates, so a real
    // collision is a programmer error.
    public void CreateBox(BodyHandle handle, BoxParameters parameters)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_solids.ContainsKey(handle.Id))
            throw new InvalidOperationException(
                $"Backend already holds a body under handle '{handle.Id}'.");

        nint buffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_manifold_size());
        nint solid = ManifoldNative.manifold_cube(
            buffer, parameters.SizeX, parameters.SizeY, parameters.SizeZ, center: 1);

        int status = ManifoldNative.manifold_status(solid);
        if (status != 0)
        {
            ManifoldNative.manifold_delete_manifold(solid); // frees `buffer`
            throw new ManifoldNativeException(
                $"manifold_cube failed with status {status} for handle '{handle.Id}'.");
        }

        _solids[handle.Id] = new ManifoldSolidHandle(solid);
    }

    // ITransformOps. Stores a translated copy of `source` under `result`, leaving the
    // source intact (ADR-0012 Amendment 1). Throws on a duplicate result handle or an
    // unknown source, mirroring CreateBox's contract.
    public void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_solids.ContainsKey(result.Id))
            throw new InvalidOperationException(
                $"Backend already holds a body under handle '{result.Id}'.");
        if (!_solids.TryGetValue(source.Id, out var src))
            throw new KeyNotFoundException(
                $"Backend has no body under handle '{source.Id}'.");

        nint buffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_manifold_size());
        // `src` is rooted in _solids for the call, so its raw handle stays alive.
        nint solid = ManifoldNative.manifold_translate(
            buffer, src.DangerousGetHandle(), dx, dy, dz);

        int status = ManifoldNative.manifold_status(solid);
        if (status != 0)
        {
            ManifoldNative.manifold_delete_manifold(solid); // frees `buffer`
            throw new ManifoldNativeException(
                $"manifold_translate failed with status {status} for handle '{result.Id}'.");
        }

        _solids[result.Id] = new ManifoldSolidHandle(solid);
    }

    // IBooleanOps. Stores (minuend - subtrahend) under `result`, leaving both operands
    // intact (ADR-0012 Amendment 1). Throws on a duplicate result handle, an unknown
    // operand, or an empty/degenerate difference (subtrahend fully contains minuend).
    public void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_solids.ContainsKey(result.Id))
            throw new InvalidOperationException(
                $"Backend already holds a body under handle '{result.Id}'.");
        if (!_solids.TryGetValue(minuend.Id, out var a))
            throw new KeyNotFoundException(
                $"Backend has no body under handle '{minuend.Id}'.");
        if (!_solids.TryGetValue(subtrahend.Id, out var b))
            throw new KeyNotFoundException(
                $"Backend has no body under handle '{subtrahend.Id}'.");

        nint buffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_manifold_size());
        // Both operands are rooted in _solids for the call, so their raw handles stay alive.
        nint solid = ManifoldNative.manifold_difference(
            buffer, a.DangerousGetHandle(), b.DangerousGetHandle());

        int status = ManifoldNative.manifold_status(solid);
        if (status != 0)
        {
            ManifoldNative.manifold_delete_manifold(solid); // frees `buffer`
            throw new ManifoldNativeException(
                $"manifold_difference failed with status {status} for handle '{result.Id}'.");
        }

        if (ManifoldNative.manifold_is_empty(solid) != 0)
        {
            ManifoldNative.manifold_delete_manifold(solid); // frees `buffer`
            throw new ManifoldNativeException(
                $"Subtraction produced an empty solid for handle '{result.Id}' " +
                "(the subtrahend fully contains the minuend).");
        }

        _solids[result.Id] = new ManifoldSolidHandle(solid);
    }

    // IExtrudeOps (ADR-0036 item 6). One simple polygon of the loop, extruded along +Z
    // with no slice, no twist and the scale 1, then moved into the frame by the matrix
    // of the columns U, V and N and the move Origin. Each entry of a frame of an origin
    // plane is 0 or ±1, so the move is exact. Throws on a duplicate handle, as
    // CreateBox does.
    public unsafe void Extrude(BodyHandle handle, ExtrudeParameters parameters)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_solids.ContainsKey(handle.Id))
            throw new InvalidOperationException(
                $"Backend already holds a body under handle '{handle.Id}'.");

        var points = new ManifoldVec2[parameters.Loop.Count];
        for (var i = 0; i < points.Length; i++)
            points[i] = new ManifoldVec2(parameters.Loop[i].X, parameters.Loop[i].Y);

        nint polygonBuffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_simple_polygon_size());
        nint polygonsBuffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_polygons_size());
        nint polygon = 0;
        nint polygons = 0;
        nint prism;
        try
        {
            fixed (ManifoldVec2* first = points)
                polygon = ManifoldNative.manifold_simple_polygon(polygonBuffer, (nint)first, (nuint)points.Length);
            nint list = polygon;
            polygons = ManifoldNative.manifold_polygons(polygonsBuffer, (nint)(&list), 1);

            nint prismBuffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_manifold_size());
            prism = ManifoldNative.manifold_extrude(
                prismBuffer, polygons, parameters.Height, slices: 0, twistDegrees: 0, scaleX: 1, scaleY: 1);
        }
        finally
        {
            // manifold_polygons copies each polygon, so both go after the extrude.
            if (polygons != 0) ManifoldNative.manifold_destruct_polygons(polygons);
            if (polygon != 0) ManifoldNative.manifold_destruct_simple_polygon(polygon);
            Marshal.FreeHGlobal(polygonsBuffer);
            Marshal.FreeHGlobal(polygonBuffer);
        }

        int status = ManifoldNative.manifold_status(prism);
        if (status != 0 || ManifoldNative.manifold_is_empty(prism) != 0)
        {
            ManifoldNative.manifold_delete_manifold(prism); // frees its buffer
            throw new ManifoldNativeException(
                $"manifold_extrude failed with status {status}, or gave an empty solid, for handle '{handle.Id}'.");
        }

        var (u, v, n, origin) = (parameters.U, parameters.V, parameters.N, parameters.Origin);
        nint buffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_manifold_size());
        nint solid = ManifoldNative.manifold_transform(
            buffer, prism, u.X, u.Y, u.Z, v.X, v.Y, v.Z, n.X, n.Y, n.Z, origin.X, origin.Y, origin.Z);
        ManifoldNative.manifold_delete_manifold(prism); // frees its buffer

        status = ManifoldNative.manifold_status(solid);
        if (status != 0)
        {
            ManifoldNative.manifold_delete_manifold(solid); // frees `buffer`
            throw new ManifoldNativeException(
                $"manifold_transform failed with status {status} for handle '{handle.Id}'.");
        }

        _solids[handle.Id] = new ManifoldSolidHandle(solid);
    }

    // IGeometryQuery. Native axis-aligned bounding box. Throws if the handle is unknown
    // to this backend — the query handler surfaces that as a structured error.
    public Aabb GetBoundingBox(BodyHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_solids.TryGetValue(handle.Id, out var solid))
            throw new KeyNotFoundException(
                $"Backend has no body under handle '{handle.Id}'.");

        nint boxBuffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_box_size());
        // `solid` is rooted in _solids for the duration of this call, so the raw handle
        // stays alive; DangerousGetHandle is safe here.
        nint box = ManifoldNative.manifold_bounding_box(boxBuffer, solid.DangerousGetHandle());
        try
        {
            ManifoldVec3 min = ManifoldNative.manifold_box_min(box);
            ManifoldVec3 max = ManifoldNative.manifold_box_max(box);
            return new Aabb(min.X, min.Y, min.Z, max.X, max.Y, max.Z);
        }
        finally
        {
            ManifoldNative.manifold_delete_box(box); // frees `boxBuffer`
        }
    }

    // ITessellationOps (ADR-0018). The triangles of the body from MeshGL64, in double
    // precision. Throws if the handle is unknown to this backend, as GetBoundingBox does.
    public Tessellation Tessellate(BodyHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_solids.TryGetValue(handle.Id, out var solid))
            throw new KeyNotFoundException(
                $"Backend has no body under handle '{handle.Id}'.");

        nint meshBuffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_meshgl64_size());
        // `solid` is rooted in _solids for the call, so its raw handle stays alive.
        nint mesh = ManifoldNative.manifold_get_meshgl64(meshBuffer, solid.DangerousGetHandle());
        try
        {
            return ReadMesh(mesh, handle);
        }
        finally
        {
            // The destructor in place, then the allocator that gave the buffer frees it
            // (see manifold_destruct_meshgl64).
            ManifoldNative.manifold_destruct_meshgl64(mesh);
            Marshal.FreeHGlobal(meshBuffer);
        }
    }

    // Each vertex holds numProp values, and the first three are x, y and z (MeshGLP of
    // Manifold). The stride is read and never assumed to be 3. Each index is 64-bit in
    // MeshGL64, and the wire form holds 32 bits (ADR-0018 item 2), so an index that
    // does not fit, or that names no vertex, is a failure and not a silent wrap.
    private static unsafe Tessellation ReadMesh(nint mesh, BodyHandle handle)
    {
        var stride = checked((int)ManifoldNative.manifold_meshgl64_num_prop(mesh));
        var vertexCount = checked((int)ManifoldNative.manifold_meshgl64_num_vert(mesh));
        var propertyLength = checked((int)ManifoldNative.manifold_meshgl64_vert_properties_length(mesh));
        var indexLength = checked((int)ManifoldNative.manifold_meshgl64_tri_length(mesh));

        if (stride < 3 || propertyLength != checked(vertexCount * stride) || indexLength % 3 != 0)
            throw new ManifoldNativeException(
                $"The mesh of handle '{handle.Id}' has {stride} properties for each vertex, {vertexCount} vertices, "
                + $"{propertyLength} property values and {indexLength} indices, which do not agree.");

        var properties = new double[propertyLength];
        var triVerts = new ulong[indexLength];
        fixed (double* target = properties)
            ManifoldNative.manifold_meshgl64_vert_properties((nint)target, mesh);
        fixed (ulong* target = triVerts)
            ManifoldNative.manifold_meshgl64_tri_verts((nint)target, mesh);

        var positions = new double[vertexCount * 3];
        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            positions[3 * vertex] = properties[stride * vertex];
            positions[3 * vertex + 1] = properties[stride * vertex + 1];
            positions[3 * vertex + 2] = properties[stride * vertex + 2];
        }

        var indices = new uint[indexLength];
        for (var i = 0; i < indexLength; i++)
        {
            if (triVerts[i] >= (ulong)vertexCount)
                throw new ManifoldNativeException(
                    $"Index {i} of the mesh of handle '{handle.Id}' is {triVerts[i]}, and the mesh has {vertexCount} vertices.");
            indices[i] = (uint)triVerts[i];
        }

        return new Tessellation(positions, indices);
    }

    // For the tests only: the volume that Manifold computes for the body, which the
    // signed volume of its tessellation must equal (ADR-0018 item 9).
    internal double Volume(BodyHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ManifoldNative.manifold_volume(_solids[handle.Id].DangerousGetHandle());
    }

    // For the tests only: the length of the merge vectors of the mesh of the body. The
    // task records it for a box (TASK-0028, notes for the implementer).
    internal int MergeLength(BodyHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        nint meshBuffer = Marshal.AllocHGlobal((nint)ManifoldNative.manifold_meshgl64_size());
        nint mesh = ManifoldNative.manifold_get_meshgl64(meshBuffer, _solids[handle.Id].DangerousGetHandle());
        try
        {
            return checked((int)ManifoldNative.manifold_meshgl64_merge_length(mesh));
        }
        finally
        {
            ManifoldNative.manifold_destruct_meshgl64(mesh);
            Marshal.FreeHGlobal(meshBuffer);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var solid in _solids.Values)
            solid.Dispose();
        _solids.Clear();
        _disposed = true;
    }
}
