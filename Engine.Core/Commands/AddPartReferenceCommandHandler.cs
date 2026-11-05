using Engine.Contracts;
using Engine.Contracts.Assembly;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Assets;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0071 items 2 and 3. The command applies in an assembly only. The handler checks the form of the name of the part and
// does not read the store, as PlaceImage does: a replay must not need the bytes (ADR-0044 item 6). So the engine does not
// check that the bytes hold Version entries; a later phase that reads the part checks it. It needs no capability of the
// backend.
public sealed class AddPartReferenceCommandHandler : ICommandHandler, ICommandEncoder, IDocumentKinds
{
    public string CommandName => "AddPartReference";
    public int SchemaVersion => 1;

    public IReadOnlySet<string> Kinds => DocumentKinds.AssemblyOnly;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["part"] = new("string", Required: true),
            ["version"] = new("integer", Required: true),
            ["x"] = new("number", Required: false, Unit: UnitKinds.Length),
            ["y"] = new("number", Required: false, Unit: UnitKinds.Length),
            ["z"] = new("number", Required: false, Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["referenceId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddPartReferenceCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["part"] = typed.Part,
            ["version"] = typed.Version,
            ["x"] = typed.X,
            ["y"] = typed.Y,
            ["z"] = typed.Z,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddPartReferenceCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Part = (string)input.Parameters["part"]!,
        Version = (long)input.Parameters["version"]!,
        X = input.Parameters.GetValueOrDefault("x") as double? ?? 0,
        Y = input.Parameters.GetValueOrDefault("y") as double? ?? 0,
        Z = input.Parameters.GetValueOrDefault("z") as double? ?? 0,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddPartReferenceCommand)command;

        // As an image: the reference takes the identifier of its command, so a command that the log holds is refused.
        if (Features.FeatureList.Of(document).Holds(command.CommandId))
            return Failure(DiagnosticCodes.AssemblyIdInUse, $"The log holds a command with the id '{command.CommandId}', and a reference takes the id of its command.");

        if (!AssetStore.IsName(add.Part))
            return Failure(DiagnosticCodes.AssemblyReferenceInvalid,
                "The part of a reference is the name of the bytes of the part file in the store: 64 lower-case hexadecimal digits (ADR-0044 item 1).");

        if (add.Version < 1)
            return Failure(DiagnosticCodes.AssemblyReferenceInvalid,
                $"The version of a reference is the count of the entries of the part, 1 or more, and the command gives {add.Version}.");

        if (!double.IsFinite(add.X) || !double.IsFinite(add.Y) || !double.IsFinite(add.Z))
            return Failure(DiagnosticCodes.AssemblyReferenceInvalid, "The place of a reference is three finite numbers.");

        var reference = new PartReference(command.CommandId, add.Part, add.Version, add.X, add.Y, add.Z);
        var outputs = new Outputs(new Dictionary<string, object?> { ["referenceId"] = reference.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, createdReferences: [reference]));
    }

    private static Task<CommandHandlerResult> Failure(string code, string message)
        => Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(code, message)));
}
