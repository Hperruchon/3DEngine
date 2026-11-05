namespace Engine.Contracts.Assembly;

// ADR-0071 item 3 (phase P8b1, TASK-0202): one reference of an assembly to a part at a fixed version. A record never
// changes. Part is the name of the bytes of the part file in the store of the account (ADR-0044 item 1): its header and
// its first Version entries, so the content of the reference cannot change. X, Y and Z place the origin of the part in
// the assembly, in the length unit of the Document. The identifier is the identifier of the command.
public sealed record PartReference(Guid Id, string Part, long Version, double X, double Y, double Z);
