using Engine.Contracts;
using Engine.Contracts.Joints;

namespace Engine.Core.Queries;

// ADR-0193 item 7 (phase P11a, TASK-0479): the joints of the Document, as GetSheets gives the sheets.
public sealed record GetJointsQuery : Query
{
    public override string Name => "GetJoints";
    public override int SchemaVersion => 1;
}

// The result of GetJoints: each joint in the order of the log of its AddJoint, with its whole record.
public sealed record JointsInfo(IReadOnlyList<JointRecord> Joints);
