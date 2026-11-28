using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0191 item 3 (phase P11.0, TASK-0474): the command that places a body, in the form of RenameBodyCommandHandler. The
// order of the checks of Rotate: the operand, the values. The handler changes no solid of the backend, so it needs no
// capability. ADR-0191 item 4: a mesh body of an STL file also takes a placement, because a placement changes no point of
// its mesh.
public sealed class PlaceBodyCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "PlaceBody";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
            ["qw"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["qx"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["qy"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["qz"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["dx"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["dy"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["dz"] = new("number", Required: true, Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (PlaceBodyCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["bodyId"] = typed.BodyId,
            ["qw"] = typed.Qw,
            ["qx"] = typed.Qx,
            ["qy"] = typed.Qy,
            ["qz"] = typed.Qz,
            ["dx"] = typed.Dx,
            ["dy"] = typed.Dy,
            ["dz"] = typed.Dz,
        };
    }

    public Command Create(CommandInput input) => new PlaceBodyCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        BodyId = (Guid)input.Parameters["bodyId"]!,
        Qw = (double)input.Parameters["qw"]!,
        Qx = (double)input.Parameters["qx"]!,
        Qy = (double)input.Parameters["qy"]!,
        Qz = (double)input.Parameters["qz"]!,
        Dx = (double)input.Parameters["dx"]!,
        Dy = (double)input.Parameters["dy"]!,
        Dz = (double)input.Parameters["dz"]!,
    };

    // ADR-0191 item 3: a quaternion that gives no turn, with four finite parts, one of them not zero, and a finite sum of
    // squares, as Rotate tells that case.
    private static bool TooSmall(PlaceBodyCommand place)
    {
        double[] parts = [place.Qw, place.Qx, place.Qy, place.Qz];
        var s = (place.Qw * place.Qw) + (place.Qx * place.Qx) + (place.Qy * place.Qy) + (place.Qz * place.Qz);
        return parts.All(double.IsFinite) && parts.Any(part => part != 0) && double.IsFinite(s);
    }

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var place = (PlaceBodyCommand)command;

        var body = document.Bodies.FirstOrDefault(b => b.Handle.Id == place.BodyId);
        if (body is null)
            return Failure(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{place.BodyId}' exists in the Document.");

        if (Placements.Turn(place.Placement) is null)
            return TooSmall(place)
                ? Failure(
                    DiagnosticCodes.GeomQuaternionTooSmall,
                    "PlaceBody requires qw² + qx² + qy² + qz² of at least 2^-969; smaller parts round the turn into a scale (ADR-0191 item 3).")
                : Failure(
                    DiagnosticCodes.GeomInvalidParam,
                    "PlaceBody requires qw, qx, qy and qz finite and not all zero, with a finite turn (ADR-0191 item 3).");

        if (!(double.IsFinite(place.Dx) && double.IsFinite(place.Dy) && double.IsFinite(place.Dz)))
            return Failure(DiagnosticCodes.GeomInvalidParam, "PlaceBody requires dx, dy and dz finite (ADR-0191 item 3).");

        // ADR-0193 item 6 (phase P11a, TASK-0479, the answer Q4 A): the joint of a child gives its placement, so a PlaceBody of a
        // child is refused, and a PlaceBody of a body with children places each body under it in the same step.
        // ADR-0194 item 2 (phase P11b, TASK-0489): the child of a loose joint takes a PlaceBody.
        if (JointTree.TightParentOf(document.Joints, body.Handle.Id) is { } joint)
            return Failure(
                DiagnosticCodes.JointPlaced,
                $"The body '{body.Handle.Id}' is the child of the joint '{joint.Id}', and the joint gives its placement (ADR-0193 item 6).");

        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["bodyId"] = place.BodyId,
        });

        List<BodyRecord> changed = [body with { Placement = Placements.Stored(place.Placement) }];
        changed.AddRange(JointTree.Below(document, document.Joints, body.Handle.Id, place.Placement));
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedBodies: changed));
    }

    private static Task<CommandHandlerResult> Failure(string code, string message)
        => Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(code, message)));
}
