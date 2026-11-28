using Engine.Contracts;
using Engine.Contracts.Joints;

namespace Engine.Core.Queries;

// ADR-0195 items 1 to 5 (phase K12a, TASK-0490): the travel of a pivot from its lower limit to its upper limit. The query turns the
// child of the joint and each body under it in memory and changes nothing: no command, no fork. Tolerance is the gap in
// millimetres at which the travel stops at a contact, 0.01 mm by default.
public sealed record CheckTravelQuery : Query
{
    public override string Name => "CheckTravel";
    public override int SchemaVersion => 1;

    public required Guid JointId { get; init; }
    public double? Tolerance { get; init; }
}

// ADR-0195 item 4: the answer of a travel. Free is true when no body that turns comes within the tolerance of a body that does
// not turn, from the lower to the upper limit, the two included. Else Contact is the first value of the joint at which a sample
// gives a gap of at most the tolerance; no contact comes before it. MovingBodyId and FixedBodyId are the pair of that sample, and
// Gap its gap in millimetres. Samples is the count of the samples, Radius the largest distance in millimetres of a point that
// turns from the axis, and Tolerance the tolerance of the travel. Lower and Upper are the limits of the joint as directions of a
// length of one.
public sealed record TravelInfo(
    Guid JointId,
    bool Free,
    JointValue? Contact,
    Guid? MovingBodyId,
    Guid? FixedBodyId,
    double? Gap,
    int Samples,
    double Radius,
    double Tolerance,
    JointValue Lower,
    JointValue Upper);
