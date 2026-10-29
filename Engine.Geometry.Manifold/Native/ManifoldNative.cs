using System.Runtime.InteropServices;

namespace Engine.Geometry.Manifold.Native;

// Blittable mirror of manifoldc's ManifoldVec3. DOUBLE precision: the production
// target is a double-precision Manifold build so BoxParameters/Aabb (both double)
// map 1:1. The 2024-era float C API would narrow — pin a double build (TASK-0012 §2).
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct ManifoldVec3(double X, double Y, double Z);

// Blittable mirror of manifoldc's ManifoldVec2 (types.h of 3.5.2: two doubles).
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct ManifoldVec2(double X, double Y);

// Thin P/Invoke surface over manifoldc — what CreateBox, GetBoundingBox, Translate,
// Subtract, Tessellate and Extrude need. Binding style: source-generated [LibraryImport] per
// ADR-0014 §1. The original nine were verified end-to-end against a real manifoldc.dll
// (TASK-0012 §2 spike); the three P7c ops (translate/difference/is_empty) follow the same
// convention and are exercised by the native [NativeManifoldFact] tests. All match
// Manifold v3.5.2's manifoldc.h (double-precision).
internal static partial class ManifoldNative
{
    private const string Lib = "manifoldc";

    // Caller-allocates-memory convention: allocate *_size() bytes, pass as `mem`.
    [LibraryImport(Lib)] internal static partial nuint manifold_manifold_size();
    [LibraryImport(Lib)] internal static partial nuint manifold_box_size();

    // center != 0 -> origin-centered, so the AABB is (-size/2 .. +size/2), matching
    // the managed stub's convention (verified in the spike).
    [LibraryImport(Lib)] internal static partial nint manifold_cube(nint mem, double x, double y, double z, int center);

    // P7c ops (ADR-0012 Amendment 1). Both take an existing manifold and produce a
    // new one into the caller-allocated `mem` (same convention as manifold_cube).
    [LibraryImport(Lib)] internal static partial nint manifold_translate(nint mem, nint manifold, double x, double y, double z);
    [LibraryImport(Lib)] internal static partial nint manifold_difference(nint mem, nint a, nint b);

    [LibraryImport(Lib)] internal static partial nint manifold_bounding_box(nint mem, nint manifold);
    [LibraryImport(Lib)] internal static partial ManifoldVec3 manifold_box_min(nint box);
    [LibraryImport(Lib)] internal static partial ManifoldVec3 manifold_box_max(nint box);

    // Returns ManifoldError (0 == MANIFOLD_NO_ERROR). Non-zero after construction
    // signals a degenerate/failed op.
    [LibraryImport(Lib)] internal static partial int manifold_status(nint manifold);

    // Non-zero when the manifold has no volume (e.g. a difference that removed
    // everything). Used to reject a degenerate boolean result.
    [LibraryImport(Lib)] internal static partial int manifold_is_empty(nint manifold);

    // The tessellation (ADR-0018, TASK-0028). manifold_get_meshgl64 builds a MeshGL64
    // in the caller `mem` with placement new and returns `mem`. Each accessor below
    // with a `mem` argument copies its vector into `mem` with memcpy (copy_data in
    // bindings/c/conv.h of Manifold 3.5.2), so the caller gives a buffer of
    // length x element size, and a pinned managed array is such a buffer.
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_size();
    [LibraryImport(Lib)] internal static partial nint manifold_get_meshgl64(nint mem, nint manifold);
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_num_prop(nint mesh);
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_num_vert(nint mesh);
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_num_tri(nint mesh);
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_vert_properties_length(nint mesh);
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_tri_length(nint mesh);
    [LibraryImport(Lib)] internal static partial nuint manifold_meshgl64_merge_length(nint mesh);
    [LibraryImport(Lib)] internal static partial nint manifold_meshgl64_vert_properties(nint mem, nint mesh);
    [LibraryImport(Lib)] internal static partial nint manifold_meshgl64_tri_verts(nint mem, nint mesh);

    // Runs the destructor of the MeshGL64 in place and does not free `mem`. The caller
    // then frees `mem` with the allocator that gave it. manifold_delete_meshgl64 would
    // call the C++ delete on a buffer that Marshal.AllocHGlobal gave (manifoldc.cpp of
    // 3.5.2, line 1075); this pair keeps one allocator for one buffer.
    [LibraryImport(Lib)] internal static partial void manifold_destruct_meshgl64(nint mesh);

    // The extrude (ADR-0036 item 6, TASK-0103). manifold_simple_polygon copies `length`
    // ManifoldVec2 values into a std::vector that it builds in `mem` with placement new;
    // manifold_polygons copies each simple polygon into a vector of vectors in `mem`
    // (manifoldc.cpp of 3.5.2, lines 68 to 85). manifold_extrude reads the polygons, and
    // manifold_transform reads four columns: the images of x, y and z, then the move
    // (lines 268 to 275 and 538 to 545). Each destruct runs the destructor in place and
    // does not free `mem`, as manifold_destruct_meshgl64 does.
    [LibraryImport(Lib)] internal static partial nuint manifold_simple_polygon_size();
    [LibraryImport(Lib)] internal static partial nuint manifold_polygons_size();
    [LibraryImport(Lib)] internal static partial nint manifold_simple_polygon(nint mem, nint points, nuint length);
    [LibraryImport(Lib)] internal static partial nint manifold_polygons(nint mem, nint polygons, nuint length);
    [LibraryImport(Lib)] internal static partial void manifold_destruct_simple_polygon(nint polygon);
    [LibraryImport(Lib)] internal static partial void manifold_destruct_polygons(nint polygons);
    [LibraryImport(Lib)]
    internal static partial nint manifold_extrude(
        nint mem, nint polygons, double height, int slices, double twistDegrees, double scaleX, double scaleY);
    [LibraryImport(Lib)]
    internal static partial nint manifold_transform(
        nint mem, nint manifold,
        double x1, double y1, double z1, double x2, double y2, double z2,
        double x3, double y3, double z3, double x4, double y4, double z4);

    // The volume of a solid, for the test of the winding (ADR-0018 item 9).
    [LibraryImport(Lib)] internal static partial double manifold_volume(nint manifold);

    // VERIFIED (TASK-0012 §2): manifold_delete_* frees the caller `mem` buffer itself.
    // Call these ONLY to release — never also Marshal.FreeHGlobal the buffer, that
    // double-frees and crashes the process.
    [LibraryImport(Lib)] internal static partial void manifold_delete_manifold(nint manifold);
    [LibraryImport(Lib)] internal static partial void manifold_delete_box(nint box);
}
