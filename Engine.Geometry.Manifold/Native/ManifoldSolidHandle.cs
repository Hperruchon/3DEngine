using System.Runtime.InteropServices;

namespace Engine.Geometry.Manifold.Native;

// Owns one native Manifold object together with its caller-allocated buffer as a
// single unit. ReleaseHandle runs the destructor in place, then frees the buffer with the
// allocator that gave it (finding E14, TASK-0115). Until TASK-0115 it called
// manifold_delete_manifold, the C++ delete, on a buffer of Marshal.AllocHGlobal; the spike of
// TASK-0012 saw a crash when it called both, because the delete had already freed the buffer.
internal sealed class ManifoldSolidHandle : SafeHandle
{
    private readonly nint _buffer;

    public ManifoldSolidHandle(nint solid, nint buffer)
        : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
    {
        _buffer = buffer;
        SetHandle(solid);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        ManifoldGeometryBackend.Release(handle, _buffer);
        return true;
    }
}
