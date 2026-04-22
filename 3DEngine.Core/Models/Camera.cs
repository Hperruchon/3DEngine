namespace ThreeDEngine.Core.Models;

/// <summary>
/// Represents a camera used to view a scene.
/// </summary>
public sealed class Camera : Entity
{
    public float FieldOfViewDegrees { get; set; } = 60.0f;

    public float NearPlane { get; set; } = 0.1f;

    public float FarPlane { get; set; } = 1000.0f;
}
