using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Sketch;

namespace Engine.Core.Queries;

// ADR-0053 items 1 to 3. The query reads the log and the records of the Document, so it needs no capability of the
// backend. A body that is not live gives E-GEOM-BODY-NOT-FOUND, and a point on no face that a datum names gives
// E-DATUM-FACE-NOT-FOUND.
public sealed class GetFaceAtQueryHandler : IQueryHandler
{
    public string QueryName => "GetFaceAt";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
            ["x"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["y"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["z"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["nx"] = new("number", Required: true, Unit: UnitKinds.Direction),
            ["ny"] = new("number", Required: true, Unit: UnitKinds.Direction),
            ["nz"] = new("number", Required: true, Unit: UnitKinds.Direction),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["reference"] = new("object"),
            ["datumId"] = new("guid"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetFaceAtQuery
    {
        QueryId = input.QueryId,
        BodyId = (Guid)input.Parameters["bodyId"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
        Z = (double)input.Parameters["z"]!,
        Nx = (double)input.Parameters["nx"]!,
        Ny = (double)input.Parameters["ny"]!,
        Nz = (double)input.Parameters["nz"]!,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var at = (GetFaceAtQuery)query;
        if (document.Bodies.FirstOrDefault(body => body.Handle.Id == at.BodyId) is not { } record)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomBodyNotFound, $"No live body with id '{at.BodyId}' exists in the Document.")));

        // ADR-0083 item 4 (TASK-0258): a mesh body has no named faces.
        if (record.Kind == Commands.ImportStlCommandHandler.MeshKind)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.MeshUnsupported, $"The body '{at.BodyId}' is a mesh body of an STL file, which has no named faces (ADR-0083 item 4).")));

        var (reference, frame, reason) = FaceFinder.Find(document, at.BodyId, (at.X, at.Y, at.Z), (at.Nx, at.Ny, at.Nz));
        if (reference is null)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(DiagnosticCodes.DatumFaceNotFound, reason!)));

        // Item 1: a datum in the order of the log, with the same reference and the frame of the face now.
        var datum = document.Datums.FirstOrDefault(item => item.Reference == reference && item.Fault is null && item.Frame == frame);
        return Task.FromResult(QueryHandlerResult.Success(new FaceInfo(reference, datum?.Id)));
    }
}
