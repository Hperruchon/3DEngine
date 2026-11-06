using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0076 items 1 and 2 (phase K5a, TASK-0238): the cylinder is the prism of the circle of ADR-0040 item 2, 64 plane
// facets, from z = -height / 2 to z = +height / 2. The handler gives the loop to IExtrudeOps, so Engine.Contracts does
// not change.
public sealed class CreateCylinderCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateCylinder";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["radius"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["height"] = new("number", Required: true, Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (CreateCylinderCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["radius"] = typed.Radius,
            ["height"] = typed.Height,
        };
    }

    public Command Create(CommandInput input) => new CreateCylinderCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Radius = (double)input.Parameters["radius"]!,
        Height = (double)input.Parameters["height"]!,
    };

    // ADR-0076 item 2: the 64 vertices of the circle about the origin, counter-clockwise from (radius, 0). The kernel
    // projection uses the same loop.
    internal static List<(double X, double Y)> Circle(double radius) => SketchProfile.Turn((0, 0), (radius, 0), null);

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var create = (CreateCylinderCommand)command;

        if (!(double.IsFinite(create.Radius) && create.Radius > 0 && double.IsFinite(create.Height) && create.Height > 0))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomInvalidParam,
                    "CreateCylinder requires a radius and a height that are finite and greater than zero.")));
        }

        var ops = backend.TryGet<IExtrudeOps>();
        if (ops is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement IExtrudeOps; cannot create a cylinder.")));
        }

        // ADR-0012 item 4: the handle is the identifier of the command, and no edit changes a cylinder, so the solid goes
        // under the handle, as for a box.
        var handle = new BodyHandle(create.CommandId);
        var half = create.Height / 2;
        try
        {
            ops.Extrude(handle, new ExtrudeParameters(Circle(create.Radius), create.Height, (0, 0, -half), (1, 0, 0), (0, 1, 0), (0, 0, 1)));
        }
        catch (Exception ex)
        {
            // The sizes and the capability are checked above, so a throw here is a backend fault, surfaced structurally
            // rather than crashing.
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomNativeOp,
                    $"Geometry backend failed to create the cylinder: {ex.Message}")));
        }

        return Task.FromResult(CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { ["bodyId"] = handle.Id }),
            createdBodies: [new BodyRecord(handle, "Cylinder")]));
    }
}
