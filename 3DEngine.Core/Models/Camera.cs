using System.Numerics;

namespace ThreeDEngine.Core.Models;

/// <summary>
/// The orbit camera of a host that draws (ADR-0023 item 2). It is render state: a
/// person moves it, it never enters the Document, and two hosts may hold different
/// cameras (ADR-0007, ADR-0009 §3).
/// </summary>
/// <remarks>
/// The camera looks at <see cref="Target"/> from <see cref="Distance"/>, turned by a
/// yaw about the y axis and a pitch above the plane of x and z. The renderer works
/// relative to the eye: the eye is at the origin of each matrix, and each position is
/// first moved by minus the eye in double precision, then narrowed to float.
/// </remarks>
public sealed class Camera : Entity
{
    public const double MinimumPitchDegrees = -89.0;
    public const double MaximumPitchDegrees = 89.0;

    // Each parameter is checked when it is set (finding V11 of the codebase review of
    // 2026-10-13, TASK-0074). Until then a near plane of 0 gave depth 0 to each point,
    // so nothing was drawn, with no message. The far plane must also be beyond the near
    // plane; Projection checks that pair, because a caller can set the two in each order.
    private float _fieldOfViewDegrees = 60.0f;
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000.0f;

    /// <summary>The vertical field of view, greater than 0 and less than 180 degrees.</summary>
    public float FieldOfViewDegrees
    {
        get => _fieldOfViewDegrees;
        set => _fieldOfViewDegrees = float.IsFinite(value) && value > 0 && value < 180
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FieldOfViewDegrees), value, "A field of view is greater than 0 and less than 180 degrees.");
    }

    /// <summary>The distance of the near plane, finite and greater than 0.</summary>
    public float NearPlane
    {
        get => _nearPlane;
        set => _nearPlane = float.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(NearPlane), value, "A near plane is finite and greater than 0.");
    }

    /// <summary>The distance of the far plane, finite and greater than 0.</summary>
    public float FarPlane
    {
        get => _farPlane;
        set => _farPlane = float.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FarPlane), value, "A far plane is finite and greater than 0.");
    }

    /// <summary>The point that the camera looks at, in the space of the scene.</summary>
    public (double X, double Y, double Z) Target { get; set; }

    /// <summary>The distance from the eye to the target.</summary>
    public double Distance { get; set; } = 10.0;

    /// <summary>The turn about the y axis; 0 puts the eye on the positive z side of the target.</summary>
    public double YawDegrees { get; set; }

    /// <summary>The angle of the eye above the plane of x and z through the target.</summary>
    public double PitchDegrees { get; set; }

    /// <summary>Turns the camera about its target. The pitch stays between -89 and 89 degrees.</summary>
    public void Orbit(double yawDegrees, double pitchDegrees)
    {
        YawDegrees = (YawDegrees + yawDegrees) % 360.0;
        PitchDegrees = Math.Clamp(PitchDegrees + pitchDegrees, MinimumPitchDegrees, MaximumPitchDegrees);
    }

    /// <summary>Multiplies the distance by the factor, and keeps the eye between the near plane and the far plane.</summary>
    public void Zoom(double factor)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "A zoom factor is positive.");

        Distance = ClampDistance(Distance * factor);
    }

    /// <summary>
    /// Frames a box (ADR-0025 item 6): the target goes to the centre of the box, and the
    /// distance puts the sphere around the box inside the smaller half angle of the field
    /// of view, for the aspect of the picture. The yaw and the pitch do not change.
    /// </summary>
    public void Frame((double X, double Y, double Z) min, (double X, double Y, double Z) max, float aspect)
    {
        if (aspect <= 0)
            throw new ArgumentOutOfRangeException(nameof(aspect), "An aspect is positive.");

        Target = ((min.X + max.X) / 2.0, (min.Y + max.Y) / 2.0, (min.Z + max.Z) / 2.0);

        var (dx, dy, dz) = (max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var radius = Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2.0;
        var halfVertical = FieldOfViewDegrees * Math.PI / 360.0;
        var halfHorizontal = Math.Atan(Math.Tan(halfVertical) * aspect);
        var half = Math.Min(halfVertical, halfHorizontal);

        Distance = ClampDistance(radius / Math.Sin(half));
    }

    // Keeps the eye between two times the near plane and half the far plane. When the
    // two planes are close, the lower limit wins; until TASK-0074 Math.Clamp threw
    // there, and the throw in the event loop ended the desktop (V11).
    private double ClampDistance(double distance)
        => Math.Max(NearPlane * 2.0, Math.Min(distance, FarPlane / 2.0));

    /// <summary>The position of the eye, in double precision.</summary>
    public (double X, double Y, double Z) Eye()
    {
        var yaw = YawDegrees * Math.PI / 180.0;
        var pitch = PitchDegrees * Math.PI / 180.0;
        return (
            Target.X + Distance * Math.Cos(pitch) * Math.Sin(yaw),
            Target.Y + Distance * Math.Sin(pitch),
            Target.Z + Distance * Math.Cos(pitch) * Math.Cos(yaw));
    }

    /// <summary>
    /// The view matrix with the eye at the origin: it turns the space so that the camera
    /// looks along negative z, with y up. The matrices of System.Numerics act on row
    /// vectors, so a shader that reads one as a column-major matrix gets the transpose,
    /// which acts on column vectors.
    /// </summary>
    public Matrix4x4 View()
    {
        var eye = Eye();
        var forward = new Vector3((float)(Target.X - eye.X), (float)(Target.Y - eye.Y), (float)(Target.Z - eye.Z));
        return Matrix4x4.CreateLookAt(Vector3.Zero, forward, Vector3.UnitY);
    }

    /// <summary>
    /// The reversed perspective projection (ADR-0023 item 1): a point on the near plane
    /// gets depth 1, and a point on the far plane gets depth 0.
    /// </summary>
    public Matrix4x4 Projection(float aspect)
    {
        if (!float.IsFinite(aspect) || aspect <= 0)
            throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "An aspect is finite and greater than 0.");
        if (FarPlane <= NearPlane)
            throw new InvalidOperationException($"The far plane ({FarPlane}) must be beyond the near plane ({NearPlane}).");

        var yScale = 1.0f / MathF.Tan(FieldOfViewDegrees * MathF.PI / 360.0f);
        var near = NearPlane;
        var far = FarPlane;

        return new Matrix4x4(
            yScale / aspect, 0, 0, 0,
            0, yScale, 0, 0,
            0, 0, near / (far - near), -1,
            0, 0, near * far / (far - near), 0);
    }
}
