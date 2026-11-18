using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Drawing;
using Engine.Core.Features;

namespace Engine.Core.Queries;

// ADR-0155 item 5 (phase P10.2, TASK-0393). The query reads the Document and the owned kernel, so it needs no capability of
// the backend. ADR-0155 item 7 (Q7 A): no cache; each read computes the lines of each view.
public sealed class GetSheetQueryHandler : IQueryHandler
{
    public string QueryName => "GetSheet";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
            ["name"] = new("string"),
            ["paper"] = new("string"),
            ["landscape"] = new("boolean"),
            ["thirdAngle"] = new("boolean"),
            ["width"] = new("number"),
            ["height"] = new("number"),
            ["views"] = new("array"),
            ["dimensions"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetSheetQuery
    {
        QueryId = input.QueryId,
        SheetId = (Guid)input.Parameters["sheetId"]!,
    };

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var get = (GetSheetQuery)query;
        if (document.Sheets.FirstOrDefault(sheet => sheet.Id == get.SheetId) is not { } sheet)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.SheetNotFound, $"No live sheet with id '{get.SheetId}' exists in the document.")));
        }

        return Task.FromResult(QueryHandlerResult.Success(Read(document, sheet, ct)));
    }

    // ADR-0155 item 5 and ADR-0158 item 4: the read of a live sheet. Since TASK-0396 the SVG export reads the same result
    // (ADR-0159 item 1).
    internal static SheetInfo Read(Document document, SheetRecord sheet, CancellationToken ct)
    {
        var views = new List<SheetViewInfo>(sheet.Views.Count);
        foreach (var view in sheet.Views)
        {
            ct.ThrowIfCancellationRequested();
            var (lines, skipped, texts, centre) = SheetLayout.Place(document, sheet, view);
            views.Add(new SheetViewInfo(view.Id, view.Direction, view.ScaleNumerator, view.ScaleDenominator, view.X, view.Y, lines, skipped, view.HiddenLines, view.CentreLines)
            {
                Section = view.Section,
                Texts = texts,
                CentreU = centre.U,
                CentreV = centre.V,
            });
        }

        // ADR-0158 item 4: the value of each dimension from the design now, in the order of the record. A dimension whose source
        // is not in the design is lost; no diagnostic code comes with it (Q6 A).
        var features = FeatureList.Of(document);
        var dimensions = new List<SheetDimensionInfo>(sheet.Dimensions.Count);
        foreach (var dimension in sheet.Dimensions)
        {
            var value = SheetSources.Value(document, features, dimension.Source, dimension.SourceKind);
            dimensions.Add(new SheetDimensionInfo(
                dimension.Id, dimension.ViewId, dimension.Source, dimension.SourceKind, dimension.X, dimension.Y,
                value is null, value?.Label, value?.Value));
        }

        var (width, height) = SheetLayout.PaperSize(sheet);
        return new SheetInfo(sheet.Id, sheet.Name, sheet.Paper, sheet.Landscape, sheet.ThirdAngle, width, height, views, dimensions);
    }
}
