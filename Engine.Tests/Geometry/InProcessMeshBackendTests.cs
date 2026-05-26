using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Geometry;

namespace Engine.Tests.Geometry;

public class InProcessMeshBackendTests
{
    [Fact]
    public void Capabilities_Are_Mesh_And_Query()
    {
        var backend = new InProcessMeshBackend();
        Assert.Equal(BackendCapabilities.Mesh | BackendCapabilities.Query, backend.Capabilities);
    }

    [Fact]
    public void TryGet_Returns_Self_For_Mesh_Ops()
    {
        var backend = new InProcessMeshBackend();
        var mesh = backend.TryGet<IMeshOps>();
        Assert.NotNull(mesh);
        Assert.Same(backend, mesh);
    }

    [Fact]
    public void TryGet_Returns_Self_For_Geometry_Query()
    {
        var backend = new InProcessMeshBackend();
        var query = backend.TryGet<IGeometryQuery>();
        Assert.NotNull(query);
        Assert.Same(backend, query);
    }

    [Fact]
    public void TryGet_Returns_Null_For_BRep_Ops()
    {
        var backend = new InProcessMeshBackend();
        Assert.Null(backend.TryGet<IBRepOps>());
    }

    [Fact]
    public void TryGet_Returns_Null_For_FeatureIdMap()
    {
        var backend = new InProcessMeshBackend();
        Assert.Null(backend.TryGet<IFeatureIdMap>());
    }

    [Fact]
    public void CreateBox_Stores_Body_Under_Given_Handle()
    {
        var backend = new InProcessMeshBackend();
        var handle = new BodyHandle(Guid.NewGuid());
        backend.CreateBox(handle, new BoxParameters(1.0, 2.0, 3.0));
        Assert.True(backend.Contains(handle));
        Assert.Equal(1, backend.BodyCount);
    }

    [Fact]
    public void GetBoundingBox_Returns_Axis_Aligned_Box_Centered_At_Origin()
    {
        var backend = new InProcessMeshBackend();
        var handle = new BodyHandle(Guid.NewGuid());
        backend.CreateBox(handle, new BoxParameters(10.0, 20.0, 40.0));
        var aabb = backend.GetBoundingBox(handle);
        Assert.Equal(-5.0, aabb.MinX);
        Assert.Equal(-10.0, aabb.MinY);
        Assert.Equal(-20.0, aabb.MinZ);
        Assert.Equal(5.0, aabb.MaxX);
        Assert.Equal(10.0, aabb.MaxY);
        Assert.Equal(20.0, aabb.MaxZ);
    }

    [Fact]
    public void CreateBox_With_Duplicate_Handle_Throws()
    {
        var backend = new InProcessMeshBackend();
        var handle = new BodyHandle(Guid.NewGuid());
        backend.CreateBox(handle, new BoxParameters(1, 1, 1));
        Assert.Throws<InvalidOperationException>(
            () => backend.CreateBox(handle, new BoxParameters(2, 2, 2)));
    }

    [Fact]
    public void GetBoundingBox_For_Unknown_Handle_Throws_KeyNotFound()
    {
        var backend = new InProcessMeshBackend();
        Assert.Throws<KeyNotFoundException>(
            () => backend.GetBoundingBox(new BodyHandle(Guid.NewGuid())));
    }
}
