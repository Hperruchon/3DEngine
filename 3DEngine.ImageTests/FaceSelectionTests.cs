using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0075 item 3 (phase R12a, TASK-0236): the selected face in the panel and in the scene, with no GPU. ADR-0075 names
// this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class FaceSelectionTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    // ADR-0131 item 1 (phase R18k, TASK-0360): on this portrait display the rows of a selected body are under the part. Until
    // TASK-0360 the panel kept the controls of the selected body on a portrait display.
    private static readonly Vector2 Display = new(768, 1024);

    private static PickHit? Top(double x, double y) => new PickHit(A, 1) { Point = (0, 0, 0.5), Normal = (0, 0, 1) };

    // The native backend, because the test moves a body.
    private static async Task<EngineHost> TwoBoxes()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        foreach (var id in new[] { A, B })
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new CreateBoxCommand { CommandId = id, SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
        return host;
    }

    // The entry of the selected face is the entry of GetFaces with its name; a face of a body that is gone is no longer
    // selected.
    [Fact]
    public async Task The_Selected_Face_Has_Its_Entry_Until_Its_Body_Is_Gone()
    {
        using var host = await TwoBoxes();
        var actions = new BodyActions(host.Session);
        actions.Apply(new SelectIntent(0, 0), Top, []);
        actions.Apply(new SelectIntent(0, 0), Top, []);

        var face = actions.SelectedFace();
        Assert.NotNull(face);
        Assert.Equal((new DatumReference(A, "+z", null), 4, new DatumAxis(0, 0, 1)), (face.Reference, face.Outline.Count, face.Normal));

        // An agent moves the body: the translate consumes A and makes a new body (ADR-0028).
        var moved = await host.Session.Apply(new TranslateCommand { BodyId = A, Dx = 5, Dy = 0, Dz = 0 });
        Assert.True(moved.Status == CommandStatus.Applied, $"{moved.Error?.Code} {moved.Error?.Message}");
        Assert.Null(actions.SelectedFace());
        Assert.Null(actions.Face);
    }

    // The line of the face and its button are in the panel; a curved face has no button; a tap on the button gives a
    // sketch on the selected face. ADR-0131 item 1 (phase R18k, TASK-0360): the line of the face is in the rows, and the
    // button is the segment "Sketch on face" of the ring. Until TASK-0360 both were in the panel on a portrait display.
    [Fact]
    public void The_Panel_Shows_The_Face_And_A_Sketch_On_It()
    {
        LiveBody[] bodies = [new(A, "Box", "Plate", true)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        void Step(FaceLine? face, params InputSample[] frame)
        {
            ui.Route(frame);
            ui.Frame(1f / 60f, Display, Vector2.One, bodies, A, face: face, ringCentre: Display / 2f, ringBody: 120f);
        }

        var curved = new FaceLine("Face side: 314 square mm", false, []);
        Step(curved);
        Step(curved);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.FaceLabel));
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.SketchOnFaceButtonLabel)));

        var flat = new FaceLine("Face +z: 1 square mm", true, [(new Vector2(500, 300), new Vector2(600, 300))]);
        Step(flat);
        Step(flat);
        var button = ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.SketchOnFaceButtonLabel)) ?? throw new InvalidOperationException("The ring has no segment for the sketch on the face.");
        var (x, y) = ((button.X + button.Z) / 2, (button.Y + button.W) / 2);
        Step(flat, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Step(flat, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Step(flat);

        Assert.Contains(new SketchOnSelectedFaceAction(), ui.TakeActions());
        Step(null);
        Assert.Null(ui.ItemRectangle(DesktopUi.FaceLabel));
    }

    // The outline in pixels: an edge in front of the eye gives one line, and the text names the element and the area.
    [Fact]
    public void The_Outline_Is_In_Pixels_And_The_Text_Names_The_Face()
    {
        var camera = new Camera { YawDegrees = 0, PitchDegrees = 0, Distance = 10, Target = (0, 0, 0) };
        var entry = new FaceEntry(new DatumReference(A, "+z", null), 1, 0, 60, new DatumPoint(0, 0, 2), new DatumAxis(0, 0, 1),
            [new FaceSegment(new DatumPoint(-1, 0, 0), new DatumPoint(1, 0, 0))]);

        var lines = FaceOverlay.Of(entry.Outline, camera, Display.X, Display.Y);

        var (a, b) = Assert.Single(lines);
        Assert.True(a.X < b.X && Math.Abs(a.Y - Display.Y / 2) < 1 && Math.Abs(b.Y - Display.Y / 2) < 1, $"{a} {b}");
        Assert.Equal("Face +z: 60 square mm", FaceOverlay.Text(entry, "mm"));
    }
}
