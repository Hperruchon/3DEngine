using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Queries;

namespace Engine.Core.Preview;

// ADR-0187 item 1 (phase R25, TASK-0462): the shape of one body after the commands of a proposal, for a host that draws the
// preview: the triangles that the query GetTessellation gives on the fork, in the coordinates of the Document. A tessellation
// is presentation (ADR-0007): no log and no Document holds it, and the answer of GET /proposals/{id}/preview does not give it.
// The backend of a fork lives only during the fork (ADR-0185 item 5), so the fork reads the triangles before it frees its
// backend.
public sealed record PreviewShape(Guid BodyId, Tessellation Triangles)
{
    // Item 2: the most triangles of the shapes of one preview that a host keeps. 100,000 triangles are about 2.4 MB in the
    // arrays of a tessellation (24 bytes for each triangle with one vertex for each two triangles), so the 8 open proposals of
    // the queue keep at most about 20 MB.
    public const int MostTriangles = 100_000;

    // Item 1: the shape of each body with a new solid after the commands, an added body or a changed body whose geometry key or
    // fault changes (ADR-0185 item 9), visible and with no fault, in the order of the body identifiers. A body whose shape
    // passes the bound of most triangles, with the shapes before it, has no shape and counts in LeftOut. A body that
    // GetTessellation refuses has no shape and does not count.
    internal static async Task<(IReadOnlyList<PreviewShape> Shapes, int LeftOut)> Read(
        DocumentSession session,
        PreviewPlan plan,
        IReadOnlyCollection<BodyRecord> bodiesAfter,
        int most,
        CancellationToken ct)
    {
        var records = bodiesAfter.ToDictionary(body => body.Handle.Id);
        var shapes = new List<PreviewShape>();
        var (triangles, leftOut) = (0, 0);
        foreach (var id in plan.Measured)
        {
            if (plan.SameSolid.Contains(id) || !records.TryGetValue(id, out var record) || !record.Visible || record.Fault is not null)
                continue;
            var result = await session.Query<Tessellation>(new GetTessellationQuery { BodyId = id }, ct).ConfigureAwait(false);
            if (result.Error is not null || result.Result is not { TriangleCount: > 0 } tessellation)
                continue;
            if (triangles + tessellation.TriangleCount > most)
            {
                leftOut++;
                continue;
            }

            triangles += tessellation.TriangleCount;
            shapes.Add(new PreviewShape(id, tessellation));
        }

        return (shapes, leftOut);
    }
}
