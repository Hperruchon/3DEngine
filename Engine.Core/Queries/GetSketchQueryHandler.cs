using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Sketch;

namespace Engine.Core.Queries;

// ADR-0033 item 7. The query reads the record in the Document, so it needs no capability of
// the backend. The residual is for the person and for the solver of K3a; a command that
// gives a constraint that the positions do not satisfy is applied.
public sealed class GetSketchQueryHandler : IQueryHandler
{
    public string QueryName => "GetSketch";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid"),
            ["datumId"] = new("guid"),
            ["name"] = new("string"),
            ["points"] = new("array"),
            ["lines"] = new("array"),
            ["circles"] = new("array"),
            ["arcs"] = new("array"),
            ["constraints"] = new("array"),
            ["solved"] = new("boolean"),
            ["freedom"] = new("integer"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetSketchQuery
    {
        QueryId = input.QueryId,
        SketchId = (Guid)input.Parameters["sketchId"]!,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var get = (GetSketchQuery)query;

        if (SketchModel.Find(document, get.SketchId) is not { } sketch)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.SketchNotFound, $"No live sketch with id '{get.SketchId}' exists in the Document.")));
        }

        var constraints = sketch.Constraints
            .Select(constraint => new SketchConstraintInfo(
                constraint.Id, constraint.Kind, constraint.Items, constraint.Values, Finite(SketchConstraints.Residual(sketch, constraint))))
            .ToList();
        var (solved, freedom) = SketchSolver.Status(sketch);
        static double? Finite(double value) => double.IsFinite(value) ? value : null;
        return Task.FromResult(QueryHandlerResult.Success(new SketchInfo(
            sketch.Id, sketch.DatumId, sketch.Name, sketch.Points, sketch.Lines, sketch.Circles, sketch.Arcs, constraints,
            solved, freedom)));
    }
}
