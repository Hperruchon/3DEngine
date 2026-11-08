using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0089 items 1 and 2 (phase K5c, TASK-0288): the cone is a closed mesh that the engine makes from the circle of
// ADR-0040 item 2: 64 vertices of the base, the centre of the base and the apex. The handler gives the mesh to
// ISolidMeshOps. Each vertex comes from + - * / and sqrt only, so the mesh has the same bits on each platform.
public sealed class CreateConeCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateCone";
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
        var typed = (CreateConeCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["radius"] = typed.Radius,
            ["height"] = typed.Height,
        };
    }

    public Command Create(CommandInput input) => new CreateConeCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Radius = (double)input.Parameters["radius"]!,
        Height = (double)input.Parameters["height"]!,
    };

    // ADR-0089 item 2: the mesh of the cone. Vertices 0 to 63 are the base circle at -height / 2, counter-clockwise from
    // (radius, 0); vertex 64 is the apex at +height / 2, and vertex 65 the centre of the base. Each triangle is
    // counter-clockwise when seen from outside: (i, i + 1, apex) on the side, and (centre, i + 1, i) on the base.
    internal static (double[] Positions, int[] Triangles) Mesh(double radius, double height)
    {
        var circle = CreateCylinderCommandHandler.Circle(radius);
        var count = circle.Count;
        var half = height / 2;
        var positions = new double[(count + 2) * 3];
        for (var i = 0; i < count; i++)
            (positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2]) = (circle[i].X, circle[i].Y, -half);
        var (apex, centre) = (count, count + 1);
        (positions[3 * apex], positions[(3 * apex) + 1], positions[(3 * apex) + 2]) = (0, 0, half);
        (positions[3 * centre], positions[(3 * centre) + 1], positions[(3 * centre) + 2]) = (0, 0, -half);

        var triangles = new int[count * 6];
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            (triangles[6 * i], triangles[(6 * i) + 1], triangles[(6 * i) + 2]) = (i, j, apex);
            (triangles[(6 * i) + 3], triangles[(6 * i) + 4], triangles[(6 * i) + 5]) = (centre, j, i);
        }

        return (positions, triangles);
    }

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var create = (CreateConeCommand)command;

        if (!(double.IsFinite(create.Radius) && create.Radius > 0 && double.IsFinite(create.Height) && create.Height > 0))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomInvalidParam,
                    "CreateCone requires a radius and a height that are finite and greater than zero.")));
        }

        var ops = backend.TryGet<ISolidMeshOps>();
        if (ops is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement ISolidMeshOps; cannot create a cone.")));
        }

        // ADR-0012 item 4: the handle is the identifier of the command, and no edit changes a cone, so the solid goes under
        // the handle, as for a box and a cylinder.
        var handle = new BodyHandle(create.CommandId);
        var (positions, triangles) = Mesh(create.Radius, create.Height);
        try
        {
            ops.CreateSolid(handle, positions, triangles);
        }
        catch (Exception ex)
        {
            // The sizes and the capability are checked above, so a throw here is a backend fault, surfaced structurally
            // rather than crashing.
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomNativeOp,
                    $"Geometry backend failed to create the cone: {ex.Message}")));
        }

        return Task.FromResult(CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { ["bodyId"] = handle.Id }),
            createdBodies: [new BodyRecord(handle, "Cone")]));
    }
}
