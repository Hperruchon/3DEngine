using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold;
using Xunit.Abstractions;

namespace Engine.Tests.Geometry;

// The tessellation of the native backend (ADR-0018, TASK-0028). Each test needs
// the native library, and in the pipeline it cannot skip (TASK-0036).
//
// ADR-0018 item 9 states that the triangles are wound counter-clockwise when seen
// from outside the body. The signed volume of a closed mesh is the sum of
// a · (b × c) / 6 over its triangles: it is positive for that winding and
// negative for the other one, so the tests measure the winding with it.
public class ManifoldTessellationTests(ITestOutputHelper output)
{
    [NativeManifoldFact]
    public void A_2_By_3_By_4_Box_Gives_12_Triangles_With_A_Signed_Volume_Of_Plus_24()
    {
        using var backend = new ManifoldGeometryBackend();
        var box = new BodyHandle(Guid.NewGuid());
        backend.CreateBox(box, new BoxParameters(2, 3, 4));

        var mesh = backend.Tessellate(box);

        Assert.Equal(12, mesh.TriangleCount);
        Assert.Equal(24.0, SignedVolume(mesh), 9);
        Assert.Equal(backend.Volume(box), SignedVolume(mesh), 9);
        AssertClosedAndConsistent(mesh);
        output.WriteLine($"box 2x3x4: {mesh.VertexCount} vertices, {mesh.TriangleCount} triangles, "
            + $"merge length {backend.MergeLength(box)}");
    }

    [NativeManifoldFact]
    public void A_Box_Minus_A_Translated_Box_Gives_A_Closed_Mesh_With_A_Signed_Volume_Of_Plus_4()
    {
        using var backend = new ManifoldGeometryBackend();
        var (a, b, moved, cut) = (Handle(), Handle(), Handle(), Handle());
        backend.CreateBox(a, new BoxParameters(2, 2, 2));
        backend.CreateBox(b, new BoxParameters(2, 2, 2));
        backend.Translate(moved, b, 1, 0, 0);
        backend.Subtract(cut, a, moved);

        var mesh = backend.Tessellate(cut);

        Assert.Equal(4.0, SignedVolume(mesh), 9);
        Assert.Equal(backend.Volume(cut), SignedVolume(mesh), 9);
        AssertClosedAndConsistent(mesh);
        output.WriteLine($"cut: {mesh.VertexCount} vertices, {mesh.TriangleCount} triangles, "
            + $"merge length {backend.MergeLength(cut)}");
    }

    [NativeManifoldFact]
    public void Each_Position_Lies_Inside_The_Bounding_Box_Of_The_Body()
    {
        using var backend = new ManifoldGeometryBackend();
        var (a, b, moved, cut) = (Handle(), Handle(), Handle(), Handle());
        backend.CreateBox(a, new BoxParameters(3, 2, 1));
        backend.CreateBox(b, new BoxParameters(1, 1, 1));
        backend.Translate(moved, b, 1.25, 0.5, 0);
        backend.Subtract(cut, a, moved);

        var mesh = backend.Tessellate(cut);
        var bounds = backend.GetBoundingBox(cut);

        for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
        {
            var (x, y, z) = (mesh.Positions[3 * vertex], mesh.Positions[3 * vertex + 1], mesh.Positions[3 * vertex + 2]);
            Assert.InRange(x, bounds.MinX - 1e-9, bounds.MaxX + 1e-9);
            Assert.InRange(y, bounds.MinY - 1e-9, bounds.MaxY + 1e-9);
            Assert.InRange(z, bounds.MinZ - 1e-9, bounds.MaxZ + 1e-9);
        }
    }

    [NativeManifoldFact]
    public void The_Backend_Declares_The_Capability_And_Refuses_An_Unknown_Body()
    {
        using var backend = new ManifoldGeometryBackend();

        Assert.True(backend.Capabilities.HasFlag(BackendCapabilities.Tessellation));
        Assert.Same(backend, backend.TryGet<ITessellationOps>());
        Assert.Throws<KeyNotFoundException>(() => backend.Tessellate(Handle()));
    }

    // The release of the native mesh (TASK-0028, notes for the implementer). A wrong
    // release frees one buffer two times, and the process stops; TASK-0012 saw exit
    // code 127. Many calls make such a defect show in this run.
    [NativeManifoldFact]
    public void Many_Tessellations_Of_One_Body_Give_The_Same_Mesh()
    {
        using var backend = new ManifoldGeometryBackend();
        var box = Handle();
        backend.CreateBox(box, new BoxParameters(2, 3, 4));
        var first = backend.Tessellate(box);

        for (var i = 0; i < 2000; i++)
        {
            var again = backend.Tessellate(box);
            Assert.Equal(first.Positions, again.Positions);
            Assert.Equal(first.Indices, again.Indices);
        }
    }

    private static BodyHandle Handle() => new(Guid.NewGuid());

    private static double SignedVolume(Tessellation mesh)
    {
        var sum = 0.0;
        for (var t = 0; t < mesh.TriangleCount; t++)
        {
            var (a, b, c) = (Point(mesh, mesh.Indices[3 * t]), Point(mesh, mesh.Indices[3 * t + 1]), Point(mesh, mesh.Indices[3 * t + 2]));
            sum += a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X);
        }

        return sum / 6;
    }

    // Each edge occurs in exactly two triangles, one time in each direction. The
    // edges are paired by position and not by index, because MeshGL can split a
    // vertex into two vertices at one position.
    private static void AssertClosedAndConsistent(Tessellation mesh)
    {
        var directed = new Dictionary<(string, string), int>();
        for (var t = 0; t < mesh.TriangleCount; t++)
        {
            var corners = new[] { Key(mesh, mesh.Indices[3 * t]), Key(mesh, mesh.Indices[3 * t + 1]), Key(mesh, mesh.Indices[3 * t + 2]) };
            for (var k = 0; k < 3; k++)
            {
                var edge = (corners[k], corners[(k + 1) % 3]);
                directed[edge] = directed.GetValueOrDefault(edge) + 1;
            }
        }

        foreach (var ((from, to), count) in directed)
        {
            Assert.True(count == 1, $"The edge {from} -> {to} occurs {count} times in one direction.");
            Assert.True(directed.GetValueOrDefault((to, from)) == 1, $"The edge {from} -> {to} has no partner in the other direction.");
        }
    }

    private static (double X, double Y, double Z) Point(Tessellation mesh, uint index)
        => (mesh.Positions[3 * index], mesh.Positions[3 * index + 1], mesh.Positions[3 * index + 2]);

    private static string Key(Tessellation mesh, uint index)
    {
        var (x, y, z) = Point(mesh, index);
        return string.Create(CultureInfo.InvariantCulture, $"{x:R},{y:R},{z:R}");
    }
}
