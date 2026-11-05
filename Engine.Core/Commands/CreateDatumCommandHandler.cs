using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0051 items 4 and 7. The reference resolves now, on each step in effect, with the current
// sketch and the distance in effect of an extrude (item 2). A name follows the rules of the name
// of a sketch (ADR-0028 item 3); an empty name is no name. The handler needs no capability of the
// backend: a datum is not a body.
public sealed class CreateDatumCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateDatum";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["featureId"] = new("guid", Required: true),
            ["element"] = new("string", Required: true),
            ["itemId"] = new("guid", Required: false),
            ["name"] = new("string", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["datumId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (CreateDatumCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["featureId"] = typed.FeatureId,
            ["element"] = typed.Element,
            ["itemId"] = typed.ItemId,
            ["name"] = typed.DatumName,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new CreateDatumCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        FeatureId = (Guid)input.Parameters["featureId"]!,
        Element = (string)input.Parameters["element"]!,
        ItemId = input.Parameters.GetValueOrDefault("itemId") as Guid?,
        DatumName = input.Parameters.GetValueOrDefault("name") as string,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var create = (CreateDatumCommand)command;

        // The datum takes the identifier of the command, so a command that the log holds is refused
        // before it gives a second datum, as for a sketch and an image.
        if (Features.FeatureList.Of(document).Holds(command.CommandId))
            return Task.FromResult(Failure(
                DiagnosticCodes.DatumIdInUse,
                $"The log holds a command with the id '{command.CommandId}', and a datum takes the id of its command."));

        if (create.DatumName is { } name && BodyNames.Refusal(name) is { } refusal)
            return Task.FromResult(Failure(DiagnosticCodes.DatumNameInvalid, refusal));

        var reference = new DatumReference(create.FeatureId, create.Element, create.ItemId);
        var graph = FeatureGraph.Of(document);
        var (frame, reason) = DatumResolver.Resolve(
            reference,
            graph,
            int.MaxValue,
            extrude => new DatumResolver.ExtrudeState(
                SketchModel.Find(document, extrude.SketchId),
                SketchRebuild.DistanceInEffect(document, extrude.CommandId) ?? extrude.Distance),
            id => DatumResolver.Frame(document, id));
        if (frame is null)
            return Task.FromResult(Failure(DiagnosticCodes.DatumReferenceInvalid, reason!));

        // ADR-0059 item 3 (phase K4c, TASK-0185): a face that a cut removed resolves its plane, but it is not a face of the
        // body at the end of its chain now, so a new datum refuses it.
        var (end, cut) = Features.KernelProjection.End(graph, reference.FeatureId);
        if (cut && end is { } handle && document.Bodies.FirstOrDefault(body => body.Handle.Id == handle) is { } live
            && Features.KernelProjection.OfRecord(document, live).Solid is { } solid
            && !FaceGone.Holds(Features.KernelProjection.Tags(solid), reference))
            return Task.FromResult(Failure(
                DiagnosticCodes.DatumReferenceInvalid,
                $"The face '{reference.Element}' of '{reference.FeatureId}' is gone: no face of the body '{handle}' has its name."));

        var datum = new DatumRecord(command.CommandId, string.IsNullOrEmpty(create.DatumName) ? null : create.DatumName, reference, frame, null);
        var outputs = new Outputs(new Dictionary<string, object?> { ["datumId"] = datum.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, createdDatums: [datum]));
    }

    private static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));
}
