using System.Numerics;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Rendering;

namespace ThreeDEngine.Tests;

// The walk of the scene graph into the draw list (ADR-0023 items 3 and 4,
// TASK-0070), with no GPU.
public class SceneWalkerTests
{
    [Fact]
    public void Each_Entity_With_A_Mesh_Gives_One_Record_And_A_Child_Is_Walked()
    {
        var scene = new Scene();
        var group = new Entity { Name = "Group" };
        group.Children.Add(new Entity { Name = "Part", MeshId = "part" });
        scene.Entities.Add(group);
        scene.Entities.Add(new Entity { Name = "Body", MeshId = "body" });
        var list = new DrawList();

        SceneWalker.Fill(scene, new Camera { Distance = 10 }, list);

        Assert.Equal(["part", "body"], list.Records.Select(record => record.MeshId));
    }

    // With no turn the view is the identity, so the model-view matrix of an entity
    // moves its origin to its position minus the eye.
    [Fact]
    public void The_Matrix_Of_A_Record_Is_Relative_To_The_Eye()
    {
        var scene = new Scene();
        scene.Entities.Add(new Entity { MeshId = "body", Transform = Transform.Identity with { Position = new Vector3(1_000_000, 2, 3) } });
        var camera = new Camera { Target = (1_000_000, 2, 3), Distance = 10 };
        var list = new DrawList();

        SceneWalker.Fill(scene, camera, list);
        var origin = Vector3.Transform(Vector3.Zero, list.Records.Single().ModelView);

        Assert.Equal(new Vector3(0, 0, -10), origin);
    }

    // ADR-0024 item 2: a float holds no fraction at 1e9, so an origin in a float
    // Transform would put this body at the eye. The origin in double precision keeps
    // the quarter.
    [Fact]
    public void An_Origin_Far_From_The_Scene_Origin_Keeps_Its_Detail()
    {
        var scene = new Scene();
        scene.Entities.Add(new Entity { MeshId = "body", Origin = (1e9 + 0.25, 0, 0) });
        var camera = new Camera { Target = (1e9, 0, 0), Distance = 10 };
        var list = new DrawList();

        SceneWalker.Fill(scene, camera, list);
        var origin = Vector3.Transform(Vector3.Zero, list.Records.Single().ModelView);

        Assert.Equal(new Vector3(0.25f, 0, -10), origin);
        Assert.Equal(1e9f, (float)(1e9 + 0.25));
    }

    [Fact]
    public void A_Child_Is_Placed_And_Turned_By_Its_Parent()
    {
        var scene = new Scene();
        var parent = new Entity
        {
            Transform = new Transform(new Vector3(10, 0, 0), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2), Vector3.One),
        };
        parent.Children.Add(new Entity { MeshId = "child", Transform = Transform.Identity with { Position = new Vector3(1, 0, 0) } });
        scene.Entities.Add(parent);
        var camera = new Camera { Target = (10, 0, -1), Distance = 10 };
        var list = new DrawList();

        SceneWalker.Fill(scene, camera, list);
        var origin = Vector3.Transform(Vector3.Zero, list.Records.Single().ModelView);

        // The parent turns the offset (1, 0, 0) to (0, 0, -1), so the child is at the
        // target, at the distance of the camera on the negative z axis of the view.
        Assert.Equal(0, origin.X, 4);
        Assert.Equal(0, origin.Y, 4);
        Assert.Equal(-10, origin.Z, 4);
    }
}
