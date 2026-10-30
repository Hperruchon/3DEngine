using System.Reflection;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold;

namespace Engine.Tests.Geometry;

// Finding E14 of the codebase review of 2026-09-30 (TASK-0115): the wrapper gave a buffer of
// Marshal.AllocHGlobal to the C++ delete of manifold_delete_*. Each buffer now goes with its
// destructor in place and then with Marshal.FreeHGlobal. The binding holds no delete function,
// and many solids and boxes come and go with no fault of the process.
public class ManifoldOwnershipTests
{
    [Fact]
    public void The_Binding_Holds_No_Delete_Function()
    {
        var binding = typeof(ManifoldGeometryBackend).Assembly.GetType("Engine.Geometry.Manifold.Native.ManifoldNative", throwOnError: true)!;
        var names = binding.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Select(method => method.Name).ToList();

        Assert.Contains("manifold_destruct_manifold", names);
        Assert.Contains("manifold_destruct_box", names);
        Assert.DoesNotContain(names, name => name.StartsWith("manifold_delete_", StringComparison.Ordinal));
    }

    [NativeManifoldFact]
    public void Many_Solids_And_Boxes_Come_And_Go()
    {
        for (var round = 0; round < 20; round++)
        {
            using var backend = new ManifoldGeometryBackend();
            for (var i = 0; i < 50; i++)
            {
                var box = new BodyHandle(Guid.NewGuid());
                var moved = new BodyHandle(Guid.NewGuid());
                backend.CreateBox(box, new BoxParameters(1 + i, 2, 3));
                backend.Translate(moved, box, 1, 0, 0);
                var aabb = backend.GetBoundingBox(moved);
                Assert.Equal(1 - ((1.0 + i) / 2), aabb.MinX, 9);
            }
        }
    }
}
