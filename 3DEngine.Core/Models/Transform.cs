using System.Numerics;

namespace ThreeDEngine.Core.Models;

/// <summary>
/// Describes the transform of an entity in the scene graph.
/// </summary>
/// <remarks>
/// A scale has no negative component. A negative scale mirrors the body, and the
/// pipeline then culls its outside, so the body shows inside out (finding V10 of the
/// codebase review of 2026-10-13, TASK-0074). A scale of 0 is permitted; it draws no
/// area.
/// </remarks>
public readonly record struct Transform(Vector3 Position, Quaternion Rotation, Vector3 Scale)
{
    private readonly Vector3 _scale = Checked(Scale);

    public Vector3 Scale
    {
        get => _scale;
        init => _scale = Checked(value);
    }

    public static Transform Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);

    private static Vector3 Checked(Vector3 scale)
        => scale.X < 0 || scale.Y < 0 || scale.Z < 0 || !float.IsFinite(scale.X) || !float.IsFinite(scale.Y) || !float.IsFinite(scale.Z)
            ? throw new ArgumentOutOfRangeException(nameof(Scale), scale, "A scale is finite and has no negative component; a negative scale mirrors the body.")
            : scale;
}
