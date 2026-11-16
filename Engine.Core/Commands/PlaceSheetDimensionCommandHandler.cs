using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Drawing;
using Engine.Core.Features;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0158 items 1, 2 and 5 (phase P10.3, TASK-0395). The handler needs no capability of the backend: it reads the live
// sketches and the extrudes in effect, and it does not test that the view shows the length true (Q4 A).
public sealed class PlaceSheetDimensionCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "PlaceSheetDimension";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: true),
            ["source"] = new("guid", Required: true),
            ["sourceKind"] = new("string", Required: true),
            ["x"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["y"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["dimensionId"] = new("guid", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["dimensionId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (PlaceSheetDimensionCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["viewId"] = typed.ViewId,
            ["source"] = typed.Source,
            ["sourceKind"] = typed.SourceKind,
            ["x"] = typed.X,
            ["y"] = typed.Y,
            ["dimensionId"] = typed.DimensionId,
        };
    }

    public Command Create(CommandInput input) => new PlaceSheetDimensionCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = (Guid)input.Parameters["viewId"]!,
        Source = (Guid)input.Parameters["source"]!,
        SourceKind = (string)input.Parameters["sourceKind"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
        DimensionId = input.Parameters.GetValueOrDefault("dimensionId") as Guid?,
    };

    // ADR-0158 item 1: the checks in the order of the record. The first failure gives its code, and the sheet does not change.
    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
        => Task.FromResult(Place((PlaceSheetDimensionCommand)command, document));

    private static CommandHandlerResult Place(PlaceSheetDimensionCommand place, Document document)
    {
        if (SheetResults.Find(document, place.SheetId) is not { } sheet)
            return SheetResults.NotFound(place.SheetId);

        // ADR-0158 item 2 (Q2 A): a new dimension takes the identifier of its command; a place again keeps its identifier and
        // its index.
        var index = -1;
        if (place.DimensionId is { } again)
        {
            index = sheet.Dimensions.ToList().FindIndex(dimension => dimension.Id == again);
            if (index < 0)
                return SheetResults.DimensionNotFound(sheet.Id, again);
        }
        else if (SheetResults.InUse(document, place, "a dimension") is { } inUse)
        {
            return inUse;
        }

        if (!sheet.Views.Any(view => view.Id == place.ViewId))
            return SheetResults.ViewNotFound(sheet.Id, place.ViewId);
        if (!SheetSources.Kinds.Contains(place.SourceKind))
            return SheetResults.DimensionInvalid($"The source kind of a dimension is {string.Join(" or ", SheetSources.Kinds.Select(kind => $"'{kind}'"))}, and the command gives '{place.SourceKind}' (ADR-0158 item 1).");
        if (SourceRefused(document, place) is { } source)
            return source;
        if (!SheetResults.OnPaper(sheet, place.X, place.Y))
        {
            var (width, height) = SheetLayout.PaperSize(sheet);
            return SheetResults.DimensionInvalid($"The place of a dimension is on the paper, from 0 to {width} mm across and from 0 to {height} mm up, and the command gives ({place.X}, {place.Y}) (ADR-0158 item 1).");
        }

        // ADR-0158 item 1 (Q3 A): a sheet shows each value of the design one time. The dimension that the command places again
        // does not count.
        var id = place.DimensionId ?? place.CommandId;
        if (sheet.Dimensions.Any(dimension => dimension.Source == place.Source && dimension.Id != id))
            return SheetResults.DimensionInvalid($"The sheet '{sheet.Id}' has a dimension of the source '{place.Source}', and a sheet shows each value one time (ADR-0158 item 1).");

        var placed = new SheetDimension(id, place.ViewId, place.Source, place.SourceKind, place.X, place.Y);
        var dimensions = sheet.Dimensions.ToList();
        if (index < 0)
            dimensions.Add(placed);
        else
            dimensions[index] = placed;
        var outputs = new Outputs(new Dictionary<string, object?> { ["dimensionId"] = id });
        return CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Dimensions = dimensions }]);
    }

    // ADR-0158 item 1 (Q1 A, Q5 A): the source is in the design now. A constraint is in a live sketch and has the kind
    // "distance" or "radius"; an extrude is in effect.
    private static CommandHandlerResult? SourceRefused(Document document, PlaceSheetDimensionCommand place)
    {
        if (place.SourceKind == SheetSources.Constraint)
        {
            if (SheetSources.FindConstraint(document, place.Source) is not { } constraint)
                return SheetResults.SourceNotFound($"No live sketch has a constraint with id '{place.Source}' (ADR-0158 item 1).");
            return SheetSources.Label(constraint.Kind) is null
                ? SheetResults.DimensionInvalid($"A dimension shows a constraint 'distance' or 'radius', and the constraint '{place.Source}' is '{constraint.Kind}' (ADR-0158 item 1).")
                : null;
        }

        return FeatureList.Of(document).DistanceInEffect(place.Source) is null
            ? SheetResults.SourceNotFound($"No Extrude in effect has the id '{place.Source}' (ADR-0158 item 1).")
            : null;
    }
}
