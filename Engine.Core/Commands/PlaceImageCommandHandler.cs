using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Images;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Assets;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0045 items 1, 2, 4 and 6. The datum is one of the three origin planes, as for a
// sketch. The handler checks the form of the name of the asset and does not read the store:
// a replay must not need the bytes (ADR-0044 item 6). It needs no capability of the backend.
public sealed class PlaceImageCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "PlaceImage";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["asset"] = new("string", Required: true),
            ["datumId"] = new("guid", Required: true),
            ["width"] = new("integer", Required: true),
            ["height"] = new("integer", Required: true),
            ["originU"] = new("number", Required: false),
            ["originV"] = new("number", Required: false),
            ["scale"] = new("number", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["imageId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (PlaceImageCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["asset"] = typed.Asset,
            ["datumId"] = typed.DatumId,
            ["width"] = typed.Width,
            ["height"] = typed.Height,
            ["originU"] = typed.OriginU,
            ["originV"] = typed.OriginV,
            ["scale"] = typed.Scale,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new PlaceImageCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Asset = (string)input.Parameters["asset"]!,
        DatumId = (Guid)input.Parameters["datumId"]!,
        Width = (long)input.Parameters["width"]!,
        Height = (long)input.Parameters["height"]!,
        OriginU = input.Parameters.GetValueOrDefault("originU") as double? ?? 0,
        OriginV = input.Parameters.GetValueOrDefault("originV") as double? ?? 0,
        Scale = input.Parameters.GetValueOrDefault("scale") as double? ?? 1,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var place = (PlaceImageCommand)command;

        if (ImageResults.InUse(document, command, createsImage: true) is { } inUse)
            return Task.FromResult(inUse);

        if (!AssetStore.IsName(place.Asset))
            return Task.FromResult(ImageResults.Failure(
                DiagnosticCodes.ImageAssetInvalid,
                "The asset of an image is the name of its bytes in the store: 64 lower-case hexadecimal digits (ADR-0044 item 1)."));

        if (Datums.Find(place.DatumId) is null)
            return Task.FromResult(ImageResults.Failure(
                DiagnosticCodes.ImageDatumNotFound,
                $"No datum with id '{place.DatumId}' exists. The datums are the origin planes XY, XZ and YZ (ADR-0033 item 1)."));

        if (place.Width is < 1 or > int.MaxValue || place.Height is < 1 or > int.MaxValue)
            return Task.FromResult(ImageResults.Failure(
                DiagnosticCodes.ImageSizeInvalid,
                $"The width and the height of an image are whole numbers of pixels from 1 to {int.MaxValue}, and the command gives {place.Width} and {place.Height}."));

        if (!double.IsFinite(place.OriginU) || !double.IsFinite(place.OriginV))
            return Task.FromResult(ImageResults.Failure(
                DiagnosticCodes.GeomInvalidParam,
                "The origin of an image is two finite numbers in the frame of its datum."));

        if (!double.IsFinite(place.Scale) || place.Scale <= 0)
            return Task.FromResult(ImageResults.ScaleInvalid(place.Scale));

        var image = new ImageRecord(
            command.CommandId, place.Asset, place.DatumId, place.OriginU, place.OriginV,
            (int)place.Width, (int)place.Height, place.Scale);
        var outputs = new Outputs(new Dictionary<string, object?> { ["imageId"] = image.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, createdImages: [image]));
    }
}
