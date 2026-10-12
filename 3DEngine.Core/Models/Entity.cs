namespace ThreeDEngine.Core.Models;

/// <summary>
/// Represents a node in the scene graph.
/// </summary>
public class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Transform Transform { get; set; } = Transform.Identity;

    /// <summary>
    /// The origin of the entity in double precision, in the space of its parent, before
    /// <see cref="Transform"/> (ADR-0024 item 2). A body of the engine keeps its
    /// detail far from the origin of the scene: the host narrows its mesh around the
    /// centre of its box, and that centre is this origin.
    /// </summary>
    public (double X, double Y, double Z) Origin { get; set; }

    public string? MeshId { get; set; }

    public string? MaterialId { get; set; }

    public IList<Entity> Children { get; } = new List<Entity>();
}
