namespace Engine.Contracts;

// ADR-0191 items 1 and 2 (phase P11.0, TASK-0474): the placement of a body, a turn and an offset. A point p of the shape of the
// body is at R·p + (Dx, Dy, Dz) in the Document, where R is the turn of the quaternion (Qw, Qx, Qy, Qz) that
// Geometry.Rotation.FromQuaternion gives. The record holds the seven numbers that the command PlaceBody holds, so a replay
// gives each placement bit for bit. A body with no placement is at the identity.
public readonly record struct BodyPlacement(double Qw, double Qx, double Qy, double Qz, double Dx, double Dy, double Dz)
{
    // The placement of a body that no PlaceBody moved.
    public static BodyPlacement Identity { get; } = new(1, 0, 0, 0, 0, 0, 0);

    // True for the seven numbers of the identity. A quaternion (2, 0, 0, 0) also gives no turn, but its numbers stay as the
    // command gives them.
    public bool IsIdentity => Qw == 1 && Qx == 0 && Qy == 0 && Qz == 0 && Dx == 0 && Dy == 0 && Dz == 0;
}
