using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Core.Sketching;

// The turn of the camera at the start of the sketch mode (ADR-0034 item 6, Q4 A): the eye looks
// along the normal of the plane, from the side of the plane where the eye is now. The target and
// the distance stay. The camera is a state of the client (ADR-0007), so this is presentation and
// the trigonometry here never reaches the log.
public static class SketchCamera
{
    public static void Face(Camera camera, (double X, double Y, double Z) normal)
    {
        ArgumentNullException.ThrowIfNull(camera);
        var length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
        if (!(length > 0))
            throw new ArgumentOutOfRangeException(nameof(normal), "A normal has a length.");

        var eye = camera.Eye();
        var side = (eye.X - camera.Target.X) * normal.X + (eye.Y - camera.Target.Y) * normal.Y + (eye.Z - camera.Target.Z) * normal.Z;
        var sign = side < 0 ? -1.0 : 1.0;
        var (x, y, z) = (Plain(sign * normal.X / length), sign * normal.Y / length, Plain(sign * normal.Z / length));

        // Camera.Eye: the direction from the target to the eye is
        // (cos pitch · sin yaw, sin pitch, cos pitch · cos yaw). A vertical direction keeps the yaw,
        // and the setter stops the pitch at the limit of the camera.
        camera.PitchDegrees = Math.Asin(Math.Clamp(y, -1, 1)) * 180.0 / Math.PI;
        if (x * x + z * z > 1e-12)
            camera.YawDegrees = Math.Atan2(x, z) * 180.0 / Math.PI;
    }

    // A negative zero gives Atan2 the other half-turn: Atan2(-0, -1) is -180 degrees, not 180.
    private static double Plain(double value) => value == 0 ? 0 : value;
}
