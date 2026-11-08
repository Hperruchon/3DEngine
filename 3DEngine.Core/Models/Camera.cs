using System.Numerics;

namespace ThreeDEngine.Core.Models;

/// <summary>
/// The orbit camera of a host that draws (ADR-0023 item 2). It is render state: a
/// person moves it, it never enters the Document, and two hosts may hold different
/// cameras (ADR-0007, ADR-0009 §3).
/// </summary>
/// <remarks>
/// The camera looks at <see cref="Target"/> from <see cref="Distance"/>, turned by a
/// yaw about the up axis and a pitch above the plane under it: the y axis and the plane of x and z, or with
/// <see cref="ZUp"/> the z axis and the plane of x and y (V151, TASK-0311). The renderer works
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
    private float _farPlane = DefaultFarPlane;
    private double _distance = 10.0;
    private float _frameFill = 1.0f;

    /// <summary>The vertical field of view, greater than 0 and less than 180 degrees.</summary>
    public float FieldOfViewDegrees
    {
        get => _fieldOfViewDegrees;
        set => _fieldOfViewDegrees = float.IsFinite(value) && value > 0 && value < 180
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FieldOfViewDegrees), value, "A field of view is greater than 0 and less than 180 degrees.");
    }

    /// <summary>
    /// The part of the smaller half angle of the field of view that a framed sphere fills, greater than 0 and at most 1.
    /// ADR-0094 (the redesign, slice k, TASK-0305): the camera of the session frames a part at 0.45, as the render of the
    /// designer brief draws it; 1 is the frame of ADR-0025 item 6, the sphere at the edge of the picture.
    /// </summary>
    public float FrameFill
    {
        get => _frameFill;
        set => _frameFill = float.IsFinite(value) && value > 0 && value <= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FrameFill), value, "A frame fill is greater than 0 and at most 1.");
    }

    /// <summary>
    /// True when the z axis is up. ADR-0096 (the redesign, slice m, TASK-0307): the camera of the session has z up, as the
    /// render of the designer brief draws a part, so a sketch on the plane of x and y lies flat. The yaw then turns about
    /// the z axis, a yaw of 0 puts the eye on the negative y side of the target, and the pitch is above the plane of x and
    /// y. False keeps y up. The orbit, the pan and the view use the frame of y up and turn it by a quarter turn about x.
    /// </summary>
    public bool ZUp { get; set; }

    /// <summary>A direction of the frame of y up, in the world: the quarter turn about x when z is up.</summary>
    public (double X, double Y, double Z) ToWorld((double X, double Y, double Z) d)
        => ZUp ? (d.X, -d.Z, d.Y) : d;

    /// <summary>A direction of the world, in the frame of y up: the inverse of <see cref="ToWorld"/>.</summary>
    public (double X, double Y, double Z) FromWorld((double X, double Y, double Z) d)
        => ZUp ? (d.X, d.Z, -d.Y) : d;

    /// <summary>
    /// The up axis of the world: z when <see cref="ZUp"/>, else y. The view, the ray of a pick, the overlay of a sketch and
    /// the gizmo take the right of the screen from the cross product of the forward direction and this axis.
    /// </summary>
    public (double X, double Y, double Z) UpAxis => ZUp ? (0, 0, 1) : (0, 1, 0);

    /// <summary>The distance of the near plane, finite and greater than 0.</summary>
    public float NearPlane
    {
        get => _nearPlane;
        set => _nearPlane = float.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(NearPlane), value, "A near plane is finite and greater than 0.");
    }

    /// <summary>The far plane of a new camera.</summary>
    public const float DefaultFarPlane = 1000.0f;

    /// <summary>
    /// Register entry R-0051 (TASK-0179): the far plane that holds a box of the scene. The
    /// far plane of 1000 cut a model in millimetres larger than about 1000 mm, because
    /// Frame and Zoom keep the distance at half the far plane. The far plane is a distance
    /// from the eye, so the radius of the box sets it. Sixteen times the radius keeps a
    /// framed box whole for an aspect down to 0.3: there Frame puts the eye at about 5.9
    /// times the radius, which is less than half the far plane, and the far side of the box
    /// at about 6.9 times the radius. The far plane is never less than the default.
    ///
    /// Finding V78 of the codebase review of 2026-11-05 (TASK-0181): the rule held each point
    /// only while the target was inside the box, so a hide of a body near the target put a
    /// visible body far from it beyond the far plane. The far plane now also holds two times
    /// the distance from the eye to the far side of the box, the distance from the eye to its
    /// centre plus its radius, wherever the target is. Zoom keeps the eye at half the far
    /// plane, so the person can still zoom out from a box.
    /// </summary>
    public float FarPlaneFor((double X, double Y, double Z) min, (double X, double Y, double Z) max)
    {
        var (dx, dy, dz) = (max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var radius = Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2.0;
        var eye = Eye();
        var (cx, cy, cz) = ((min.X + max.X) / 2.0 - eye.X, (min.Y + max.Y) / 2.0 - eye.Y, (min.Z + max.Z) / 2.0 - eye.Z);
        var reach = Math.Sqrt(cx * cx + cy * cy + cz * cz) + radius;
        var far = Math.Max(DefaultFarPlane, Math.Max(16.0 * radius, Math.Max(2.0 * reach, 2.0 * Distance)));
        return double.IsFinite(far) && far < float.MaxValue ? (float)far : float.MaxValue;
    }

    /// <summary>The distance of the far plane, finite and greater than 0.</summary>
    public float FarPlane
    {
        get => _farPlane;
        set => _farPlane = float.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FarPlane), value, "A far plane is finite and greater than 0.");
    }

    /// <summary>The point that the camera looks at, in the space of the scene, with finite coordinates.</summary>
    // Finding V38 of the second codebase review of 2026-10-28 (TASK-0099): a target of NaN gave
    // a view of NaN. The setter refuses it, as the setter of the distance does.
    public (double X, double Y, double Z) Target
    {
        get => _target;
        set => _target = double.IsFinite(value.X) && double.IsFinite(value.Y) && double.IsFinite(value.Z)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Target), value, "A target has finite coordinates.");
    }

    private (double X, double Y, double Z) _target;

    /// <summary>The distance from the eye to the target, finite and greater than 0.</summary>
    /// <remarks>
    /// Finding V22 of the codebase review of 2026-10-17 (TASK-0079): a distance of 0 put
    /// the eye on the target, and the view had no direction and gave NaN.
    /// </remarks>
    public double Distance
    {
        get => _distance;
        set => _distance = double.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Distance), value, "A distance is finite and greater than 0.");
    }

    /// <summary>
    /// The turn about the up axis. With y up, 0 puts the eye on the positive z side of the target; with <see cref="ZUp"/>, on
    /// the negative y side (V151, TASK-0311).
    /// </summary>
    // V38: a yaw of NaN or of an infinity gave a view of NaN. The setter gives 0 for a value that
    // is not finite, as the setter of the pitch does for NaN.
    public double YawDegrees
    {
        get => _yawDegrees;
        set => _yawDegrees = double.IsFinite(value) ? value : 0.0;
    }

    private double _yawDegrees;

    /// <summary>
    /// The angle of the eye above the plane through the target under the up axis: the plane of x and z, or with
    /// <see cref="ZUp"/> the plane of x and y (V151, TASK-0311).
    /// </summary>
    // Finding V30 of the codebase review of 2026-10-28 (TASK-0090): a pitch of 90 degrees put
    // the forward vector on the up vector, and the view was NaN. The setter clamps to the
    // range of Orbit.
    public double PitchDegrees
    {
        get => _pitchDegrees;
        set => _pitchDegrees = double.IsNaN(value) ? 0.0 : Math.Clamp(value, MinimumPitchDegrees, MaximumPitchDegrees);
    }

    private double _pitchDegrees;

    /// <summary>Turns the camera about its target. The pitch stays between -89 and 89 degrees.</summary>
    public void Orbit(double yawDegrees, double pitchDegrees)
    {
        YawDegrees = (YawDegrees + yawDegrees) % 360.0;
        PitchDegrees = Math.Clamp(PitchDegrees + pitchDegrees, MinimumPitchDegrees, MaximumPitchDegrees);
    }

    /// <summary>Multiplies the distance by the factor, and keeps the eye between the near plane and the far plane.</summary>
    public void Zoom(double factor)
    {
        if (!(double.IsFinite(factor) && factor > 0))
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "A zoom factor is finite and positive.");

        Distance = ClampDistance(Distance * factor);
    }

    /// <summary>
    /// Moves the target, and the eye with it, in the plane of the view (ADR-0026, TASK-0082):
    /// a pointer that moves by these pixels of a picture of this height moves the scene with
    /// it, at the depth of the target. The yaw, the pitch and the distance do not change.
    /// </summary>
    public void Pan(double dxPixels, double dyPixels, double pictureHeight)
    {
        if (!(pictureHeight > 0))
            throw new ArgumentOutOfRangeException(nameof(pictureHeight), "A picture height is positive.");

        // The size of one pixel at the depth of the target.
        var scale = 2.0 * Distance * Math.Tan(FieldOfViewDegrees * Math.PI / 360.0) / pictureHeight;

        var yaw = YawDegrees * Math.PI / 180.0;
        var pitch = PitchDegrees * Math.PI / 180.0;

        // The right and up directions of the view, from the eye toward the target.
        var right = ToWorld((Math.Cos(yaw), 0.0, -Math.Sin(yaw)));
        var up = ToWorld((-Math.Sin(pitch) * Math.Sin(yaw), Math.Cos(pitch), -Math.Sin(pitch) * Math.Cos(yaw)));

        // A drag to the right moves the scene to the right, so the target moves to the left;
        // a drag down (y down on the screen) moves the target up.
        var (mx, my) = (-dxPixels * scale, dyPixels * scale);
        Target = (
            Target.X + right.X * mx + up.X * my,
            Target.Y + right.Y * mx + up.Y * my,
            Target.Z + right.Z * mx + up.Z * my);
    }

    /// <summary>
    /// Frames a box (ADR-0025 item 6): the target goes to the centre of the box, and the
    /// distance puts the sphere around the box inside the smaller half angle of the field
    /// of view, for the aspect of the picture, or the part FrameFill of it (ADR-0094). The yaw and the pitch do not change.
    /// </summary>
    /// <returns>
    /// False when the sphere around the box is not between the near plane and the far
    /// plane at that distance, so that a part of the box is not drawn. The caller reports
    /// it (finding V22 of the codebase review of 2026-10-17, TASK-0079). Until TASK-0079 a
    /// box of 3,000 units gave a distance clamped to 500, with the eye inside the body, and
    /// nothing said so.
    /// </returns>
    public bool Frame((double X, double Y, double Z) min, (double X, double Y, double Z) max, float aspect)
    {
        // NaN passed the old check "aspect <= 0" and gave a distance of NaN (V22).
        if (!(float.IsFinite(aspect) && aspect > 0))
            throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "An aspect is finite and positive.");

        Target = ((min.X + max.X) / 2.0, (min.Y + max.Y) / 2.0, (min.Z + max.Z) / 2.0);

        var (dx, dy, dz) = (max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var radius = Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2.0;
        var halfVertical = FieldOfViewDegrees * Math.PI / 360.0;
        var halfHorizontal = Math.Atan(Math.Tan(halfVertical) * aspect);
        var half = Math.Min(halfVertical, halfHorizontal);
        // ADR-0094: the sphere fills FrameFill of the half picture: its tangent is that part of the tangent of the half angle.
        var filled = Math.Atan(Math.Tan(half) * FrameFill);

        Distance = ClampDistance(radius / Math.Sin(filled));
        return Distance - radius >= NearPlane && Distance + radius <= FarPlane;
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
        var d = ToWorld((Math.Cos(pitch) * Math.Sin(yaw), Math.Sin(pitch), Math.Cos(pitch) * Math.Cos(yaw)));
        return (Target.X + Distance * d.X, Target.Y + Distance * d.Y, Target.Z + Distance * d.Z);
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
        return Matrix4x4.CreateLookAt(Vector3.Zero, forward, ZUp ? Vector3.UnitZ : Vector3.UnitY);
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
