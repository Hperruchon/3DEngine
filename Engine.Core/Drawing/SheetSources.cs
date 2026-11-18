using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Features;
using Engine.Core.Sketch;

namespace Engine.Core.Drawing;

// ADR-0158 items 1 and 4 (phase P10.3, TASK-0395): the source of a dimension of a sheet in the design now. The read copies the
// number of the design, or doubles a radius into a diameter (ADR-0165 item 4), and no command reads it, so nothing here can
// change a replay.
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
    // ADR-0165 item 4 (phase R30g, TASK-0405): a radius on a whole circle of its sketch is a diameter, with the label "diameter"
    // and twice the radius, for the sign ⌀ of ISO 129-1; a radius on an arc stays a radius.
    public static (string Label, double Value)? Value(Document document, FeatureList features, Guid source, string kind)
    {
        if (kind == Constraint)
        {
            var sketch = document.Sketches.FirstOrDefault(sketch => sketch.Constraints.Any(constraint => constraint.Id == source));
            if (sketch?.Constraints.First(constraint => constraint.Id == source) is not { } constraint || Label(constraint.Kind) is not { } label)
                return null;
            return label == "radius" && constraint.Items.Count == 1 && sketch.Circles.Any(circle => circle.Id == constraint.Items[0])
                ? ("diameter", 2 * constraint.Values[0])
                : (label, constraint.Values[0]);
        }
        if (kind == Distance)
            return features.DistanceInEffect(source) is { } distance ? ("distance", distance) : null;
        return null;
    }
}
