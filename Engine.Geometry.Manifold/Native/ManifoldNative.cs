using System.Runtime.InteropServices;

namespace Engine.Geometry.Manifold.Native;

// Blittable mirror of manifoldc's ManifoldVec3. DOUBLE precision: the production
// target is a double-precision Manifold build so BoxParameters/Aabb (both double)
// map 1:1. The 2024-era float C API would narrow — pin a double build (TASK-0012 §2).
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct ManifoldVec3(double X, double Y, double Z);

// Thin P/Invoke surface over manifoldc — only what CreateBox + GetBoundingBox need.
// Binding style: source-generated [LibraryImport] per ADR-0014 §1. Symbol names were
// verified end-to-end against a real manifoldc.dll (TASK-0012 §2 spike) and match
// Manifold v3.5.2's manifoldc.h (double-precision; all nine symbols present).
internal static partial class ManifoldNative
{
    private const string Lib = "manifoldc";

    // Caller-allocates-memory convention: allocate *_size() bytes, pass as `mem`.
    [LibraryImport(Lib)] internal static partial nuint manifold_manifold_size();
    [LibraryImport(Lib)] internal static partial nuint manifold_box_size();

    // center != 0 -> origin-centered, so the AABB is (-size/2 .. +size/2), matching
    // the managed stub's convention (verified in the spike).
    [LibraryImport(Lib)] internal static partial nint manifold_cube(nint mem, double x, double y, double z, int center);
    [LibraryImport(Lib)] internal static partial nint manifold_bounding_box(nint mem, nint manifold);
    [LibraryImport(Lib)] internal static partial ManifoldVec3 manifold_box_min(nint box);
    [LibraryImport(Lib)] internal static partial ManifoldVec3 manifold_box_max(nint box);

    // Returns ManifoldError (0 == MANIFOLD_NO_ERROR). Non-zero after construction
    // signals a degenerate/failed op.
    [LibraryImport(Lib)] internal static partial int manifold_status(nint manifold);

    // VERIFIED (TASK-0012 §2): manifold_delete_* frees the caller `mem` buffer itself.
    // Call these ONLY to release — never also Marshal.FreeHGlobal the buffer, that
    // double-frees and crashes the process.
    [LibraryImport(Lib)] internal static partial void manifold_delete_manifold(nint manifold);
    [LibraryImport(Lib)] internal static partial void manifold_delete_box(nint box);
}
