using Engine.Contracts.Sketch;

namespace Engine.Contracts.Joints;

// ADR-0193 items 1 to 4 (phase P11a, TASK-0479): one joint of the Document, between a parent body and a child body of one part.
// A record never changes: a change of the value gives a new record with the same identifier, as a sheet does. Id is the
// identifier of the command AddJoint. Kind is one of Kinds. Parent and Child are the two connectors, each with its frame in
// the frame of its body. Value is the value of the joint, and Limits is null for a joint with no limits. ADR-0194 item 1 (phase
// P11b, TASK-0489, the answer Q5 A): State is one of States, "tight" by default, so a log of P11a gives the same record.
public sealed record JointRecord(
    Guid Id,
    string Kind,
    Guid ParentBodyId,
    JointConnector Parent,
    Guid ChildBodyId,
    JointConnector Child,
    JointValue Value,
    JointLimits? Limits,
    string State = "tight")
{
    // ADR-0194 item 1: a tight joint gives the placement of its child; a loose joint keeps its record, its connectors, its value
    // and its intended pair, and gives no placement.
    public const string Tight = "tight";
    public const string Loose = "loose";

    public static readonly IReadOnlyList<string> States = [Tight, Loose];

    // ADR-0193 item 1: the three kinds of a joint. A fixed joint has no value, a pivot turns the child about the normal of the
    // parent connector, and a slider moves the child along that normal.
    public const string Fixed = "fixed";
    public const string Pivot = "pivot";
    public const string Slider = "slider";

    public static readonly IReadOnlyList<string> Kinds = [Fixed, Pivot, Slider];
}

// ADR-0193 item 2 (the answer Q1 A): a connector names a face of the body as GetConnectors names it (Face), or a datum in
// effect on the body (DatumId), and not both. Frame is the right-handed frame of the connector in the frame of the body: the
// origin, a direction U at a right angle to the normal, V = Normal × U, and the normal. The command AddJoint reads it one time,
// so the frame follows each later placement of the body.
public sealed record JointConnector(DatumReference? Face, Guid? DatumId, DatumFrame Frame);

// ADR-0193 item 4 (the answer Q3 A): the value of a joint. A pivot holds the cosine and the sine of its turn, as a direction of
// a length greater than zero, and no angle (CLAUDE.md, determinism rule 2). A slider holds a distance in millimetres along the
// normal of the parent connector. The other numbers are 1, 0 and 0, so a fixed joint has the value (1, 0, 0).
public sealed record JointValue(double Cos, double Sin, double Distance)
{
    public static JointValue None { get; } = new(1, 0, 0);
}

// ADR-0193 item 5: the limits of a pivot or of a slider. The values of a pivot go counterclockwise about the normal of the
// parent connector from Lower to Upper; the distance of a slider goes from Lower to Upper.
public sealed record JointLimits(JointValue Lower, JointValue Upper);
