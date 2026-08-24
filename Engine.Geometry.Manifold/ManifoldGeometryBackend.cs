using System.Runtime.InteropServices;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold.Native;

namespace Engine.Geometry.Manifold;

// Native geometry backend (ADR-0014), backed by the native Manifold kernel via manifoldc.
// P7b shipped the mesh + query surface (parity with the managed InProcessMeshBackend);
// P7c (ADR-0012 Amendment 1) adds transform (Translate) and boolean (Subtract) ops that
// the managed stub deliberately does not implement — real solids the stub can't represent.
//
// Threading (ADR-0014 §3): single-threaded from the engine's view. Every call runs
// inside CommandBus's serial commit section, so no internal lock is taken.
//
// Lifecycle (ADR-0014 §2): owns native handles; Dispose() releases them. Native object
// + its caller-allocated buffer are owned as a unit by ManifoldSolidHandle.
//
// Host-wired in P7b: the CLI and HTTP hosts select this backend when the native
// manifoldc library is loadable (IsNativeAvailable), else fall back to the managed
// InProcessMeshBackend so the engine runs on any platform. Native failures still throw
// plain exceptions rather than E-GEOM-* codes (TASK-0012 §5, pending). It mirrors
// InProcessMeshBackend's exception contract (InvalidOperationException on duplicate,
// KeyNotFoundException on missing) so the existing handlers behave the same.
public sealed class ManifoldGeometryBackend
    : IGeometryBackend, IMeshOps, IGeometryQuery, ITransformOps, IBooleanOps, IDisposable
{
    private readonly Dictionary<Guid, ManifoldSolidHandle> _solids = new();
    private bool _disposed;

    public BackendCapabilities Capabilities
        => BackendCapabilities.Mesh | BackendCapabilities.Query
         | BackendCapabilities.Transform | BackendCapabilities.Booleans;

    public T? TryGet<T>() where T : class => this as T;

    // Whether the native manifoldc library can be loaded on this platform/RID. Hosts use
    // this to select Manifold when the payload is present and fall back to the managed
    // stub otherwise, so the engine runs everywhere. The canonical replay gate stays on
    // the stub, so this platform-dependent selection does not affect core determinism.
    public static bool IsNativeAvailable()
    {
        try
        {
            if (NativeLibrary.TryLoad(
                    "manifoldc", typeof(ManifoldGeometryBackend).Assembly, null, out var handle))
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

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var solid in _solids.Values)
            solid.Dispose();
        _solids.Clear();
        _disposed = true;
    }
}
