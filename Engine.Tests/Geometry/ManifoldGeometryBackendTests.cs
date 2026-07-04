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
    public void Capabilities_Are_Mesh_And_Query()
    {
        using var backend = NewBackend();
        Assert.Equal(BackendCapabilities.Mesh | BackendCapabilities.Query, backend.Capabilities);
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
}
