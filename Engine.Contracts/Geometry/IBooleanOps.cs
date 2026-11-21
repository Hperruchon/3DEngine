namespace Engine.Contracts.Geometry;

// Boolean capability per ADR-0012 (Amendment 1, P7c). Combines two bodies into
// a new one. These are Manifold mesh booleans — distinct from the reserved BRep
// ExactBooleans (ADR-0001 §1), which stay unimplemented.
//
// Kept separate from IMeshOps because the managed InProcessMeshBackend stores
// only box dimensions and cannot represent an arbitrary carved solid. It does
// not implement this interface, so callers on that backend get a clean
// E-GEOM-CAP-MISSING rather than a silent or dishonest fallback.
public interface IBooleanOps
{
    // Stores (minuend - subtrahend) under `result`: the minuend with the
    // subtrahend's overlapping volume removed. Both operands are left intact.
    // The command handler computes `result` deterministically from the
    // CommandId (ADR-0012 §4). Throws if either operand is unknown to the
    // backend, if `result` is already taken, or if the difference is
    // empty/degenerate (the subtrahend fully contains the minuend).
    void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend);

    // ADR-0181 items 1 and 4 (phase K16, TASK-0433): stores the volume of the two bodies together under `result`. Both
    // operands stay intact. Two bodies that do not touch give one body of two parts. Throws as Subtract does: an unknown
    // operand, a `result` that is already taken, or a result that is empty.
    void Union(BodyHandle result, BodyHandle first, BodyHandle second);

    // ADR-0181 items 3 and 4: stores the volume that the two bodies share under `result`. Both operands stay intact.
    // Throws as Subtract does. Two bodies apart give an empty result, which throws; two bodies that touch on a face can
    // give a flat result of no volume, so the handler first asks the owned kernel whether the bodies share a volume.
    void Intersect(BodyHandle result, BodyHandle first, BodyHandle second);
}
