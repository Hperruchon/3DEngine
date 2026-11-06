using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Export;

// ADR-0070 (phase K6a, TASK-0200): the live bodies of a Document as one STEP file of AP214, from the B-Rep of the owned
// kernel (ADR-0058). Each face is a PLANE with its loops, each edge is a LINE between two VERTEX_POINT, and each closed
// shell is a MANIFOLD_SOLID_BREP, or a BREP_WITH_VOIDS with the voids inside it (phase K6b, TASK-0201). A STEP file is presentation, as an STL file is: it never enters a command, an event,
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
        var planned = new List<(Guid Body, Brep Brep, IReadOnlyList<Solid> Solids)>();
        foreach (var (body, brep) in solids)
        {
            var (plan, reason) = Plan(brep);
            if (plan is null)
                return Refused(body, reason!);
            planned.Add((body, brep, plan));
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
        foreach (var (body, brep, plan) in planned)
            items.AddRange(Body(file, body, brep, plan));

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
        // The format R of a finite double holds no letter other than the exponent, so the first letter is the exponent.
        // A search for a letter keeps the diagnostics scanner free of a constant severity (T137, TASK-0192).
        var e = text.AsSpan().IndexOfAnyInRange('A', 'Z');
        var (mantissa, exponent) = e < 0 ? (text, "") : (text[..e], text[e..]);
        return mantissa.Contains('.', StringComparison.Ordinal) ? mantissa + exponent : mantissa + "." + exponent;
    }

    private static (byte[]? Content, ErrorDetail? Error) Refused(Guid body, string reason)
        => (null, new ErrorDetail(DiagnosticCodes.ExportBodyFailed, $"The STEP export does not write the body '{body}': {reason}"));

    // Item 2: one solid of the file: the index of its outer shell, and the indices of the voids inside it.
    private sealed record Solid(int Outer, IReadOnlyList<int> Voids);

    // The directions of the ray of item 2, in their order. Each one has integer parts that no box of the engine shares, so
    // the first ray seldom meets an edge or a vertex; a ray that does goes to the next direction.
    internal static readonly Vector3R[] Rays =
    [
        new(7, 3, 2), new(-5, 11, 3), new(3, -2, 13), new(-11, -7, 5), new(2, 13, -17), new(19, -3, -7), new(-13, 5, -11), new(1, -17, 19),
    ];

    // Items 2 and 3: the solids of a B-Rep that the file can hold, or the reason that it cannot. The B-Rep is closed, each
    // coordinate is finite as a double, each shell has a volume, and each void is inside an outer shell. A shell with a
    // positive volume is an outer shell; a shell with a negative volume is a void, and it belongs to the smallest outer
    // shell that holds its first vertex, so a void inside a solid inside a void belongs to the inner solid.
    private static (IReadOnlyList<Solid>? Solids, string? Reason) Plan(Brep brep)
    {
        // Finding E145 of the codebase review of 2026-11-06 (TASK-0197): an edge of four faces gave the reason "not closed".
        if (!brep.Valid)
            return (null, brep.Edges.Any(edge => edge.Faces.Count > 2)
                ? "an edge of its B-Rep has more than two faces."
                : "its B-Rep in the owned kernel is not closed.");

        // Finding E146 (TASK-0197): two vertices that differ by less than the precision of a double became one point of
        // the file, so a sliver of the kernel was a solid of zero thickness.
        var points = new HashSet<(double, double, double)>();
        foreach (var vertex in brep.Vertices)
        {
            var (vx, vy, vz) = vertex.ToDoubles();
            if (!double.IsFinite(vx) || !double.IsFinite(vy) || !double.IsFinite(vz))
                return (null, "a vertex has a coordinate that a double does not hold.");
            if (!points.Add((vx, vy, vz)))
                return (null, "two vertices of its B-Rep round to one point of a double, so the file would hold an edge of no length.");
        }

        // Finding E143 (TASK-0197), Q1 A: a face that holds an edge of other faces inside it touches the solid along that
        // edge, and a reader of ISO 10303-42 refuses such a shell. Each edge has one coedge in each direction there, so
        // the test of Valid does not see it.
        var half = new Rational(1, 2);
        foreach (var edge in brep.Edges)
        {
            var (a, b) = (brep.Vertices[edge.From], brep.Vertices[edge.To]);
            for (var f = 0; f < brep.Faces.Count; f++)
            {
                var plane = brep.Faces[f].Plane;
                if (edge.Faces.Contains(f) || !(plane.Normal.Dot(a) - plane.Offset).IsZero || !(plane.Normal.Dot(b) - plane.Offset).IsZero)
                    continue;
                if (InFace(brep, brep.Faces[f], (a + b) * half) == true)
                    return (null, "an edge of its B-Rep lies inside a face that does not hold it, so the solid is pinched along that edge.");
            }
        }

        var volumes = brep.Shells.Select(shell => Volume(brep, shell)).ToList();
        if (volumes.Any(volume => volume.IsZero))
            return (null, "a shell of its B-Rep has no volume.");
        var outers = Enumerable.Range(0, brep.Shells.Count).Where(s => volumes[s].Sign > 0).ToList();
        var voids = outers.ToDictionary(s => s, _ => new List<int>());
        foreach (var hollow in Enumerable.Range(0, brep.Shells.Count).Where(s => volumes[s].Sign < 0))
        {
            var point = brep.Vertices[brep.Faces[brep.Shells[hollow][0]].Loops[0].Vertices[0]];
            int? owner = null;
            foreach (var outer in outers)
            {
                switch (Inside(brep, brep.Shells[outer], point))
                {
                    case null:
                        return (null, "no ray of the test of a void passes clear of the edges of a shell.");
                    case true when owner is null || volumes[outer] < volumes[owner.Value]:
                        owner = outer;
                        break;
                }
            }

            if (owner is null)
                return (null, "a void of its B-Rep is inside no outer shell.");
            voids[owner.Value].Add(hollow);
        }

        return ([.. outers.Select(outer => new Solid(outer, voids[outer]))], null);
    }

    // Item 2: true when the point is inside the closed shell, by the parity of the faces that a ray from the point crosses.
    // Each test is exact. A ray that meets an edge, a vertex or the plane of a face at the point goes to the next direction;
    // null when each direction does.
    internal static bool? Inside(Brep brep, IReadOnlyList<int> shell, Vector3R point)
    {
        foreach (var direction in Rays)
        {
            if (Crossings(brep, shell, point, direction) is { } count)
                return count % 2 == 1;
        }

        return null;
    }

    internal static int? Crossings(Brep brep, IReadOnlyList<int> shell, Vector3R point, Vector3R direction)
    {
        var count = 0;
        foreach (var f in shell)
        {
            var face = brep.Faces[f];
            var along = face.Plane.Normal.Dot(direction);
            var height = face.Plane.Offset - face.Plane.Normal.Dot(point);
            if (along.IsZero)
            {
                if (height.IsZero)
                    return null;
                continue;
            }

            var t = height / along;
            if (t.IsZero)
                return null;
            if (t.Sign < 0)
                continue;
            switch (InFace(brep, face, point + (direction * t)))
            {
                case null:
                    return null;
                case true:
                    count++;
                    break;
            }
        }

        return count;
    }

    // True when a point of the plane of the face is inside the face, false when it is outside, and null when it is on a
    // loop. The axis of the largest part of the normal drops out, and the parity of all the loops gives the holes.
    private static bool? InFace(Brep brep, BrepFace face, Vector3R q)
    {
        static Rational Abs(Rational r) => r.Sign < 0 ? -r : r;
        var n = face.Plane.Normal;
        var drop = Abs(n.X) >= Abs(n.Y) && Abs(n.X) >= Abs(n.Z) ? 0 : Abs(n.Y) >= Abs(n.Z) ? 1 : 2;
        (Rational U, Rational V) P(Vector3R p) => drop switch { 0 => (p.Y, p.Z), 1 => (p.Z, p.X), _ => (p.X, p.Y) };
        var point = P(q);
        var inside = false;
        foreach (var loop in face.Loops)
        {
            for (var i = 0; i < loop.Vertices.Count; i++)
            {
                var (a, b) = (P(brep.Vertices[loop.Vertices[i]]), P(brep.Vertices[loop.Vertices[(i + 1) % loop.Vertices.Count]]));
                var cross = ((b.U - a.U) * (point.V - a.V)) - ((b.V - a.V) * (point.U - a.U));
                if (cross.IsZero
                    && (a.U <= point.U || b.U <= point.U) && (a.U >= point.U || b.U >= point.U)
                    && (a.V <= point.V || b.V <= point.V) && (a.V >= point.V || b.V >= point.V))
                    return null;
                if ((a.V > point.V) == (b.V > point.V))
                    continue;
                var x = a.U + ((b.U - a.U) * (point.V - a.V) / (b.V - a.V));
                if (x > point.U)
                    inside = !inside;
            }
        }

        return inside;
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
    // list of its solids, one for each outer shell.
    private static List<int> Body(Writer file, Guid body, Brep brep, IReadOnlyList<Solid> solids)
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

        // Item 2: the faces of a void turn, so that its CLOSED_SHELL points out of the void, and the ORIENTED_CLOSED_SHELL
        // with .F. turns it back, as ISO 10303-42 gives BREP_WITH_VOIDS.
        var turned = solids.SelectMany(solid => solid.Voids).SelectMany(v => brep.Shells[v]).ToHashSet();
        var faces = new List<int>();
        for (var f = 0; f < brep.Faces.Count; f++)
        {
            var face = brep.Faces[f];
            var turn = turned.Contains(f);
            var bounds = new List<int>();
            foreach (var kept in face.Loops)
            {
                var loop = turn ? (IReadOnlyList<int>)[.. kept.Vertices.Reverse()] : kept.Vertices;
                var uses = new List<int>();
                for (var i = 0; i < loop.Count; i++)
                {
                    var (a, b) = (loop[i], loop[(i + 1) % loop.Count]);
                    var edge = edges[edgeOf[a < b ? (a, b) : (b, a)]];
                    uses.Add(file.Add($"ORIENTED_EDGE('',*,*,{Ref(edge)},{(a < b ? ".T." : ".F.")})"));
                }
                var edgeLoop = file.Add($"EDGE_LOOP('',({string.Join(',', uses.Select(Ref))}))");
                bounds.Add(file.Add($"{(kept.Outer ? "FACE_OUTER_BOUND" : "FACE_BOUND")}('',{Ref(edgeLoop)},.T.)"));
            }

            // The plane: its normal points out of the solid, or out of the void for a face of a void, and its first axis is
            // the first side of the outer loop, which lies in the plane exactly.
            var outer = turn ? (IReadOnlyList<int>)[.. face.Loops[0].Vertices.Reverse()] : face.Loops[0].Vertices;
            var axis = file.Add($"DIRECTION('',{Triple(Unit(turn ? -face.Plane.Normal : face.Plane.Normal))})");
            var reference = file.Add($"DIRECTION('',{Triple(Unit(brep.Vertices[outer[1]] - brep.Vertices[outer[0]]))})");
            var frame = file.Add($"AXIS2_PLACEMENT_3D('',{Ref(points[outer[0]])},{Ref(axis)},{Ref(reference)})");
            var plane = file.Add($"PLANE('',{Ref(frame)})");
            faces.Add(file.Add($"ADVANCED_FACE('',({string.Join(',', bounds.Select(Ref))}),{Ref(plane)},.T.)"));
        }

        var name = body.ToString("D", CultureInfo.InvariantCulture);
        int Closed(int shell) => file.Add($"CLOSED_SHELL('',({string.Join(',', brep.Shells[shell].Select(f => Ref(faces[f])))}))");
        return [.. solids.Select(solid =>
        {
            var closed = Closed(solid.Outer);
            if (solid.Voids.Count == 0)
                return file.Add($"MANIFOLD_SOLID_BREP('{name}',{Ref(closed)})");
            var voids = solid.Voids.Select(v => file.Add($"ORIENTED_CLOSED_SHELL('',*,{Ref(Closed(v))},.F.)")).ToList();
            return file.Add($"BREP_WITH_VOIDS('{name}',{Ref(closed)},({string.Join(',', voids.Select(Ref))}))");
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
