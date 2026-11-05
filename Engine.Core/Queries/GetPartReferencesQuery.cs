using Engine.Contracts;
using Engine.Contracts.Assembly;

namespace Engine.Core.Queries;

// ADR-0071 item 6 (phase P8b1, TASK-0202): the kind of the Document and each reference of an assembly.
public sealed record GetPartReferencesQuery : Query
{
    public override string Name => "GetPartReferences";
    public override int SchemaVersion => 1;
}

// The kind of the Document, and its references in the order of the log. A part has none.
public sealed record PartReferencesInfo(string Kind, IReadOnlyList<PartReference> References);
