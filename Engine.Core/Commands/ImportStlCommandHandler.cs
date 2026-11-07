using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Assets;
using Engine.Core.Import;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0083 items 1 to 6 (phase P9b, TASK-0258): ImportStl reads the bytes of its asset at each apply, open and replay,
// reads the STL file, and stores the triangles in the backend under the handle of the body, with the kind Mesh. It is
// the first handler that reads the store (ADR-0044 and ADR-0045, amended); a host gives the store when it composes the
// engine, and a host with no store refuses the import.
public sealed class ImportStlCommandHandler(IAssetSource? assets = null) : ICommandHandler, ICommandEncoder
{
    // The kind of a mesh body (ADR-0083 item 1).
    public const string MeshKind = "Mesh";

    public string CommandName => "ImportStl";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["asset"] = new("string", Required: true),
            ["name"] = new("string", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (ImportStlCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["asset"] = typed.Asset,
            ["name"] = typed.BodyName,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new ImportStlCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Asset = (string)input.Parameters["asset"]!,
        BodyName = input.Parameters.GetValueOrDefault("name") as string,
    };

    public async Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var import = (ImportStlCommand)command;

        // Item 5: STL has no unit, and each Document is in millimetres (ADR-0015 item 7), so the floats need no conversion.
        if (backend.TryGet<IMeshImportOps>() is not { } mesh)
            return Failure(DiagnosticCodes.GeomCapMissing, "Active backend does not implement IMeshImportOps; cannot import an STL file.");

        // Item 2: the bytes of the asset, at each apply and each replay.
        if (assets is null)
            return Failure(DiagnosticCodes.StlAsset, "This host has no store of the assets, so it cannot read the STL file of the import (ADR-0083 item 2).");
        if (!AssetStore.IsName(import.Asset))
            return Failure(DiagnosticCodes.StlAsset, $"'{import.Asset}' is not the name of an asset: a name is 64 lower-case hexadecimal characters (ADR-0044 item 1).");
        var (content, error) = await assets.Read(import.Asset, ct).ConfigureAwait(false);
        if (content is null)
            return Failure(DiagnosticCodes.StlAsset, $"The STL file '{import.Asset}' does not read from the store: {error?.Code} {error?.Message}");

        var (triangles, fault) = StlReader.Read(content);
        if (triangles is null)
            return CommandHandlerResult.Failure(fault!);

        var handle = new BodyHandle(import.CommandId);
        try
        {
            mesh.ImportMesh(handle, triangles);
        }
        catch (Exception ex)
        {
            return Failure(DiagnosticCodes.GeomNativeOp, $"Geometry backend failed to store the mesh: {ex.Message}");
        }

        var outputs = new Outputs(new Dictionary<string, object?> { ["bodyId"] = handle.Id });
        return CommandHandlerResult.Success(outputs, createdBodies: [new BodyRecord(handle, MeshKind, import.BodyName)]);
    }

    private static CommandHandlerResult Failure(string code, string message) => CommandHandlerResult.Failure(new ErrorDetail(code, message));
}
