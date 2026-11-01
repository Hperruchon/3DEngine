using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0049 items 1 to 6: the new distance of an extrude in effect. The extrude and each step of
// its chain rebuild as for an edit of its sketch, and a rebuild that fails gives the body a fault
// and never refuses the command.
public sealed class SetExtrudeDistanceCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "SetExtrudeDistance";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["extrudeId"] = new("guid", Required: true),
            ["distance"] = new("number", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["extrudeId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (SetExtrudeDistanceCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["extrudeId"] = typed.ExtrudeId,
            ["distance"] = typed.Distance,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new SetExtrudeDistanceCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        ExtrudeId = (Guid)input.Parameters["extrudeId"]!,
        Distance = (double)input.Parameters["distance"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var set = (SetExtrudeDistanceCommand)command;

        // Item 2: the rule of SketchResults.InUse for a command that creates no item. A command sent
        // again would reuse the keys of its rebuild (finding E95), and an entry of a log applies as
        // the session that wrote it did (finding E100).
        if (!SketchResults.ApplyingEntry.Value && document.Log.Any(entry => entry.Command.CommandId == command.CommandId))
            return Task.FromResult(Failure(
                DiagnosticCodes.FeatureIdInUse,
                $"The log holds a command with the id '{command.CommandId}', and the rebuild of an edit takes the id of its command."));

        // Item 2: a signed distance that is finite and not zero, as Extrude requires (ADR-0036 item 3).
        if (!double.IsFinite(set.Distance) || set.Distance == 0)
            return Task.FromResult(Failure(
                DiagnosticCodes.GeomInvalidParam, "SetExtrudeDistance requires a distance that is finite and not zero."));

        // Item 2: an extrude in effect, live or consumed.
        if (SketchRebuild.ExtrudeInEffect(document.Log, set.ExtrudeId) is not { } extrude)
            return Task.FromResult(Failure(
                DiagnosticCodes.FeatureNotFound, $"No Extrude in effect has the id '{set.ExtrudeId}'."));

        var (bodies, diagnostics) = SketchRebuild.RebuildDistance(document, backend, set.CommandId, extrude, set.Distance);
        return Task.FromResult(CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { ["extrudeId"] = extrude.CommandId }),
            diagnostics: diagnostics,
            changedBodies: bodies));
    }

    private static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));
}
