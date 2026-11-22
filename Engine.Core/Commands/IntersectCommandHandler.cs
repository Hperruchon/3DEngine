using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0181 items 1 to 3 (phase K16, TASK-0433): the handler of Intersect, in the form of SubtractCommandHandler. Two bodies
// that share no volume give E-GEOM-NATIVE-OP, as a cut that leaves nothing does.
public sealed class IntersectCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "Intersect";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["firstBodyId"] = new("guid", Required: true),
            ["secondBodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // Per ADR-0016. ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (IntersectCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["firstBodyId"] = typed.FirstBodyId,
            ["secondBodyId"] = typed.SecondBodyId,
        };
    }

    public Command Create(CommandInput input) => new IntersectCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        FirstBodyId = (Guid)input.Parameters["firstBodyId"]!,
        SecondBodyId = (Guid)input.Parameters["secondBodyId"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var intersect = (IntersectCommand)command;
        return BooleanStep.Handle(
            document,
            backend,
            new BooleanStep.Operands(intersect.CommandId, intersect.FirstBodyId, intersect.SecondBodyId),
            "intersect",
            (ops, result, first, second) => ops.Intersect(result, first, second),
            intersect);
    }
}
