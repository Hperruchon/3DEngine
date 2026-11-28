using Engine.Contracts;
using Engine.Contracts.Handlers;
using Engine.Contracts.Joints;
using Engine.Contracts.Sketch;
using Engine.Core.Features;
using Engine.Core.Queries;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0193 items 2, 4, 5 and 8 (phase P11a, TASK-0479): the shared checks and results of the commands of a joint.
internal static class JointResults
{
    public static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));

    public static Task<CommandHandlerResult> Refused(string code, string message) => Task.FromResult(Failure(code, message));

    public static JointRecord? Find(Document document, Guid id)
        => document.Joints.FirstOrDefault(joint => joint.Id == id);

    public static CommandHandlerResult NotFound(Guid id)
        => Failure(DiagnosticCodes.JointNotFound, $"No joint with id '{id}' exists in the document.");

    public static CommandHandlerResult Invalid(string message) => Failure(DiagnosticCodes.JointInvalid, message);

    // Item 2 (the answer Q1 A): the connector of one side of a joint. A side names a face with featureId, element and an
    // optional itemId, as GetConnectors names it, or a datum with datumId, and not both. A face is a plane or the side of a
    // cylinder; a datum is in effect, has no fault, and is on a feature whose chain of consumers ends at the body (the rule of
    // GetConnectors). The frame goes into the frame of the body with the placement of the body now.
    public static (JointConnector? Connector, CommandHandlerResult? Refusal) Connector(
        Document document, BodyRecord body, string side, Guid? featureId, string? element, Guid? itemId, Guid? datumId)
    {
        var placement = Placements.Of(body);
        if (datumId is { } id)
        {
            if (featureId is not null || element is not null || itemId is not null)
                return (null, Bad($"The {side} connector names a datum and a face; it names one of them (ADR-0193 item 2)."));
            if (document.Datums.FirstOrDefault(datum => datum.Id == id) is not { } datum)
                return (null, Bad($"The {side} connector names the datum '{id}', which is not a datum in effect."));
            if (KernelProjection.End(FeatureGraph.Of(document), datum.Reference.FeatureId).End != body.Handle.Id)
                return (null, Bad($"The datum '{id}' of the {side} connector is not on the body '{body.Handle.Id}'."));
            if (datum.Fault is { } fault)
                return (null, Bad($"The datum '{id}' of the {side} connector has the fault {fault.Code}, so its frame is not where the design is."));
            return (new JointConnector(null, id, JointTree.InBody(placement, datum.Frame)), null);
        }

        if (featureId is not { } feature || element is not { } name)
            return (null, Bad($"The {side} connector names no face and no datum: a face needs its featureId and its element (ADR-0193 item 2)."));
        var (solid, error) = MeasureTarget.SolidOf(document, body.Handle.Id);
        if (solid is null)
            return (null, Bad($"The {side} connector is on the body '{body.Handle.Id}', which has no faces to read: {error!.Message}"));
        var reference = new DatumReference(feature, name, itemId);
        if (GetConnectorsQueryHandler.Faces(document, body.Handle.Id, solid).FirstOrDefault(face => face.Reference == reference) is not { } found)
            return (null, Bad($"The body '{body.Handle.Id}' of the {side} connector has no face '{feature}/{name}{(itemId is { } item ? "/" + item : "")}'."));
        if (found.Point is not { } point || found.Direction is not { } direction)
            return (null, Bad($"The face '{feature}/{name}' of the {side} connector is curved and not the side of a cylinder, so it gives no frame (ADR-0193 item 2)."));
        if (found.Kind == ConnectorKind.Plane && KernelProjection.OfRecord(document, body, local: true).Solid is { } shape)
        {
            // The centre and the normal of a plane come from the shape of the body, with no placement, so the frame has the same
            // bits at each placement of the body. The curve of the side of a cylinder is in the frame of the Document only.
            var tag = new Engine.Kernel.FaceTag(feature, name, itemId);
            var pieces = shape.Polygons.Where(polygon => polygon.Tag == tag).ToList();
            var (x, y, z) = GetConnectorsQueryHandler.Centre(pieces).ToDoubles();
            return (new JointConnector(reference, null, JointTree.FaceFrame(new DatumPoint(x, y, z), GetConnectorsQueryHandler.Unit(pieces[0].Plane.Normal))), null);
        }

        return (new JointConnector(reference, null, JointTree.InBody(placement, JointTree.FaceFrame(point, direction))), null);
    }

    private static CommandHandlerResult Bad(string message) => Failure(DiagnosticCodes.JointConnectorInvalid, message);

    // Item 4: the value of a joint of the kind, from the numbers of a command. A fixed joint takes no number; a pivot takes a
    // cosine and a sine of a length greater than zero that give a turn; a slider takes a finite distance.
    public static (JointValue? Value, CommandHandlerResult? Refusal) Value(string kind, string what, double? cos, double? sin, double? distance)
    {
        switch (kind)
        {
            case JointRecord.Fixed:
                return cos is null && sin is null && distance is null
                    ? (JointValue.None, null)
                    : (null, Invalid($"A fixed joint takes no {what} (ADR-0193 item 4)."));
            case JointRecord.Pivot:
                if (distance is not null)
                    return (null, Invalid($"A pivot takes a cosine and a sine for its {what}, and no distance (ADR-0193 item 4)."));
                if (cos is not { } c || sin is not { } s)
                    return (null, Invalid($"A pivot takes a cosine and a sine for its {what} (ADR-0193 item 4)."));
                if (!(double.IsFinite(c) && double.IsFinite(s) && double.IsFinite((c * c) + (s * s))) || Placements.Turn(JointTree.Turn(c, s)) is null)
                    return (null, Invalid($"The cosine and the sine of the {what} of a pivot are finite and not both zero, and give a turn (ADR-0193 item 4)."));
                return (new JointValue(c, s, 0), null);
            default:
                if (cos is not null || sin is not null)
                    return (null, Invalid($"A slider takes a distance for its {what}, and no cosine and no sine (ADR-0193 item 4)."));
                if (distance is not { } d || !double.IsFinite(d))
                    return (null, Invalid($"A slider takes a finite distance for its {what} (ADR-0193 item 4)."));
                return (new JointValue(1, 0, d), null);
        }
    }

    // Item 5: the limits of a joint of the kind, or null. The four numbers of the limits of a pivot, or the two distances of a
    // slider, come all together or not at all; a fixed joint has no limits; the lower distance of a slider is not above the upper.
    public static (JointLimits? Limits, CommandHandlerResult? Refusal) Limits(
        string kind, double? lowerCos, double? lowerSin, double? upperCos, double? upperSin, double? lowerDistance, double? upperDistance)
    {
        double?[] turns = [lowerCos, lowerSin, upperCos, upperSin];
        double?[] distances = [lowerDistance, upperDistance];
        if (turns.All(part => part is null) && distances.All(part => part is null))
            return (null, null);
        if (kind == JointRecord.Fixed)
            return (null, Invalid("A fixed joint has no limits (ADR-0193 item 5)."));
        if (kind == JointRecord.Pivot && (distances.Any(part => part is not null) || turns.Any(part => part is null)))
            return (null, Invalid("The limits of a pivot are lowerCos, lowerSin, upperCos and upperSin, all four, and no distance (ADR-0193 item 5)."));
        if (kind == JointRecord.Slider && (turns.Any(part => part is not null) || distances.Any(part => part is null)))
            return (null, Invalid("The limits of a slider are lowerDistance and upperDistance, both, and no cosine and no sine (ADR-0193 item 5)."));

        var (lower, refusal) = Value(kind, "lower limit", lowerCos, lowerSin, lowerDistance);
        if (refusal is not null)
            return (null, refusal);
        var (upper, second) = Value(kind, "upper limit", upperCos, upperSin, upperDistance);
        if (second is not null)
            return (null, second);
        if (kind == JointRecord.Slider && lower!.Distance > upper!.Distance)
            return (null, Invalid($"The lower distance {lower.Distance} of a slider is above its upper distance {upper.Distance} (ADR-0193 item 5)."));
        return (new JointLimits(lower!, upper!), null);
    }

    // Item 5: a value outside the limits of its joint.
    public static CommandHandlerResult? OutsideLimits(string kind, JointValue value, JointLimits? limits)
        => JointTree.InLimits(kind, value, limits)
            ? null
            : Failure(DiagnosticCodes.JointLimit, kind == JointRecord.Pivot
                ? $"The turn ({value.Cos}, {value.Sin}) is outside the limits of the pivot, counterclockwise from ({limits!.Lower.Cos}, {limits.Lower.Sin}) to ({limits.Upper.Cos}, {limits.Upper.Sin}) (ADR-0193 item 5)."
                : $"The distance {value.Distance} is outside the limits of the slider, from {limits!.Lower.Distance} to {limits.Upper.Distance} (ADR-0193 item 5).");

    // Items 3 and 6: the new records of the child of the joint and of each body under it, when the parent and the child are
    // live. The joints are the joints of the Document with the change of the command.
    public static List<BodyRecord> Moved(Document document, IReadOnlyCollection<JointRecord> joints, JointRecord joint)
    {
        var parent = document.Bodies.FirstOrDefault(body => body.Handle.Id == joint.ParentBodyId);
        var child = document.Bodies.FirstOrDefault(body => body.Handle.Id == joint.ChildBodyId);
        // ADR-0194 item 2 (phase P11b, TASK-0489): a loose joint places nothing.
        if (parent is null || child is null || joint.State != JointRecord.Tight)
            return [];
        var at = JointTree.Child(Placements.Of(parent), joint);
        var placed = child with { Placement = Placements.Stored(at) };
        var changed = placed == child ? new List<BodyRecord>() : [placed];
        changed.AddRange(JointTree.Below(document, joints, child.Handle.Id, at));
        return changed;
    }
}
