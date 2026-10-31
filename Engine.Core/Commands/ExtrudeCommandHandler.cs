using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0036 items 1 to 4 and 7: the extrude reads the solved positions of the sketch,
// makes one counter-clockwise loop of its lines, and gives a new body of the kind
// "Extrude". The sketch stays live and does not change.
public sealed class ExtrudeCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "Extrude";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["distance"] = new("number", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (ExtrudeCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["distance"] = typed.Distance,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new ExtrudeCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        Distance = (double)input.Parameters["distance"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var extrude = (ExtrudeCommand)command;

        // Finding E99 of the codebase review of 2026-10-30 (b) (TASK-0123): the body takes the
        // identifier of the command, so a command that the log holds is refused before the backend,
        // as for each other sketch command. The backend refused it only by the key that it held.
        if (SketchResults.InUse(document, command, createsItem: true) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, extrude.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(extrude.SketchId));

        // Item 3 (Q2 A): a signed distance that is finite and not zero.
        if (!double.IsFinite(extrude.Distance) || extrude.Distance == 0)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomInvalidParam, "Extrude requires a distance that is finite and not zero."));

        // Items 2 to 4: a sketch that is not solved, or whose lines are not one closed loop,
        // gives no solid. The rebuild of ADR-0037 uses the same code.
        var (parameters, reason) = ExtrudeProfile.Parameters(sketch, extrude.Distance);
        if (parameters is not { } profile)
            return Task.FromResult(SketchResults.Failure(DiagnosticCodes.SketchProfileInvalid, reason!));

        var ops = backend.TryGet<IExtrudeOps>();
        if (ops is null)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomCapMissing, "Active backend does not implement IExtrudeOps; cannot extrude a profile."));

        // ADR-0012 §4: the handle is the identifier of the command. ADR-0037 item 3: the
        // solid goes under a derived key and never under the handle, so a read of a solid
        // by the body identifier fails loudly.
        var handle = new BodyHandle(extrude.CommandId);
        var key = GeometryKeys.Derive(extrude.CommandId, extrude.CommandId);
        try
        {
            ops.Extrude(new BodyHandle(key), profile);
        }
        catch (Exception ex)
        {
            // The profile and the capability are checked above, so a throw here is a
            // backend fault, surfaced structurally rather than crashing.
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomNativeOp, $"Geometry backend failed to extrude the profile: {ex.Message}"));
        }

        return Task.FromResult(CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { ["bodyId"] = handle.Id }),
            createdBodies: [new BodyRecord(handle, "Extrude", Geometry: key)]));
    }
}
