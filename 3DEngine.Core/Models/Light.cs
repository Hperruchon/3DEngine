using System.Numerics;

namespace ThreeDEngine.Core.Models;

public enum LightType
{
    Directional,
    Point,
    Spot
}

/// <summary>
/// Represents a light that influences scene rendering.
/// </summary>
public sealed class Light : Entity
{
    public LightType LightType { get; set; } = LightType.Directional;

    public Vector3 Color { get; set; } = Vector3.One;

    public float Intensity { get; set; } = 1.0f;
}
