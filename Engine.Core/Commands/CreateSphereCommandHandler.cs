using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0089 items 1 and 5 (phase K5d, TASK-0289): the sphere is a closed mesh that the engine makes from the unit 64-gon of
// ADR-0040 item 2: 31 rings at the latitudes k × 2π/64 for k from -15 to 15, and the two poles. Each vertex is a product
// of the radius and two values of the table, so the mesh has the same bits on each platform.
public sealed class CreateSphereCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateSphere";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["radius"] = new("number", Required: true, Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
        => new Dictionary<string, object?>(StringComparer.Ordinal) { ["radius"] = ((CreateSphereCommand)command).Radius };

    public Command Create(CommandInput input) => new CreateSphereCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Radius = (double)input.Parameters["radius"]!,
    };

    // The count of the rings between the poles.
    internal const int Rings = 31;

    // ADR-0089 item 5: vertex 64 r + j is point j of ring r (latitude (r - 15) × 2π/64); the south pole is 64 × 31 and the
    // north pole 64 × 31 + 1. Each triangle is counter-clockwise when seen from outside.
    internal static (double[] Positions, int[] Triangles) Mesh(double radius)
    {
        var unit = CreateCylinderCommandHandler.Circle(1);
        var count = unit.Count;
        var positions = new double[((count * Rings) + 2) * 3];
        for (var r = 0; r < Rings; r++)
        {
            var k = r - (Rings / 2);
            var (cos, sin) = unit[(k + count) % count];
            for (var j = 0; j < count; j++)
            {
                var at = 3 * ((count * r) + j);
                (positions[at], positions[at + 1], positions[at + 2]) = (radius * cos * unit[j].X, radius * cos * unit[j].Y, radius * sin);
            }
        }

        var (south, north) = (count * Rings, (count * Rings) + 1);
        (positions[3 * south], positions[(3 * south) + 1], positions[(3 * south) + 2]) = (0, 0, -radius);
        (positions[3 * north], positions[(3 * north) + 1], positions[(3 * north) + 2]) = (0, 0, radius);

        var triangles = new List<int>(((Rings - 1) * count * 6) + (count * 6));
        for (var j = 0; j < count; j++)
        {
            var next = (j + 1) % count;
            triangles.AddRange([south, next, j]);
            for (var r = 0; r + 1 < Rings; r++)
            {
                var (a, b, c, d) = ((count * r) + j, (count * r) + next, (count * (r + 1)) + next, (count * (r + 1)) + j);
                triangles.AddRange([a, b, c, a, c, d]);
            }

            var top = count * (Rings - 1);
            triangles.AddRange([top + j, top + next, north]);
        }

        return (positions, [.. triangles]);
    }

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var create = (CreateSphereCommand)command;
        if (!(double.IsFinite(create.Radius) && create.Radius > 0))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.GeomInvalidParam, "CreateSphere requires a radius that is finite and greater than zero.")));
        }

        var (positions, triangles) = Mesh(create.Radius);
        return SolidMesh.Create(create.CommandId, backend, positions, triangles, "Sphere", "a sphere");
    }
}

// ADR-0089 items 1 and 5 (phase K5d, TASK-0289): the torus is a closed mesh: a tube of 32 points, every second point of the
// unit 64-gon of ADR-0040, turned to the 64 positions of the same table around the z axis.
public sealed class CreateTorusCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateTorus";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["radius"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["tubeRadius"] = new("number", Required: true, Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (CreateTorusCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal) { ["radius"] = typed.Radius, ["tubeRadius"] = typed.TubeRadius };
    }

    public Command Create(CommandInput input) => new CreateTorusCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Radius = (double)input.Parameters["radius"]!,
        TubeRadius = (double)input.Parameters["tubeRadius"]!,
    };

    // The count of the points of the tube.
    internal const int Tube = 32;

    // ADR-0089 item 5: vertex 32 i + m is point m of the tube at position i around the axis. Each triangle is
    // counter-clockwise when seen from outside.
    internal static (double[] Positions, int[] Triangles) Mesh(double radius, double tubeRadius)
    {
        var unit = CreateCylinderCommandHandler.Circle(1);
        var count = unit.Count;
        var positions = new double[count * Tube * 3];
        for (var i = 0; i < count; i++)
        {
            for (var m = 0; m < Tube; m++)
            {
                var (cos, sin) = unit[2 * m];
                var distance = radius + (tubeRadius * cos);
                var at = 3 * ((Tube * i) + m);
                (positions[at], positions[at + 1], positions[at + 2]) = (distance * unit[i].X, distance * unit[i].Y, tubeRadius * sin);
            }
        }

        var triangles = new List<int>(count * Tube * 6);
        for (var i = 0; i < count; i++)
        {
            var nextI = (i + 1) % count;
            for (var m = 0; m < Tube; m++)
            {
                var nextM = (m + 1) % Tube;
                var (a, b, c, d) = ((Tube * i) + m, (Tube * nextI) + m, (Tube * nextI) + nextM, (Tube * i) + nextM);
                triangles.AddRange([a, b, c, a, c, d]);
            }
        }

        return (positions, [.. triangles]);
    }

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var create = (CreateTorusCommand)command;
        if (!(double.IsFinite(create.Radius) && double.IsFinite(create.TubeRadius) && create.TubeRadius > 0 && create.Radius > create.TubeRadius))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomInvalidParam,
                    "CreateTorus requires a tube radius that is finite and greater than zero, and a radius that is finite and greater than the tube radius.")));
        }

        var (positions, triangles) = Mesh(create.Radius, create.TubeRadius);
        return SolidMesh.Create(create.CommandId, backend, positions, triangles, "Torus", "a torus");
    }
}

// ADR-0089 item 1: the common end of a handler of a closed mesh: the capability, the store and the result.
internal static class SolidMesh
{
    public static Task<CommandHandlerResult> Create(Guid commandId, IGeometryBackend backend, double[] positions, int[] triangles, string kind, string what)
    {
        var ops = backend.TryGet<ISolidMeshOps>();
        if (ops is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.GeomCapMissing, $"Active backend does not implement ISolidMeshOps; cannot create {what}.")));
        }

        // ADR-0012 item 4: the handle is the identifier of the command, and no edit changes the body.
        var handle = new BodyHandle(commandId);
        try
        {
            ops.CreateSolid(handle, positions, triangles);
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.GeomNativeOp, $"Geometry backend failed to create {what}: {ex.Message}")));
        }

        return Task.FromResult(CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { ["bodyId"] = handle.Id }),
            createdBodies: [new BodyRecord(handle, kind)]));
    }
}
