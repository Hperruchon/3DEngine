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

        if (SketchModel.Find(document, extrude.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(extrude.SketchId));

        // Item 3 (Q2 A): a signed distance that is finite and not zero.
        if (!double.IsFinite(extrude.Distance) || extrude.Distance == 0)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomInvalidParam, "Extrude requires a distance that is finite and not zero."));

        // CreateSketch refuses a datum that does not exist (ADR-0033 item 6), so each
        // live sketch has one of the origin planes.
        var datum = Datums.Find(sketch.DatumId)!;

        // Item 4 (Q3 A): a sketch that is not solved gives no solid.
        if (!SketchSolver.Status(sketch).Solved)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchProfileInvalid, $"The sketch '{sketch.Id}' is not solved, so its profile is not the part that its constraints give."));

        var (loop, reason) = SketchProfile.Find(sketch);
        if (loop is null)
            return Task.FromResult(SketchResults.Failure(DiagnosticCodes.SketchProfileInvalid, reason!));

        var ops = backend.TryGet<IExtrudeOps>();
        if (ops is null)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomCapMissing, "Active backend does not implement IExtrudeOps; cannot extrude a profile."));

        // Item 3: the height is |distance|, and the frame moves by min(0, distance) along
        // n. Each axis of an origin plane holds 0 and ±1 only, so each product is exact.
        var height = extrude.Distance > 0 ? extrude.Distance : -extrude.Distance;
        var shift = extrude.Distance > 0 ? 0.0 : extrude.Distance;
        var (u, v, n) = (datum.U, datum.V, datum.Normal);
        var parameters = new ExtrudeParameters(
            loop,
            height,
            (shift * n.X, shift * n.Y, shift * n.Z),
            (u.X, u.Y, u.Z),
            (v.X, v.Y, v.Z),
            (n.X, n.Y, n.Z));

        // ADR-0012 §4: the handle is the identifier of the command.
        var handle = new BodyHandle(extrude.CommandId);
        try
        {
            ops.Extrude(handle, parameters);
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
            createdBodies: [new BodyRecord(handle, "Extrude")]));
    }
}
