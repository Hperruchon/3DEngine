using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0077 (phase R12b, TASK-0245): a tap near an edge of the selected face selects the edge, with no GPU. ADR-0077 names
// this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class EdgeSelectionTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Vector2 Display = new(1024, 768);

    private static PickHit? Top(double x, double y) => new PickHit(A, 1) { Point = (0, 0, 0.5), Normal = (0, 0, 1) };

    private static async Task<EngineHost> OneBox()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = A, SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        return host;
    }

    private static DatumReference Face(string element) => new(A, element, null);

    // Item 3: with a face selected, a tap that the host finds near an edge selects that edge and keeps the face; Escape goes
    // back from the edge to the face, and then to the body; a tap away from each edge selects the face again, with no edge.
    [Fact]
    public async Task A_Tap_Near_An_Edge_Of_The_Selected_Face_Selects_The_Edge()
    {
        using var host = await OneBox();
        var actions = new BodyActions(host.Session);
        var near = (First: Face("+x"), Second: Face("+z"));
        (DatumReference, DatumReference)? Near(double x, double y) => x > 100 ? near : null;
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        Assert.Null(actions.Edge);
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        Assert.Equal((Face("+z"), ((DatumReference, DatumReference)?)null), (actions.Face, actions.Edge));

        actions.Apply(new SelectIntent(200, 0), Top, [], Near);
        Assert.Equal((Face("+z"), ((DatumReference, DatumReference)?)near), (actions.Face, actions.Edge));
        var entry = actions.SelectedEdge();
        Assert.NotNull(entry);
        Assert.Equal((Face("+x"), Face("+z"), 1.0), (entry.First, entry.Second, entry.Length));
        Assert.Equal(4, actions.EdgesOfFace().Count);

        var escape = new CancelIntent(ByKey: true);
        Assert.True(actions.Apply(escape, Top, []));
        Assert.Equal((Face("+z"), ((DatumReference, DatumReference)?)null), (actions.Face, actions.Edge));
        actions.Apply(new SelectIntent(200, 0), Top, [], Near);
        actions.Apply(new SelectIntent(0, 0), Top, [], Near);
        Assert.Equal((Face("+z"), ((DatumReference, DatumReference)?)null), (actions.Face, actions.Edge));
        Assert.True(actions.Apply(escape, Top, []));
        Assert.Equal((A, (DatumReference?)null), (actions.Selection, actions.Face));
    }

    // Finding V117 of the codebase review of 2026-11-06 (e) (TASK-0252): a tap near an edge on the silhouette, outside the
    // body, selects the edge and keeps the body and the face; until TASK-0252 the pick missed, and the tap cleared the body.
    [Fact]
    public async Task A_Tap_Near_An_Edge_Outside_The_Silhouette_Selects_The_Edge()
    {
        using var host = await OneBox();
        var actions = new BodyActions(host.Session);
        var near = (First: Face("+y"), Second: Face("+z"));
        (DatumReference, DatumReference)? Near(double x, double y) => x > 100 ? near : null;
        PickHit? Miss(double x, double y) => x > 100 ? null : Top(x, y);
        actions.Apply(new SelectIntent(0, 0), Miss, [], Near);
        actions.Apply(new SelectIntent(0, 0), Miss, [], Near);
        Assert.Equal(Face("+z"), actions.Face);

        actions.Apply(new SelectIntent(200, 0), Miss, [], Near);

        Assert.Equal((A, Face("+z"), ((DatumReference, DatumReference)?)near), (actions.Selection, actions.Face, actions.Edge));
    }

    // Item 3: the edge nearest the point on the screen, at most 12 pixels away. A point on the image of an edge gives that
    // edge, and a point far from each edge gives none.
    [Fact]
    public void The_Nearest_Edge_On_The_Screen_Is_At_Most_12_Pixels_Away()
    {
        var camera = new Camera { YawDegrees = 30, PitchDegrees = 20, Distance = 10, Target = (0, 0, 0) };
        FaceSegment Segment((double X, double Y, double Z) a, (double X, double Y, double Z) b)
            => new(new DatumPoint(a.X, a.Y, a.Z), new DatumPoint(b.X, b.Y, b.Z));
        var low = new EdgeEntry(Face("-y"), Face("+z"), 1, 2, [Segment((-1, -1, -1), (1, -1, -1))]);
        var high = new EdgeEntry(Face("+y"), Face("+z"), 1, 2, [Segment((-1, 1, 1), (1, 1, 1))]);
        var (a, b) = FaceOverlay.Of(high.Segments, camera, 800, 600).Single();
        var middle = (a + b) / 2;

        Assert.Same(high, FaceOverlay.Nearest([low, high], camera, 800, 600, middle.X, middle.Y));
        Assert.Same(high, FaceOverlay.Nearest([low, high], camera, 800, 600, middle.X + 3, middle.Y + 8));
        var (c, d) = FaceOverlay.Of(low.Segments, camera, 800, 600).Single();
        var far = ((a + b + c + d) / 4) + new Vector2(400, 0);
        Assert.Null(FaceOverlay.Nearest([low, high], camera, 800, 600, far.X, far.Y));
    }

    // Item 4: the panel gives the line of the selected edge under the line of the face.
    [Fact]
    public void The_Panel_Shows_The_Selected_Edge()
    {
        LiveBody[] bodies = [new(A, "Box", "Plate", true)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var face = new FaceLine("Face +z: 1 square mm", true, [], "Edge +x | +z: 1 mm", []);
        ui.Frame(1f / 60f, Display, Vector2.One, bodies, A, face: face);
        ui.Frame(1f / 60f, Display, Vector2.One, bodies, A, face: face);

        var edge = ui.ItemRectangle(DesktopUi.EdgeLabel);
        Assert.NotNull(edge);
        Assert.True(edge.Value.Y >= ui.ItemRectangle(DesktopUi.FaceLabel)!.Value.W, "The line of the edge is not under the line of the face.");
        ui.Frame(1f / 60f, Display, Vector2.One, bodies, A, face: face with { EdgeText = null });
        Assert.Null(ui.ItemRectangle(DesktopUi.EdgeLabel));
    }
}
