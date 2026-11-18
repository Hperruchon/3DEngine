using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0167 item 1 (phase R30b, TASK-0407): a new record of the sheet with the new scale of one view, or of each view, and the
// views, their places and the dimensions as before. The handler needs no capability of the backend.
public sealed class ScaleSheetViewCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "ScaleSheetView";
    public int SchemaVersion => 1;

    // As AddSheetView: the two parts of the scale are whole numbers with no unit.
    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: false),
            ["scaleNumerator"] = new("integer", Required: true, Unit: UnitKinds.Count),
            ["scaleDenominator"] = new("integer", Required: true, Unit: UnitKinds.Count),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (ScaleSheetViewCommand)command;
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal) { ["sheetId"] = typed.SheetId };
        if (typed.ViewId is { } view)
            fields["viewId"] = view;
        fields["scaleNumerator"] = typed.ScaleNumerator;
        fields["scaleDenominator"] = typed.ScaleDenominator;
        return fields;
    }

    public Command Create(CommandInput input) => new ScaleSheetViewCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = input.Parameters.TryGetValue("viewId", out var view) ? (Guid?)view : null,
        ScaleNumerator = (long)input.Parameters["scaleNumerator"]!,
        ScaleDenominator = (long)input.Parameters["scaleDenominator"]!,
    };

    // ADR-0167 item 1: the checks in the order of the record: the sheet is live, the view is on it, and each part of the scale
    // is greater than zero. With no view named, a sheet with no view keeps its record.
    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var scale = (ScaleSheetViewCommand)command;
        if (SheetResults.Find(document, scale.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(scale.SheetId));
        if (scale.ViewId is { } id && sheet.Views.All(view => view.Id != id))
            return Task.FromResult(SheetResults.ViewNotFound(sheet.Id, id));
        if (scale.ScaleNumerator <= 0 || scale.ScaleDenominator <= 0)
            return Task.FromResult(SheetResults.ViewInvalid($"Each part of the scale of a view is greater than zero, and the command gives {scale.ScaleNumerator} : {scale.ScaleDenominator} (ADR-0167 item 1)."));

        var views = sheet.Views
            .Select(view => scale.ViewId is null || view.Id == scale.ViewId
                ? view with { ScaleNumerator = scale.ScaleNumerator, ScaleDenominator = scale.ScaleDenominator }
                : view)
            .ToList();
        var outputs = new Outputs(new Dictionary<string, object?> { ["sheetId"] = sheet.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Views = views }]));
    }
}
