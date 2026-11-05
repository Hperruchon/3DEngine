using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Export;

// ADR-0070 (phase K6a, TASK-0200): the live bodies of a Document as one STEP file of AP214, from the B-Rep of the owned
// kernel (ADR-0058). Each face is a PLANE with its loops, each edge is a LINE between two VERTEX_POINT, and each closed
// shell is a MANIFOLD_SOLID_BREP. A STEP file is presentation, as an STL file is: it never enters a command, an event,
// the Document or the log. The text is the same bytes on each platform: an exact vertex leaves the kernel with one
// rounding to the nearest double, a direction uses + - * / and sqrt only, and each number has the shortest form that
// reads back to the same double, in the invariant culture (CLAUDE.md, determinism rules 1 and 6).
public static class StepExport
{
    // Item 1: the schema of the file, AP214 (ISO 10303-214), and the text of FILE_DESCRIPTION.
    public const string Schema = "AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }";

    public const string Description = "3DEngine STEP export, planar faces, millimetres";

    // Item 2: the file of each live body in the order of its handle, or E-EXPORT-BODY-FAILED for the first body that the
    // owned kernel does not give as closed planar shells.
    public static (byte[]? Content, ErrorDetail? Error) Write(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var solids = new List<(Guid Body, Brep Brep)>();
        foreach (var record in document.Bodies.OrderBy(body => body.Handle.Id))
        {
            var (solid, reason) = KernelProjection.OfRecord(document, record);
            if (solid is null)
                return Refused(record.Handle.Id, reason!);
            Brep brep;
            try
            {
                brep = Brep.Of(solid);
            }
            catch (InvalidOperationException ex)
            {
                return Refused(record.Handle.Id, ex.Message);
            }

            solids.Add((record.Handle.Id, brep));
        }

        return Write(solids);
    }

    // Items 1 to 3: the file of B-Reps of the kernel, each one under the identifier of its body.
    public static (byte[]? Content, ErrorDetail? Error) Write(IReadOnlyList<(Guid Body, Brep Brep)> solids)
    {
        ArgumentNullException.ThrowIfNull(solids);
        foreach (var (body, brep) in solids)
        {
            if (Check(brep) is { } reason)
                return Refused(body, reason);
        }

        var file = new Writer();

        // The context of the product and of the representation: the millimetre, the radian, the steradian, and one global
        // uncertainty of length (the charter, "The kernel boundary": one global length value).
        var application = file.Add("APPLICATION_CONTEXT('automotive design')");
        file.Add($"APPLICATION_PROTOCOL_DEFINITION('international standard','automotive_design',2000,{Ref(application)})");
        var productContext = file.Add($"PRODUCT_CONTEXT('',{Ref(application)},'mechanical')");
        var definitionContext = file.Add($"PRODUCT_DEFINITION_CONTEXT('part definition',{Ref(application)},'design')");
        var product = file.Add($"PRODUCT('document','document','',({Ref(productContext)}))");
        file.Add($"PRODUCT_RELATED_PRODUCT_CATEGORY('part',$,({Ref(product)}))");
        var formation = file.Add($"PRODUCT_DEFINITION_FORMATION('','',{Ref(product)})");
        var definition = file.Add($"PRODUCT_DEFINITION('design','',{Ref(formation)},{Ref(definitionContext)})");
        var shape = file.Add($"PRODUCT_DEFINITION_SHAPE('','',{Ref(definition)})");
        var length = file.Add("(LENGTH_UNIT()NAMED_UNIT(*)SI_UNIT(.MILLI.,.METRE.))");
        var angle = file.Add("(NAMED_UNIT(*)PLANE_ANGLE_UNIT()SI_UNIT($,.RADIAN.))");
        var solidAngle = file.Add("(NAMED_UNIT(*)SI_UNIT($,.STERADIAN.)SOLID_ANGLE_UNIT())");
        var uncertainty = file.Add($"UNCERTAINTY_MEASURE_WITH_UNIT(LENGTH_MEASURE(1.E-07),{Ref(length)},'distance_accuracy_value','confusion accuracy')");
        var context = file.Add($"(GEOMETRIC_REPRESENTATION_CONTEXT(3)GLOBAL_UNCERTAINTY_ASSIGNED_CONTEXT(({Ref(uncertainty)}))GLOBAL_UNIT_ASSIGNED_CONTEXT(({Ref(length)},{Ref(angle)},{Ref(solidAngle)}))REPRESENTATION_CONTEXT('',''))");
        var origin = file.Add("CARTESIAN_POINT('',(0.,0.,0.))");
        var z = file.Add("DIRECTION('',(0.,0.,1.))");
        var x = file.Add("DIRECTION('',(1.,0.,0.))");
        var placement = file.Add($"AXIS2_PLACEMENT_3D('',{Ref(origin)},{Ref(z)},{Ref(x)})");

        var items = new List<int> { placement };
        foreach (var (body, brep) in solids)
            items.AddRange(Body(file, body, brep));

        var representation = file.Add($"ADVANCED_BREP_SHAPE_REPRESENTATION('',({string.Join(',', items.Select(Ref))}),{Ref(context)})");
        file.Add($"SHAPE_DEFINITION_REPRESENTATION({Ref(shape)},{Ref(representation)})");

        var text = new StringBuilder()
            .Append("ISO-10303-21;\n")
            .Append("HEADER;\n")
            .Append("FILE_DESCRIPTION(('").Append(Description).Append("'),'2;1');\n")
            .Append("FILE_NAME('','',(''),(''),'3DEngine','3DEngine','');\n")
            .Append("FILE_SCHEMA(('").Append(Schema).Append("'));\n")
            .Append("ENDSEC;\n")
            .Append("DATA;\n")
            .Append(file.Data)
            .Append("ENDSEC;\n")
            .Append("END-ISO-10303-21;\n")
            .ToString();
        return (Encoding.ASCII.GetBytes(text), null);
    }

