namespace ThreeDEngine.Core.Models;

/// <summary>
/// Represents a node in the scene graph.
/// </summary>
public class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Transform Transform { get; set; } = Transform.Identity;

    public string? MeshId { get; set; }

    public string? MaterialId { get; set; }

    public IList<Entity> Children { get; } = new List<Entity>();
}
