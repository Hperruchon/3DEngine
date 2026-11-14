using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Hosting;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0142 (phase K9a, TASK-0375): the visible and the hidden lines of a view, computed outside the owned kernel from its
// faces and edges. ADR-0142 names this file in enforced-by. Each test gives the exact lines that it expects, as segments
// on the plane of the view.
public sealed class ViewEdgesTests
{
    private static KernelSolid Box(Rational x, Rational y, Rational z) => KernelSolid.Box(Guid.NewGuid(), x, y, z);

    private static IReadOnlyList<ViewLine> Lines(StandardView view, params KernelSolid[] solids)
        => ViewEdges.Of([.. solids.Select(solid => (Guid.NewGuid(), Brep.Of(solid)))], ViewFrame.Of(view));

    // A line as text with its ends in order, so a set of lines compares with no regard to the direction of each one.
    private static string Text(ViewLine line)
    {
        var (a, b) = ((line.From.X.ToDouble(), line.From.Y.ToDouble()), (line.To.X.ToDouble(), line.To.Y.ToDouble()));
        if (a.CompareTo(b) > 0)
            (a, b) = (b, a);
        return $"{(line.Visible ? "visible" : "hidden")} {a.Item1},{a.Item2} {b.Item1},{b.Item2}";
    }

    private static string[] Texts(IEnumerable<ViewLine> lines) => [.. lines.Select(Text).Order(StringComparer.Ordinal)];

    private static string[] Rectangle(string visibility, int x0, int y0, int x1, int y1)
        => [$"{visibility} {x0},{y0} {x1},{y0}", $"{visibility} {x0},{y1} {x1},{y1}", $"{visibility} {x0},{y0} {x0},{y1}", $"{visibility} {x1},{y0} {x1},{y1}"];

    // Item 2: each standard view is a valid frame, and a frame with a shear, two lengths or the eye behind is not.
    [Fact]
    public void Each_Standard_View_Is_A_Valid_Frame_And_A_Frame_With_A_Shear_Is_Not()
    {
        foreach (var view in Enum.GetValues<StandardView>())
            Assert.True(ViewFrame.Of(view).IsValid, $"{view}");
        Assert.True(new ViewFrame(new(3, 4, 0), new(0, 0, 5), new(4, -3, 0)).IsValid);
        Assert.False(new ViewFrame(new(1, 0, 0), new(0, 0, 2), new(0, -1, 0)).IsValid);
        Assert.False(new ViewFrame(new(1, 0, 0), new(1, 0, 1), new(0, -1, 0)).IsValid);
        Assert.False(new ViewFrame(new(1, 0, 0), new(0, 0, 1), new(0, 1, 0)).IsValid);
        Assert.Throws<ArgumentException>(() => ViewEdges.Of([(Guid.NewGuid(), Brep.Of(Box(1, 1, 1)))], new ViewFrame(new(1, 0, 0), new(0, 0, 2), new(0, -1, 0))));
    }

    // Items 3 and 5: a box from the front and from the top is one rectangle: the edges along the view are points, and the
    // edges of the back face lie under the edges of the front face, so they are not drawn.
    [Fact]
    public void A_Box_Is_One_Rectangle_From_The_Front_And_From_The_Top()
    {
        var box = Box(20, 10, 30);
        Assert.Equal(Rectangle("visible", -10, -15, 10, 15).Order(StringComparer.Ordinal), Texts(Lines(StandardView.Front, box)));
        Assert.Equal(Rectangle("visible", -10, -5, 10, 5).Order(StringComparer.Ordinal), Texts(Lines(StandardView.Top, box)));
        Assert.Equal(Rectangle("visible", -5, -15, 5, 15).Order(StringComparer.Ordinal), Texts(Lines(StandardView.Right, box)));
    }

    // Item 4: the walls of a hole through a plate are hidden from the front, and the hole is visible from the top, with a
    // body under it. A line of the hole on the outline of the plate is the outline, one time.
    [Fact]
    public void The_Walls_Of_A_Hole_Are_Hidden_From_The_Front_And_Visible_From_The_Top()
    {
        var plate = PlanarBoolean.Subtract(Box(20, 10, 2), Box(4, 4, 6))!;
        string[] front = [.. Rectangle("visible", -10, -1, 10, 1), "hidden -2,-1 -2,1", "hidden 2,-1 2,1"];
        Assert.Equal(front.Order(StringComparer.Ordinal), Texts(Lines(StandardView.Front, plate)));
        string[] top = [.. Rectangle("visible", -10, -5, 10, 5), .. Rectangle("visible", -2, -2, 2, 2)];
        Assert.Equal(top.Order(StringComparer.Ordinal), Texts(Lines(StandardView.Top, plate)));

        // Through the hole, a body under the plate shows from the top.
        var under = Box(2, 2, 2).Translate(new Vector3R(0, 0, -10));
        string[] through = [.. top, .. Rectangle("visible", -1, -1, 1, 1)];
        Assert.Equal(through.Order(StringComparer.Ordinal), Texts(Lines(StandardView.Top, plate, under)));
    }

    // Items 4 and 5: a body in front of another one hides the part of its edges behind it, with the cut where the outline
    // of the front body crosses them; each edge of the front body that the cut splits is one line again.
    [Fact]
    public void A_Body_In_Front_Hides_The_Edges_Behind_It_To_Its_Outline()
    {
        var near = Box(10, 2, 10);
        var far = Box(10, 2, 10).Translate(new Vector3R(5, 10, 5));
        string[] expected =
        [
            .. Rectangle("visible", -5, -5, 5, 5),
            "hidden 0,0 0,5", "visible 0,5 0,10",
            "hidden 0,0 5,0", "visible 5,0 10,0",
            "visible 0,10 10,10", "visible 10,0 10,10",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), Texts(Lines(StandardView.Front, near, far)));