    // Item 1: a real of ISO 10303-21: the shortest text that reads back to the same double, with a point in the mantissa
    // and the exponent of .NET. The format of .NET Core 3.0 and later is managed code, so the text does not depend on the
    // platform; the invariant culture keeps the point.
    public static string Real(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "A real of a STEP file is finite.");
        if (value == 0)
            return "0.";
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        // The place of the exponent, by a comparison of each character, so no source holds a letter that a code starts
        // with as a value (finding T91, DiagnosticsRegistryGateTests).
        var e = -1;
        for (var i = 0; i < text.Length && e < 0; i++)
        {
            if (text[i] == 'E')
                e = i;
        }
        var (mantissa, exponent) = e < 0 ? (text, "") : (text[..e], text[e..]);
        return mantissa.Contains('.', StringComparison.Ordinal) ? mantissa + exponent : mantissa + "." + exponent;
    }

    private static (byte[]? Content, ErrorDetail? Error) Refused(Guid body, string reason)
        => (null, new ErrorDetail(DiagnosticCodes.ExportBodyFailed, $"The STEP export does not write the body '{body}': {reason}"));

    // Item 3: a B-Rep that the file can hold: closed, with no void, and with each coordinate finite as a double.
    private static string? Check(Brep brep)
    {
        if (!brep.Valid)
            return "its B-Rep in the owned kernel is not closed.";
        foreach (var shell in brep.Shells)
        {
            var volume = Volume(brep, shell);
            if (volume.Sign < 0)
                return "it has a void inside, and K6a writes no BREP_WITH_VOIDS.";
            if (volume.IsZero)
                return "a shell of its B-Rep has no volume.";
        }

        foreach (var vertex in brep.Vertices)
        {
            var (vx, vy, vz) = vertex.ToDoubles();
            if (!double.IsFinite(vx) || !double.IsFinite(vy) || !double.IsFinite(vz))
                return "a vertex has a coordinate that a double does not hold.";
        }

        return null;
    }

    // The volume of a shell times six, by the divergence theorem: each face gives a point of its plane dot the sum of the
    // cross products of its loops. A hole turns the other way, so its part is negative. An outer shell has a positive
    // volume, because each face turns counter-clockwise seen from outside; a void has a negative one.
    private static Rational Volume(Brep brep, IReadOnlyList<int> shell)
    {
        var sum = Rational.Zero;
        foreach (var f in shell)
        {
            var face = brep.Faces[f];
            var area = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
            foreach (var loop in face.Loops)
            {
                for (var i = 0; i < loop.Vertices.Count; i++)
                    area += brep.Vertices[loop.Vertices[i]].Cross(brep.Vertices[loop.Vertices[(i + 1) % loop.Vertices.Count]]);
            }
            sum += brep.Vertices[face.Loops[0].Vertices[0]].Dot(area);
        }

        return sum;
    }

    // Item 2: the entities of one body, from the leaves up, so each reference names an earlier instance. The result is the
    // list of its solids, one for each shell.
    private static List<int> Body(Writer file, Guid body, Brep brep)
    {
        var points = brep.Vertices.Select(p => file.Add($"CARTESIAN_POINT('',{Triple(p.ToDoubles())})")).ToList();
        var vertices = points.Select(p => file.Add($"VERTEX_POINT('',{Ref(p)})")).ToList();

        var edges = new List<int>();
        var edgeOf = new Dictionary<(int From, int To), int>();
        foreach (var edge in brep.Edges)
        {
            var direction = file.Add($"DIRECTION('',{Triple(Unit(brep.Vertices[edge.To] - brep.Vertices[edge.From]))})");
            var vector = file.Add($"VECTOR('',{Ref(direction)},1.)");
            var line = file.Add($"LINE('',{Ref(points[edge.From])},{Ref(vector)})");
            edgeOf[(edge.From, edge.To)] = edges.Count;
            edges.Add(file.Add($"EDGE_CURVE('',{Ref(vertices[edge.From])},{Ref(vertices[edge.To])},{Ref(line)},.T.)"));
        }

        var faces = new List<int>();
        foreach (var face in brep.Faces)
        {
            var bounds = new List<int>();
            foreach (var loop in face.Loops)
            {
                var uses = new List<int>();
                for (var i = 0; i < loop.Vertices.Count; i++)
                {
                    var (a, b) = (loop.Vertices[i], loop.Vertices[(i + 1) % loop.Vertices.Count]);
                    var edge = edges[edgeOf[a < b ? (a, b) : (b, a)]];
                    uses.Add(file.Add($"ORIENTED_EDGE('',*,*,{Ref(edge)},{(a < b ? ".T." : ".F.")})"));
                }
                var edgeLoop = file.Add($"EDGE_LOOP('',({string.Join(',', uses.Select(Ref))}))");
                bounds.Add(file.Add($"{(loop.Outer ? "FACE_OUTER_BOUND" : "FACE_BOUND")}('',{Ref(edgeLoop)},.T.)"));
            }

            // The plane: its normal points out of the solid, and its first axis is the first side of the outer loop, which
            // lies in the plane exactly.
            var outer = face.Loops[0].Vertices;
            var axis = file.Add($"DIRECTION('',{Triple(Unit(face.Plane.Normal))})");
            var reference = file.Add($"DIRECTION('',{Triple(Unit(brep.Vertices[outer[1]] - brep.Vertices[outer[0]]))})");
            var frame = file.Add($"AXIS2_PLACEMENT_3D('',{Ref(points[outer[0]])},{Ref(axis)},{Ref(reference)})");
            var plane = file.Add($"PLANE('',{Ref(frame)})");
            faces.Add(file.Add($"ADVANCED_FACE('',({string.Join(',', bounds.Select(Ref))}),{Ref(plane)},.T.)"));
        }

        var name = body.ToString("D", CultureInfo.InvariantCulture);
        return [.. brep.Shells.Select(shell =>
        {
            var closed = file.Add($"CLOSED_SHELL('',({string.Join(',', shell.Select(f => Ref(faces[f])))}))");
            return file.Add($"MANIFOLD_SOLID_BREP('{name}',{Ref(closed)})");
        })];
    }

    // The unit vector of an exact vector that is not zero: the exact vector over its largest component, so each part is
    // at most 1 in magnitude before its rounding, then over its length.
    private static (double X, double Y, double Z) Unit(Vector3R v)
    {
        static Rational Abs(Rational r) => r.Sign < 0 ? -r : r;
        var largest = Abs(v.X);
        if (Abs(v.Y) > largest)
            largest = Abs(v.Y);
        if (Abs(v.Z) > largest)
            largest = Abs(v.Z);
        var (x, y, z) = ((v.X / largest).ToDouble(), (v.Y / largest).ToDouble(), (v.Z / largest).ToDouble());
        var length = Math.Sqrt((x * x) + (y * y) + (z * z));
        return (x / length, y / length, z / length);
    }

    private static string Triple((double X, double Y, double Z) value) => $"({Real(value.X)},{Real(value.Y)},{Real(value.Z)})";

    private static string Ref(int id) => "#" + id.ToString(CultureInfo.InvariantCulture);

    // The instances of the section DATA, one on each line, numbered from 1 in the order of their addition.
    private sealed class Writer
    {
        private readonly StringBuilder _data = new();
        private int _next = 1;

        public string Data => _data.ToString();

        public int Add(string entity)
        {
            var id = _next++;
            _data.Append(Ref(id)).Append('=').Append(entity).Append(";\n");
            return id;
        }
    }
}
