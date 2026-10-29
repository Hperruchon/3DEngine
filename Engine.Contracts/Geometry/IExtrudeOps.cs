namespace Engine.Contracts.Geometry;

// Extrude capability per ADR-0036 item 5. Makes a solid from one closed loop in a plane.
//
// Kept separate from IMeshOps because the managed InProcessMeshBackend stores only box
// dimensions and cannot represent a prism. It does not implement this interface, so a
// caller on that backend gets a clean E-GEOM-CAP-MISSING (ADR-0012 item 7).
public interface IExtrudeOps
{
    // Stores under `handle` the prism of the loop of `parameters`, from the plane of the
    // frame to the height along N. The command handler computes `handle` from the
    // CommandId (ADR-0012 §4) and gives a loop that is closed, simple and
    // counter-clockwise. Throws if `handle` is already taken.
    void Extrude(BodyHandle handle, ExtrudeParameters parameters);
}