        // From the back, the far box is in front, so the near box hides behind it.
        Assert.Contains(Lines(StandardView.Back, near, far), line => !line.Visible);
        Assert.All(Lines(StandardView.Front, near).Concat(Lines(StandardView.Front, far)), line => Assert.True(line.Visible));
    }

    // Item 5: each line keeps the body and the tags of the two faces of its edge.
    [Fact]
    public void Each_Line_Keeps_Its_Body_And_The_Tags_Of_Its_Faces()
    {
        var feature = Guid.NewGuid();
        var body = Guid.NewGuid();
        var lines = ViewEdges.Of([(body, Brep.Of(KernelSolid.Box(feature, 20, 10, 30)))], ViewFrame.Of(StandardView.Front));
        var bottom = Assert.Single(lines, line => line.From.Y == line.To.Y && line.From.Y == -15);
        Assert.Equal(body, bottom.Body);
        Assert.Equal(ViewLineKind.Edge, bottom.Kind);
        Assert.Equal(new[] { "-y", "-z" }, new[] { bottom.First.Element, bottom.Second.Element }.Order(StringComparer.Ordinal));
        Assert.All(new[] { bottom.First, bottom.Second }, tag => Assert.Equal(feature, tag.Feature));
    }

    // Item 3: the facets of a curved side give an outline where a facet that looks toward the eye meets one that does
    // not, and no line where two facets of one side look the same way.
    [Fact]
    public void A_Curved_Side_Gives_An_Outline_And_No_Line_Between_Its_Facets()
    {
        (Rational, Rational)[] octagon = [(10, 0), (7, 7), (0, 10), (-7, 7), (-10, 0), (-7, -7), (0, -10), (7, -7)];
        var cylinder = KernelSolid.Cylinder(Guid.NewGuid(), octagon, 20);
        var lines = Lines(StandardView.Front, cylinder);

        var outlines = lines.Where(line => line.Kind == ViewLineKind.Outline).ToArray();
        Assert.Equal(new[] { "visible -10,-10 -10,10", "visible 10,-10 10,10" }, Texts(outlines));
        Assert.All(lines, line => Assert.True(line.Visible));

        // No vertical line inside the outline: the seams between the facets are not drawn.
        Assert.DoesNotContain(lines, line => line.From.X == line.To.X && line.From.X > -10 && line.From.X < 10);

        // The ends are the front half of each circle, one time each: four pieces on the top and four on the bottom.
        Assert.Equal(8, lines.Count(line => line.Kind == ViewLineKind.Edge));
    }

    // Item 4: an edge exactly behind a seam between two facets of a nearer curved side is hidden: it is on the border of
    // each facet and behind both. An edge of a face itself is at the depth of the face, so it is not hidden.
    [Fact]
    public void An_Edge_Behind_A_Seam_Of_A_Curved_Side_Is_Hidden()
    {
        (Rational, Rational)[] octagon = [(10, 0), (7, 7), (0, 10), (-7, 7), (-10, 0), (-7, -7), (0, -10), (7, -7)];
        var (near, far) = (Guid.NewGuid(), Guid.NewGuid());
        var cylinder = Brep.Of(KernelSolid.Cylinder(Guid.NewGuid(), octagon, 20));
        var box = Brep.Of(Box(10, 2, 10).Translate(new Vector3R(12, 20, 0)));
        var lines = ViewEdges.Of([(near, cylinder), (far, box)], ViewFrame.Of(StandardView.Front));

        string[] expected =
        [
            "hidden 7,-5 7,5",
            "hidden 7,-5 10,-5", "visible 10,-5 17,-5",
            "hidden 7,5 10,5", "visible 10,5 17,5",
            "visible 17,-5 17,5",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), Texts(lines.Where(line => line.Body == far)));
        Assert.All(lines.Where(line => line.Body == near), line => Assert.True(line.Visible));
    }

    // Item 6: the engine gives the lines of the live bodies of the Document, in the order of their handles, as the lines
    // of the solids of the owned kernel; the read changes neither the version nor the log.
    [NativeManifoldFact]
    public async Task The_Engine_Gives_The_Lines_Of_The_Live_Bodies()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var plate = await Body(session, new CreateBoxCommand { SizeX = 20, SizeY = 10, SizeZ = 2 });
        var tool = await Body(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 6 });
        await Body(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool });
        await Body(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });

        var before = await session.Read((document, _) => (document.Version, document.Log.Count));
        var (lines, skipped) = await session.ReadGeometry((document, _) => ViewEdges.Of(document, ViewFrame.Of(StandardView.Front)));

        Assert.Empty(skipped);
        Assert.Equal(2, lines.Select(line => line.Body).Distinct().Count());
        // The small box is in the hole, behind the front face of the plate: its sides are hidden, as the walls of the hole are.
        Assert.Equal(new[] { "hidden -1,-1 -1,1", "hidden -2,-1 -2,1", "hidden 1,-1 1,1", "hidden 2,-1 2,1" }, Texts(lines.Where(line => !line.Visible)));
        Assert.Equal(before, await session.Read((document, _) => (document.Version, document.Log.Count)));
    }

    private static async Task<Guid> Body(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException("No output bodyId.");
    }
}
