using Engine.Contracts;

namespace Engine.Core.Queries;

// ADR-0054 item 5: the ordered feature list of the Document.
public sealed record GetFeaturesQuery : Query
{
    public override string Name => "GetFeatures";
    public override int SchemaVersion => 1;
}

// The result of GetFeatures: each feature in effect, in the order of the log.
public sealed record FeaturesInfo(IReadOnlyList<FeatureInfo> Features);

// ADR-0054 items 2 and 3. FeatureId is the identifier of the command, which is also the identifier of the object that it
// creates. Kind is the name of the command. Inputs are the identifiers that the feature reads, in the order of its fields:
// a body, a sketch, a datum (an origin plane too), or the feature of a datum or of a reference of a sketch. ConsumedBy is
// the feature in effect that consumes the body, or null. State is correct, failed, consumed or unresolved, and Fault is
// the reason of a failed or an unresolved feature, or null.
public sealed record FeatureInfo(Guid FeatureId, string Kind, IReadOnlyList<Guid> Inputs, Guid? ConsumedBy, string State, FeatureFault? Fault);

public sealed record FeatureFault(string Code, string Message);
