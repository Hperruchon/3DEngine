using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold;

namespace Engine.Tests.Geometry;

// A [Fact] that is skipped when the native manifoldc library is not loadable on the
// runner (e.g. a RID without the payload). xunit v2 has no Assert.Skip, so a derived
// FactAttribute sets Skip at discovery time.
public sealed class NativeManifoldFactAttribute : FactAttribute
{
    public NativeManifoldFactAttribute()
    {
        if (!ManifoldGeometryBackend.IsNativeAvailable())
            Skip = "Native manifoldc is not available on this runner.";
    }
}

// Native Manifold backend, mirroring InProcessMeshBackendTests for behavioural parity.
// Skipped when native is unavailable — the canonical replay-determinism gate stays on
// the managed stub, so skipping here does not weaken core determinism (TASK-0012 §7).
public class ManifoldGeometryBackendTests
{
    private static ManifoldGeometryBackend NewBackend() => new();

    [NativeManifoldFact]
    public void Capabilities_Include_Mesh_Query_Transform_Booleans()
    {
        using var backend = NewBackend();
        Assert.Equal(
            BackendCapabilities.Mesh | BackendCapabilities.Query
                | BackendCapabilities.Transform | BackendCapabilities.Booleans,
            backend.Capabilities);
    }

    [NativeManifoldFact]
    public void TryGet_Returns_Self_For_Mesh_And_Query()
    {
        using var backend = NewBackend();
        Assert.Same(backend, backend.TryGet<IMeshOps>());
        Assert.Same(backend, backend.TryGet<IGeometryQuery>());
    }

    [NativeManifoldFact]
    public void TryGet_Returns_Null_For_Reserved_Capabilities()
    {
        using var backend = NewBackend();
        Assert.Null(backend.TryGet<IBRepOps>());
        Assert.Null(backend.TryGet<IFeatureIdMap>());
    }

    [NativeManifoldFact]
    public void CreateBox_Then_GetBoundingBox_Returns_Origin_Centered_Aabb()
    {
        using var backend = NewBackend();
        var handle = new BodyHandle(Guid.NewGuid());
        ((IMeshOps)backend).CreateBox(handle, new BoxParameters(10.0, 20.0, 40.0));

        var aabb = ((IGeometryQuery)backend).GetBoundingBox(handle);

        // Parity with the managed stub (origin-centered), within FP tolerance.
        Assert.Equal(-5.0, aabb.MinX, 6);
        Assert.Equal(-10.0, aabb.MinY, 6);
        Assert.Equal(-20.0, aabb.MinZ, 6);
        Assert.Equal(5.0, aabb.MaxX, 6);
        Assert.Equal(10.0, aabb.MaxY, 6);
        Assert.Equal(20.0, aabb.MaxZ, 6);
    }

    [NativeManifoldFact]
    public void CreateBox_With_Duplicate_Handle_Throws()
    {
        using var backend = NewBackend();
        var handle = new BodyHandle(Guid.NewGuid());
        var mesh = (IMeshOps)backend;
        mesh.CreateBox(handle, new BoxParameters(1, 1, 1));
        Assert.Throws<InvalidOperationException>(
            () => mesh.CreateBox(handle, new BoxParameters(2, 2, 2)));
    }

    [NativeManifoldFact]
    public void GetBoundingBox_For_Unknown_Handle_Throws_KeyNotFound()
    {
        using var backend = NewBackend();
        Assert.Throws<KeyNotFoundException>(
            () => ((IGeometryQuery)backend).GetBoundingBox(new BodyHandle(Guid.NewGuid())));
    }

    [NativeManifoldFact]
    public void Dispose_Is_Idempotent()
    {
        var backend = NewBackend();
        ((IMeshOps)backend).CreateBox(new BodyHandle(Guid.NewGuid()), new BoxParameters(1, 1, 1));
        backend.Dispose();
        backend.Dispose(); // second dispose must not throw
    }

    // --- P7c: Translate + Subtract (ADR-0012 Amendment 1) ---

    [NativeManifoldFact]
    public void TryGet_Returns_Self_For_Transform_And_Boolean_Ops()
    {
        using var backend = NewBackend();
        Assert.Same(backend, backend.TryGet<ITransformOps>());
        Assert.Same(backend, backend.TryGet<IBooleanOps>());
    }

