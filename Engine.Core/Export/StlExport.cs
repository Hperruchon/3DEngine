using System.Buffers.Binary;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;

namespace Engine.Core.Export;

// ADR-0048 (phase P8g, TASK-0157): the live bodies of a Document as one binary STL file. An STL file is
// presentation, as a tessellation is: it never enters a command, an event, the Document or the log
// (CLAUDE.md, determinism rule 6). The coordinates are in the length unit of the Document, the millimetre.
public static class StlExport
{
    // The 80 bytes of the header, padded with spaces.
    public const string Header = "3DEngine STL export, binary, millimetres";

    // Finding E121 of the codebase review of 2026-11-01 (TASK-0160): the file is one array of 84 + 50 bytes for each
    // triangle, so it holds at most this count. Up to TASK-0160 a larger count threw OverflowException.
    public static readonly int MaximumTriangles = (int)((Array.MaxLength - 84L) / 50);

    // The file of each live body in the order of its handle, or the error of a backend with no tessellation.
    public static (byte[]? Content, ErrorDetail? Error) Write(Document document, IGeometryBackend backend)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(backend);
        if (backend.TryGet<ITessellationOps>() is not { } ops)
            return (null, new ErrorDetail(DiagnosticCodes.GeomCapMissing, "The backend does not implement ITessellationOps, so it gives no triangles for an STL file."));

        List<(Guid Body, Tessellation Mesh)> meshes;
        try
        {
            meshes = document.Bodies
                .OrderBy(body => body.Handle.Id)
                .Select(body => (body.Handle.Id, ops.Tessellate(new BodyHandle(body.GeometryKey))))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, new ErrorDetail(DiagnosticCodes.GeomNativeOp, $"The backend gave no triangles of a body: {ex.Message}"));
        }
        var count = meshes.Sum(item => (long)(item.Mesh.Indices.Length / 3));
        if (count > MaximumTriangles)
            return (null, new ErrorDetail(DiagnosticCodes.ExportTooLarge, $"The live bodies have {count} triangles, and one STL file of the export holds at most {MaximumTriangles}."));

        var triangles = (int)count;
        var content = new byte[84 + (50 * triangles)];
        var header = Encoding.ASCII.GetBytes(Header.PadRight(80));
        header.CopyTo(content, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(80), (uint)triangles);
        var at = 84;
        foreach (var (body, mesh) in meshes)
        {
            for (var t = 0; t + 2 < mesh.Indices.Length; t += 3)
            {
                var (a, b, c) = (Vertex(mesh, mesh.Indices[t]), Vertex(mesh, mesh.Indices[t + 1]), Vertex(mesh, mesh.Indices[t + 2]));
                var normal = Normal(a, b, c);

                // E121: a float of the file holds at most about 3.4e38, and ADR-0048 item 1 gives a unit normal. A
                // vertex out of that range, or a normal that is not finite, is refused, and no file is written.
                if (!IsFloat(a) || !IsFloat(b) || !IsFloat(c) || !IsFloat(normal))
                    return (null, new ErrorDetail(DiagnosticCodes.ExportRange, $"A triangle of the body '{body}' has a vertex or a normal that a float of an STL file does not hold; each coordinate must be finite and at most {float.MaxValue} mm in magnitude."));

                var span = content.AsSpan(at, 50);
                Floats(span, normal);
                Floats(span[12..], a);
                Floats(span[24..], b);
                Floats(span[36..], c);
                at += 50;
            }
        }

        return (content, null);
    }

    private static (double X, double Y, double Z) Vertex(Tessellation mesh, uint index)
        => (mesh.Positions[index * 3], mesh.Positions[(index * 3) + 1], mesh.Positions[(index * 3) + 2]);

    // The unit normal of the triangle by the right-hand rule, as the tessellation winds it counter-clockwise
    // from outside (ADR-0018 item 9); zero for a triangle with no area.
    private static (double X, double Y, double Z) Normal((double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) c)
    {
        var (ux, uy, uz) = (b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var (vx, vy, vz) = (c.X - a.X, c.Y - a.Y, c.Z - a.Z);
        var (nx, ny, nz) = ((uy * vz) - (uz * vy), (uz * vx) - (ux * vz), (ux * vy) - (uy * vx));
        var length = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        return length > 0 ? (nx / length, ny / length, nz / length) : (0, 0, 0);
    }

    private static bool IsFloat((double X, double Y, double Z) value)
        => float.IsFinite((float)value.X) && float.IsFinite((float)value.Y) && float.IsFinite((float)value.Z);

    private static void Floats(Span<byte> span, (double X, double Y, double Z) value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span, (float)value.X);
        BinaryPrimitives.WriteSingleLittleEndian(span[4..], (float)value.Y);
        BinaryPrimitives.WriteSingleLittleEndian(span[8..], (float)value.Z);
    }
}
