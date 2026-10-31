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

    // The file of each live body in the order of its handle, or the error of a backend with no tessellation.
    public static (byte[]? Content, ErrorDetail? Error) Write(Document document, IGeometryBackend backend)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(backend);
        if (backend.TryGet<ITessellationOps>() is not { } ops)
            return (null, new ErrorDetail(DiagnosticCodes.GeomCapMissing, "The backend does not implement ITessellationOps, so it gives no triangles for an STL file."));

        List<Tessellation> meshes;
        try
        {
            meshes = document.Bodies
                .OrderBy(body => body.Handle.Id)
                .Select(body => ops.Tessellate(new BodyHandle(body.GeometryKey)))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, new ErrorDetail(DiagnosticCodes.GeomNativeOp, $"The backend gave no triangles of a body: {ex.Message}"));
        }
        var triangles = meshes.Sum(mesh => mesh.Indices.Length / 3);

        var content = new byte[84 + (50 * triangles)];
        var header = Encoding.ASCII.GetBytes(Header.PadRight(80));
        header.CopyTo(content, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(80), (uint)triangles);
        var at = 84;
        foreach (var mesh in meshes)
        {
            for (var t = 0; t + 2 < mesh.Indices.Length; t += 3)
            {
                var (a, b, c) = (Vertex(mesh, mesh.Indices[t]), Vertex(mesh, mesh.Indices[t + 1]), Vertex(mesh, mesh.Indices[t + 2]));
                var span = content.AsSpan(at, 50);
                Floats(span, Normal(a, b, c));
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

    private static void Floats(Span<byte> span, (double X, double Y, double Z) value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span, (float)value.X);
        BinaryPrimitives.WriteSingleLittleEndian(span[4..], (float)value.Y);
        BinaryPrimitives.WriteSingleLittleEndian(span[8..], (float)value.Z);
    }
}
