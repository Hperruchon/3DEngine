using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0052 items 2, 3, 4 and 7. A vertex adds one point with the identifier of the command; an edge
// adds a line with the identifier of the command and two points with derived identifiers. The points
// have no variable in the solver. The new record goes through the solver, and each extrude of the
// sketch follows, as after each edit of a sketch.
public sealed class AddSketchReferenceCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSketchReference";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["featureId"] = new("guid", Required: true),
            ["element"] = new("string", Required: true),
            ["itemId"] = new("guid", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["referenceId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddSketchReferenceCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["featureId"] = typed.FeatureId,
            ["element"] = typed.Element,
            ["itemId"] = typed.ItemId,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddSketchReferenceCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        FeatureId = (Guid)input.Parameters["featureId"]!,
        Element = (string)input.Parameters["element"]!,
        ItemId = input.Parameters.GetValueOrDefault("itemId") as Guid?,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddSketchReferenceCommand)command;

        // Finding E86 of the codebase review of 2026-10-29: the item takes the identifier of the
        // command, so a command that the log holds is refused before it gives a second item.
        if (SketchResults.InUse(document, command, createsItem: true) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, add.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(add.SketchId));

        // Item 7: a feature that depends on the sketch would make the sketch depend on itself.
        var graph = FeatureGraph.Of(document);
        if (DatumResolver.DependsOn(document, graph, add.FeatureId, sketch.Id))
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchReferenceCycle,
                $"The feature '{add.FeatureId}' depends on the sketch '{sketch.Id}', so a reference to it would make the sketch depend on itself."));

        var target = new DatumReference(add.FeatureId, add.Element, add.ItemId);
        var (positions, reason) = DatumResolver.Positions(
            target,
            graph,
            int.MaxValue,
            extrude => new DatumResolver.ExtrudeState(
                SketchModel.Find(document, extrude.SketchId),
                SketchRebuild.DistanceInEffect(document, extrude.CommandId) ?? extrude.Distance),
            id => DatumResolver.Frame(document, id),
            sketch);
        if (positions is null)
            return Task.FromResult(SketchResults.Failure(DiagnosticCodes.SketchReferenceInvalid, reason!));

        // Item 2: the identifiers of the new items depend on the command only, so a replay gives them again.
        var id = command.CommandId;
        IReadOnlyList<Guid> points = positions.Count == 1
            ? [id]
            : [GeometryKeys.Derive(id, DatumResolver.FirstEnd), GeometryKeys.Derive(id, DatumResolver.SecondEnd)];
        var record = sketch with
        {
            Points = sketch.Points.Concat(points.Zip(positions, (point, at) => new SketchPoint(point, at.X, at.Y))).ToList().AsReadOnly(),
            Lines = positions.Count == 1 ? sketch.Lines : sketch.Lines.Append(new SketchLine(id, points[0], points[1])).ToList().AsReadOnly(),
            References = sketch.References.Append(new SketchReference(id, target, points, positions.Count == 1 ? null : id, null)).ToList().AsReadOnly(),
        };
        return Task.FromResult(SketchResults.Changed(document, backend, id, record, "referenceId", id));
    }
}
