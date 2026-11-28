using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0191 item 3 (phase P11.0, TASK-0474): sets the placement of a live body to the turn of the quaternion (Qw, Qx, Qy, Qz)
// and the offset (Dx, Dy, Dz). The placement is absolute: the command holds the new placement, not a change of it. The body
// keeps its handle, its kind and its shape; only its record changes, as for a rename. The log holds the seven numbers and no
// angle (CLAUDE.md, determinism rule 2).
public sealed record PlaceBodyCommand : Command
{
    public override string Name => "PlaceBody";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required double Qw { get; init; }
    public required double Qx { get; init; }
    public required double Qy { get; init; }
    public required double Qz { get; init; }
    public required double Dx { get; init; }
    public required double Dy { get; init; }
    public required double Dz { get; init; }

    // The placement that the command gives.
    public BodyPlacement Placement => new(Qw, Qx, Qy, Qz, Dx, Dy, Dz);
}
