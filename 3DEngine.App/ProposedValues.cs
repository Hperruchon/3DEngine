using System.Globalization;
using System.Text.Json;
using Engine.Core.Queries;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0110 item 1 (phase R16t, TASK-0334): a value that a command of a proposal changes. Constraint and Sketch are set for
// the value of a constraint, and Extrude for the distance of an extrude. Number is the proposed value. Name, Label and Now
// come from the sketches and the bodies of the frame (Named): "Plate", "C3" and 60, or "Bracket", "distance" and 20. Name
// is null when the frame does not hold the value.
internal sealed record ProposedValue(Guid? Sketch, Guid? Constraint, Guid? Extrude, double Number)
{
    public string? Name { get; init; }

    public string? Label { get; init; }

    public double? Now { get; init; }

    // ADR-0114 item 2 (finding V164 of the codebase review of 2026-11-10, TASK-0339): the second value of the command, the
    // sine of an angle; the proposed value and the unit as the line shows them, when they are not Number and the length
    // unit of the document. For an angle Now and Shown are in degrees, and Number stays the cosine of the command.
    public double? Second { get; init; }

    public double? Shown { get; init; }

    public string? Unit { get; init; }

    // ADR-0110 item 2: the keys of the chips of this value: the chip of the constraint on the edge of a body and its chip in
    // the active sketch, or the chip of the distance of the extrude.
    public IReadOnlyList<string> Keys => Constraint is { } constraint
        ? [EdgeValues.ConstraintKey(constraint), SketchValues.Key(constraint)]
        : Extrude is { } extrude ? [EdgeValues.DistanceKey(extrude)] : [];

    // Two proposed values are the same value when they change the same constraint or the same extrude.
    public Guid? Target => Constraint ?? Extrude;
}

// ADR-0110 item 1 (TASK-0334): the proposed values of the commands of a proposal. The values are presentation (ADR-0007):
// they read the parameters that the queue keeps, and send nothing.
internal static class ProposedValues
{
    public const string SetConstraintValue = "SetSketchConstraintValue";
    public const string SetExtrudeDistance = "SetExtrudeDistance";

    // The word of the distance of an extrude in a line of the arc.
    public const string DistanceLabel = "distance";

    // The proposed value of one command, or null. Only the two commands of item 1 have a value. A parameter that is absent,
    // or that does not bind to its type of the schema of the handler (a GUID, or a finite number), gives no value. The names
    // of the parameters are those of the schema, with the case of the schema.
    public static ProposedValue? Of(string name, IEnumerable<KeyValuePair<string, JsonElement>> parameters)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameters);
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
            values[key] = value;

        return name switch
        {
            SetConstraintValue when Id(values, "sketchId") is { } sketch && Id(values, "constraintId") is { } constraint && Number(values, "value") is { } number
                => new ProposedValue(sketch, constraint, null, number) { Second = Number(values, "secondValue") },
            SetExtrudeDistance when Id(values, "extrudeId") is { } extrude && Number(values, "distance") is { } distance
                => new ProposedValue(null, null, extrude, distance),
            _ => null,
        };
    }

    // ADR-0110 item 1: the proposed values of a proposal, from the value of each command in the order of the commands. A later
    // command for the same value replaces the earlier one, at the place of the earlier one.
    public static IReadOnlyList<ProposedValue> Of(IEnumerable<ProposedValue?> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var values = new List<ProposedValue>();
        foreach (var value in commands.OfType<ProposedValue>())
        {
            var earlier = values.FindIndex(candidate => candidate.Target == value.Target);
            if (earlier >= 0)
                values[earlier] = value;
            else
                values.Add(value);
        }

        return values;
    }

    // ADR-0110 item 3: each value of each card with the name, the label and the value now, from the sketches and the bodies of
    // the frame. The value now of a distance comes from the chip of the distance of the selected extrude, because the frame
    // has no other distance; with no chip the value has no value now.
    public static IReadOnlyList<ProposalCard>? Named(IReadOnlyList<ProposalCard>? cards, IReadOnlyCollection<SketchInfo> sketches, IReadOnlyList<LiveBody> bodies,
        IReadOnlyList<EdgeValues.Value>? values)
    {
        ArgumentNullException.ThrowIfNull(sketches);
        ArgumentNullException.ThrowIfNull(bodies);
        return cards?.Select(card => card.Values is { } commands ? card with { Values = [.. commands.Select(value => Named(value, sketches, bodies, values))] } : card).ToList();
    }

    private static ProposedValue? Named(ProposedValue? value, IReadOnlyCollection<SketchInfo> sketches, IReadOnlyList<LiveBody> bodies, IReadOnlyList<EdgeValues.Value>? values)
    {
        if (value is { Sketch: { } sketchId, Constraint: { } constraintId })
        {
            if (sketches.FirstOrDefault(sketch => sketch.SketchId == sketchId) is not { } sketch
                || sketch.Constraints.FirstOrDefault(constraint => constraint.Id == constraintId) is not { } constraint)
                return value;
            // ADR-0034 item 8: the label of a constraint is its place in the constraints of its sketch, from 1. ADR-0114 item 2
            // (finding V164): a distance and a radius are lengths; an angle holds its cosine and its sine (ADR-0033 item 3), so
            // the line shows it in degrees; each other kind keeps the line of its command.
            var named = value with { Name = sketch.Name ?? sketch.SketchId.ToString()[..8], Label = SketchText.Labels(sketch)[constraint.Id] };
            return constraint switch
            {
                { Kind: SketchLength or SketchRadius, Values: [var length] } => named with { Now = length },
                { Kind: SketchAngle, Values: [var cosine, var sine] } when value.Second is { } second
                    => named with { Now = Degrees(cosine, sine), Shown = Degrees(value.Number, second), Unit = DesktopUi.AngleUnit },
                _ => value,
            };
        }

        if (value is { Extrude: { } extrude } && bodies.FirstOrDefault(body => body.Id == extrude) is { } live)
        {
            var now = values?.FirstOrDefault(chip => chip.Constraint is null && chip.Body == extrude);
            return value with { Name = live.Name ?? live.Kind, Label = DistanceLabel, Now = now?.Number };
        }

        return value;
    }

    // The kinds of a constraint whose value the line names (SketchConstraints, ADR-0033 item 3).
    private const string SketchLength = "distance";
    private const string SketchRadius = "radius";
    private const string SketchAngle = "angle";

    // An angle in degrees from its cosine and its sine, as the pad of a sketch shows it. Presentation: nothing of it enters
    // the log (determinism rule 1 covers code that writes to the log only).
    private static double Degrees(double cosine, double sine) => Math.Atan2(sine, cosine) * 180 / Math.PI;

    private static Guid? Id(Dictionary<string, JsonElement> values, string name)
        => values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id) ? id : null;

    // A number of JSON, or a text that the binder reads as a number (ParameterBinder), when it is finite.
    private static double? Number(Dictionary<string, JsonElement> values, string name)
    {
        if (!values.TryGetValue(name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number))
            return number;
        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)
            ? parsed
            : null;
    }
}
