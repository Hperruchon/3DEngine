using System.Runtime.InteropServices;

namespace Engine.Geometry.Manifold.Native;

// Owns one native Manifold object together with its caller-allocated buffer as a
// single unit. Per the verified ownership protocol (TASK-0012 §2):
// manifold_delete_manifold frees the buffer itself, so ReleaseHandle calls ONLY the
// native delete — it must not additionally free the buffer (doing both double-frees
// and crashes the process, observed as exit 127 in the spike).
internal sealed class ManifoldSolidHandle : SafeHandle
{
    public ManifoldSolidHandle(nint solid)
        : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
        => SetHandle(solid);

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        ManifoldNative.manifold_delete_manifold(handle);
        return true;
    }
}
