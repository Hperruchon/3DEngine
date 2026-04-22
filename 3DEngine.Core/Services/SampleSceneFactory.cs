using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Services;

/// <summary>
/// Creates a small sample scene that can be reused by hosts and tooling.
/// </summary>
public static class SampleSceneFactory
{
    public static Scene CreateDefault()
    {
        var scene = new Scene
        {
            Name = "Sample Workspace"
        };

        scene.Cameras.Add(new Camera
        {
            Name = "Editor Camera",
            Transform = new Transform(new Vector3(0, 3, -8), Quaternion.Identity, Vector3.One)
        });

        scene.Lights.Add(new Light
        {
            Name = "Sun",
            LightType = LightType.Directional,
            Intensity = 2.5f,
            Transform = new Transform(new Vector3(0, 10, 0), Quaternion.Identity, Vector3.One)
        });

        scene.Entities.Add(new Entity
        {
            Name = "Ground",
            MeshId = "mesh-ground",
            MaterialId = "mat-ground",
            Transform = new Transform(new Vector3(0, -1, 0), Quaternion.Identity, new Vector3(20, 1, 20))
        });

        scene.Entities.Add(new Entity
        {
            Name = "Hero",
            MeshId = "mesh-hero",
            MaterialId = "mat-hero",
            Transform = Transform.Identity
        });

        return scene;
    }
}
