using System.Numerics;

namespace ThreeDEngine.Core.Models;

/// <summary>
/// Describes the transform of an entity in the scene graph.
/// </summary>
public readonly record struct Transform(Vector3 Position, Quaternion Rotation, Vector3 Scale)
{
    public static Transform Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);
}
