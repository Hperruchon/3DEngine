using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0045 items 5 and 6. The scale is the real distance divided by the distance in pixels,
// so the handler uses only + - * / and sqrt (CLAUDE.md, determinism rule 1). The origin, the
// asset and the datum of the image stay.
public sealed class CalibrateImageCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CalibrateImage";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["imageId"] = new("guid", Required: true),
            ["x1"] = new("number", Required: true),
            ["y1"] = new("number", Required: true),
            ["x2"] = new("number", Required: true),
            ["y2"] = new("number", Required: true),
            ["distance"] = new("number", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["imageId"] = new("guid"),
            ["scale"] = new("number"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (CalibrateImageCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["imageId"] = typed.ImageId,
            ["x1"] = typed.X1,
            ["y1"] = typed.Y1,
            ["x2"] = typed.X2,
            ["y2"] = typed.Y2,
            ["distance"] = typed.Distance,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new CalibrateImageCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        ImageId = (Guid)input.Parameters["imageId"]!,
        X1 = (double)input.Parameters["x1"]!,
        Y1 = (double)input.Parameters["y1"]!,
        X2 = (double)input.Parameters["x2"]!,
        Y2 = (double)input.Parameters["y2"]!,
        Distance = (double)input.Parameters["distance"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var calibrate = (CalibrateImageCommand)command;

        if (ImageResults.InUse(document, command, createsImage: false) is { } inUse)
            return Task.FromResult(inUse);

        if (ImageResults.Find(document, calibrate.ImageId) is not { } image)
            return Task.FromResult(ImageResults.NotFound(calibrate.ImageId));

        var (dx, dy) = (calibrate.X2 - calibrate.X1, calibrate.Y2 - calibrate.Y1);
        var pixels = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(pixels) || pixels <= 0 || !double.IsFinite(calibrate.Distance) || calibrate.Distance <= 0)
            return Task.FromResult(ImageResults.Failure(
                DiagnosticCodes.ImageCalibrationInvalid,
                "A calibration gives two different points of the image with finite coordinates in pixels, and a finite real distance greater than 0 in millimetres."));

        var scale = calibrate.Distance / pixels;
        if (!double.IsFinite(scale) || scale <= 0)
            return Task.FromResult(ImageResults.ScaleInvalid(scale));

        var calibrated = image with { Scale = scale };
        var outputs = new Outputs(new Dictionary<string, object?> { ["imageId"] = image.Id, ["scale"] = scale });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedImages: [calibrated]));
    }
}
