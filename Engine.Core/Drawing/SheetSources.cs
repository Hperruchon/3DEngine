using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Features;
using Engine.Core.Sketch;

namespace Engine.Core.Drawing;

// ADR-0158 items 1 and 4 (phase P10.3, TASK-0395): the source of a dimension of a sheet in the design now. The read copies the
// number of the design and uses no arithmetic, so nothing here can change a replay.
internal static class SheetSources
{
    public const string Constraint = "constraint";
    public const string Distance = "distance";

    // ADR-0158 item 1 (Q1 A): the kinds of a source. The list stays in Engine.Core, so the contract gets no new name.
    public static readonly IReadOnlyList<string> Kinds = [Constraint, Distance];

    // The constraint with the identifier in a live sketch, or null. The identifier of a constraint is the identifier of its
    // command, so no two live constraints have the same one.
    public static SketchConstraint? FindConstraint(Document document, Guid id)
        => document.Sketches.SelectMany(sketch => sketch.Constraints).FirstOrDefault(constraint => constraint.Id == id);

    // ADR-0158 item 4: the label of a constraint kind with one length as its first value, or null for each other kind.
    public static string? Label(string constraintKind) => constraintKind switch
    {
        SketchConstraints.Distance => "length",
        SketchConstraints.Radius => "radius",
        _ => null,
    };

    // ADR-0158 item 4: the label and the number of the source in the design now, or null when the source is not in the design
    // (the dimension is lost). "Lost" is never stored: an undo that gives the source back gives the number back.
    public static (string Label, double Value)? Value(Document document, FeatureList features, Guid source, string kind)
    {
        if (kind == Constraint)
            return FindConstraint(document, source) is { } constraint && Label(constraint.Kind) is { } label ? (label, constraint.Values[0]) : null;
        if (kind == Distance)
            return features.DistanceInEffect(source) is { } distance ? ("distance", distance) : null;
        return null;
    }
}