    [NativeManifoldFact]
    public void Translate_Shifts_The_BoundingBox_And_Leaves_Source_Intact()
    {
        using var backend = NewBackend();
        var box = new BodyHandle(Guid.NewGuid());
        ((IMeshOps)backend).CreateBox(box, new BoxParameters(10, 10, 10)); // -5..5 each axis

        var moved = new BodyHandle(Guid.NewGuid());
        ((ITransformOps)backend).Translate(moved, box, 5, 0, 0);

        var aabb = ((IGeometryQuery)backend).GetBoundingBox(moved);
        Assert.Equal(0.0, aabb.MinX, 6);
        Assert.Equal(10.0, aabb.MaxX, 6);
        Assert.Equal(-5.0, aabb.MinY, 6);
        Assert.Equal(5.0, aabb.MaxY, 6);

        // The source body is left where it was.
        var src = ((IGeometryQuery)backend).GetBoundingBox(box);
        Assert.Equal(-5.0, src.MinX, 6);
        Assert.Equal(5.0, src.MaxX, 6);
    }

    [NativeManifoldFact]
    public void Subtract_Of_Off_Center_Box_Trims_The_BoundingBox()
    {
        using var backend = NewBackend();
        var mesh = (IMeshOps)backend;
        var a = new BodyHandle(Guid.NewGuid());
        var b = new BodyHandle(Guid.NewGuid());
        mesh.CreateBox(a, new BoxParameters(10, 10, 10)); // -5..5
        mesh.CreateBox(b, new BoxParameters(10, 10, 10)); // -5..5

        // Move B to +x so it overlaps only the +x half of A.
        var bMoved = new BodyHandle(Guid.NewGuid());
        ((ITransformOps)backend).Translate(bMoved, b, 5, 0, 0); // 0..10 on x

        // A - B' removes A's +x half, leaving x in [-5, 0]. This is something the
        // managed stub could never compute — the AABB visibly shrinks.
        var diff = new BodyHandle(Guid.NewGuid());
        ((IBooleanOps)backend).Subtract(diff, a, bMoved);

        var aabb = ((IGeometryQuery)backend).GetBoundingBox(diff);
        Assert.Equal(-5.0, aabb.MinX, 6);
        Assert.Equal(0.0, aabb.MaxX, 6);   // trimmed from 5 to 0
        Assert.Equal(-5.0, aabb.MinY, 6);
        Assert.Equal(5.0, aabb.MaxY, 6);
        Assert.Equal(-5.0, aabb.MinZ, 6);
        Assert.Equal(5.0, aabb.MaxZ, 6);
    }

    [NativeManifoldFact]
    public void Subtract_Where_Subtrahend_Contains_Minuend_Throws()
    {
        using var backend = NewBackend();
        var mesh = (IMeshOps)backend;
        var a = new BodyHandle(Guid.NewGuid());
        var b = new BodyHandle(Guid.NewGuid());
        mesh.CreateBox(a, new BoxParameters(10, 10, 10)); // -5..5
        mesh.CreateBox(b, new BoxParameters(20, 20, 20)); // -10..10, fully contains A

        var diff = new BodyHandle(Guid.NewGuid());
        Assert.Throws<ManifoldNativeException>(
            () => ((IBooleanOps)backend).Subtract(diff, a, b));
    }

    [NativeManifoldFact]
    public void Translate_Unknown_Source_Throws_KeyNotFound()
    {
        using var backend = NewBackend();
        Assert.Throws<KeyNotFoundException>(
            () => ((ITransformOps)backend).Translate(
                new BodyHandle(Guid.NewGuid()), new BodyHandle(Guid.NewGuid()), 1, 2, 3));
    }

    [NativeManifoldFact]
    public void Subtract_Unknown_Operand_Throws_KeyNotFound()
    {
        using var backend = NewBackend();
        var a = new BodyHandle(Guid.NewGuid());
        ((IMeshOps)backend).CreateBox(a, new BoxParameters(1, 1, 1));
        Assert.Throws<KeyNotFoundException>(
            () => ((IBooleanOps)backend).Subtract(
                new BodyHandle(Guid.NewGuid()), a, new BodyHandle(Guid.NewGuid())));
    }
}
