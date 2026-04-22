namespace ThreeDEngine.Core.Models;

/// <summary>
/// Represents the aggregate state of a 3D scene.
/// </summary>
public sealed class Scene
{
    public string Name { get; set; } = "Untitled Scene";

    public IList<Entity> Entities { get; } = new List<Entity>();

    public IList<Camera> Cameras { get; } = new List<Camera>();

    public IList<Light> Lights { get; } = new List<Light>();
}
