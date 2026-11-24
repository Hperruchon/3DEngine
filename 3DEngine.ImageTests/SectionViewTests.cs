using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0184 (phase R26, TASK-0447): the section view. "Section" on the ring of the part starts a section on XZ through the middle
// of the visible bodies; "Plane", "Offset" and "Flip" change it; Escape at the level of the document or a second tap on
// "Section" ends it. No tool of the section gives an action, so nothing goes to the engine. The cut of each body is the cut of
// the owned kernel by a plane, with a cap that is a real face. ADR-0184 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class SectionViewTests
{
    private static readonly LiveBody Plate = new(Guid.Parse("84000000-0000-0000-0000-000000000001"), "Box", "Plate", true);
    private static readonly LiveBody Post = new(Guid.Parse("84000000-0000-0000-0000-000000000002"), "Cylinder", "Post", true);
    private static readonly IReadOnlyList<LiveBody> Bodies = [Plate, Post];
    private static readonly DocumentLine File = new("bracket.3de", null);

    // The box of the visible bodies that the host gives: the middle of y is 15, of x is 5 and of z is 4.
    private static readonly ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Box = ((-20, 0, 0), (30, 30, 8));

    private static void Step(DesktopUi ui, Vector2 display, Guid? selection = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, Bodies, selection, File);
    }

    private static PointerSample Pointer(Vector2 at, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    private static Vector2 Mid(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    // A tap on the middle of an item, and a frame after it; the actions of the tap.
    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, string item)
    {
        ui.TakeActions();
        var at = Mid(ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item} at {display}."));
        Step(ui, display, null, Pointer(at, PointerPhase.Down, 0));
        Step(ui, display, null, Pointer(at, PointerPhase.Up, 50));
        Step(ui, display);
        return ui.TakeActions();
    }

    private static KeySample Key(InputKey key) => new(key, true, KeyModifiers.None, 0);

    // The first frames size the structure view, so the ring of the part takes its place from the third frame.
    private static DesktopUi Started(Vector2 display)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre) { SectionBox = Box };
        for (var i = 0; i < 3; i++)
            Step(ui, display);
        return ui;
    }

    // Started, and a tap on "Section".
    private static DesktopUi Sectioned(Vector2 display)
    {
        var ui = Started(display);
        Assert.Empty(Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionLabel)));
        Assert.NotNull(ui.Section);
        return ui;
    }

    private static readonly string[] SectionTools = [DesktopUi.SectionLabel, DesktopUi.SectionPlaneLabel, DesktopUi.SectionOffsetLabel, DesktopUi.SectionFlipLabel];

    // Item 1: the ring of the part has "Section" after "Bodies" while the part has a live body. A tap starts a section on XZ through
    // the middle of the visible bodies, rounded to a whole millimetre, with the side of +y going; the ring of the part then holds
    // "Section", "Plane", "Offset" and "Flip", with "Section" in the colour of a tool in use, the name of the plane and the
    // offset on their segments. The tap gives no action.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void A_Tap_On_Section_Starts_A_Section_On_XZ_Through_The_Middle_Of_The_Bodies(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display);
        Assert.Null(ui.Section);
        Assert.Equal(DesktopUi.SectionLabel, ui.RingLabels[ui.RingLabels.ToList().IndexOf(DesktopUi.BodiesLabel) + 1]);

        Assert.Empty(Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionLabel)));
        var section = ui.Section ?? throw new InvalidOperationException("The tap started no section.");
        Assert.Equal(new SectionState(DesktopUi.DefaultSectionPlane, 15, false), section);
        Assert.Equal("XZ", section.Plane.Name);
        Assert.Equal(new SectionCut((0, 15, 0), (0, 1, 0)), section.Cut);
        Assert.Equal(SectionTools, ui.RingLabels);
        Assert.True(ui.RingInUse(DesktopUi.SectionLabel));
        Assert.False(ui.RingInUse(DesktopUi.SectionFlipLabel));
        foreach (var label in SectionTools)
        {
            var text = ui.ItemRectangle(DesktopUi.RingTextItem(label)) ?? throw new InvalidOperationException($"No label {label}.");
            Assert.True(text.X >= 0f && text.Z <= display.X && text.Y >= 0f && text.W <= display.Y, $"The label {label} at {text} is outside the display {display}.");
            Assert.NotNull(ui.ItemRectangle(DesktopUi.RingIconItem(label)));
        }

        // The plane and the offset show on their segments: "XZ" is narrower than "Plane", and "15 mm" than "Offset".
        Assert.True(Width(ui, DesktopUi.SectionPlaneLabel) < Width(ui, DesktopUi.SectionOffsetLabel));

        // With no live body the ring of the part has no "Section", and a section ends.
        ui.Route([]);
        ui.Frame(1f / 60f, display, Vector2.One, [], null, File);
        Assert.Null(ui.Section);
        Assert.DoesNotContain(DesktopUi.SectionLabel, ui.RingLabels);
    }

    private static float Width(DesktopUi ui, string label) => ui.ItemRectangle(DesktopUi.RingTextItem(label)) is { } box ? box.Z - box.X : float.NaN;

    // Item 3: "Offset" opens the pad of the offset with the offset in the unit of the document. An apply sets the offset and
    // closes the pad with no action; Escape in the pad closes it and keeps the offset.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void The_Pad_Sets_The_Offset(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Sectioned(display);
        Assert.Empty(Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionOffsetLabel)));
        var pad = ui.Pad ?? throw new InvalidOperationException("Offset opened no pad.");
        Assert.Equal(DesktopUi.SectionOffsetLabel, pad.Title);
        Assert.Equal(Document.Millimetre, pad.Unit);
        Assert.Equal([new ThreeDEngine.Core.Numeric.PadField("offset", "15")], pad.Fields);

        Assert.Empty(Tap(ui, display, DesktopUi.PadItem("Clear")));
        Step(ui, display, null, new TextSample("-42.5", 0));
        Step(ui, display, null, Key(InputKey.Enter));
        Assert.Empty(ui.TakeActions());
        Assert.Null(ui.Pad);
        Assert.Equal(-42.5, ui.Section?.Offset);
        Assert.Equal(new SectionCut((0, -42.5, 0), (0, 1, 0)), ui.Section?.Cut);
        Step(ui, display);
        Assert.Equal(SectionTools, ui.RingLabels);

        // Escape in the pad closes the pad only: the section and its offset stay.
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionOffsetLabel));
        Step(ui, display, null, new TextSample("7", 0));
        Step(ui, display, null, Key(InputKey.Escape));
        Assert.Null(ui.Pad);
        Assert.Equal(-42.5, ui.Section?.Offset);
        Assert.Empty(ui.TakeActions());
    }

    // Item 2: "Plane" goes through the origin planes XY, XZ and YZ, then each named datum, and back to XY. An origin plane goes
    // through the middle of the bodies, a datum is at its own place; the flip stays.
    [Fact]
    public void Plane_Goes_Through_The_Origin_Planes_And_The_Named_Datums()
    {
        var display = new Vector2(1280, 720);
        using var ui = Sectioned(display);
        var shelf = new SectionPlane("Shelf top", (0, 0, 40), (0, 0, 1));
        ui.ShowSectionDatums([shelf]);
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionFlipLabel));

        var seen = new List<(string Name, double Offset, bool Flip)>();
        for (var i = 0; i < 4; i++)
        {
            Assert.Empty(Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel)));
            var section = ui.Section!;
            seen.Add((section.Plane.Name, section.Offset, section.Flip));
        }

        Assert.Equal([("YZ", 5d, true), ("Shelf top", 0d, true), ("XY", 4d, true), ("XZ", 15d, true)], seen);
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Assert.Equal(new SectionCut((0, 0, 40), (0, 0, -1)), ui.Section!.Cut);
    }

    // Item 2: the planes of the datums of a Document: each datum with a name and no fault, with its origin and its normal.
    [Fact]
    public void The_Planes_Of_The_Datums_Are_The_Named_Datums_With_No_Fault()
    {
        var frame = new DatumFrame(new DatumPoint(1, 2, 3), new DatumAxis(1, 0, 0), new DatumAxis(0, 0, 1), new DatumAxis(0, -1, 0));
        var reference = new DatumReference(Guid.NewGuid(), "+y", null);
        var front = Guid.NewGuid();
        var planes = SectionDatums.Of(
        [
            new DatumRecord(front, "Front face", reference, frame, null),
            new DatumRecord(Guid.NewGuid(), null, reference, frame, null),
            new DatumRecord(Guid.NewGuid(), "Lost", reference, frame, new DatumFault("E-DATUM-LOST", "The face is gone.")),
        ]);

        // TASK-0449 (finding V232): each plane keeps the identity of its datum.
        Assert.Equal([new SectionPlane("Front face", (1, 2, 3), (0, -1, 0), front)], planes);
    }

    // TASK-0449 (finding V232 of the third codebase review of 2026-11-21; item 2): a section on a datum follows the datum by its
    // identity. A move or a rename of the datum, for example by an undo, moves or renames the plane of the section and keeps the
    // offset and the flip; a datum that is gone gives the plane XZ again. Before TASK-0449 the section kept the copy of the plane
    // of the tap, and the next tap on "Plane" went to XY.
    [Fact]
    public void A_Section_On_A_Datum_Follows_The_Datum_And_Goes_Back_To_XZ_When_It_Is_Gone()
    {
        var display = new Vector2(1280, 720);
        using var ui = Sectioned(display);
        var (id, reference) = (Guid.NewGuid(), new DatumReference(Guid.NewGuid(), "+z", null));
        DatumRecord Shelf(string name, double z)
            => new(id, name, reference, new DatumFrame(new DatumPoint(0, 0, z), new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1)), null);
        ui.ShowSectionDatums(SectionDatums.Of([Shelf("Shelf top", 40)]));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionFlipLabel));
        Assert.Equal(new SectionCut((0, 0, 40), (0, 0, -1)), ui.Section!.Cut);

        ui.ShowSectionDatums(SectionDatums.Of([Shelf("Top", 60)]));
        Assert.Equal(("Top", new SectionCut((0, 0, 60), (0, 0, -1))), (ui.Section!.Plane.Name, ui.Section.Cut));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Assert.Equal("XY", ui.Section!.Plane.Name);
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionPlaneLabel));
        Assert.Equal("Top", ui.Section!.Plane.Name);

        ui.ShowSectionDatums([]);
        Assert.Equal(new SectionState(DesktopUi.DefaultSectionPlane, 15, true), ui.Section);
    }

    // Item 4: "Flip" turns the side that goes, keeps the plane, and has the colour of a tool in use while the side is flipped.
    [Fact]
    public void Flip_Turns_The_Side_That_Goes()
    {
        var display = new Vector2(390, 844);
        using var ui = Sectioned(display);
        Assert.Empty(Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionFlipLabel)));
        Assert.True(ui.Section!.Flip);
        Assert.Equal(new SectionCut((0, 15, 0), (0, -1, 0)), ui.Section.Cut);
        Assert.True(ui.RingInUse(DesktopUi.SectionFlipLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionFlipLabel));
        Assert.False(ui.Section!.Flip);
        Assert.False(ui.RingInUse(DesktopUi.SectionFlipLabel));
    }

    // Item 6: Escape ends the section at the level of the document. With a selected body Escape goes up to the document first, so
    // the section stays; the next Escape ends it. A second tap on "Section" ends it too, and the ring of the part gives its tools
    // back.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(390, 844)]
    public void Escape_At_The_Document_Or_A_Second_Tap_On_Section_Ends_The_Section(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Sectioned(display);
        Step(ui, display, null, Key(InputKey.Escape));
        Assert.Null(ui.Section);

        ui.StartSection();
        Step(ui, display, Plate.Id);
        Step(ui, display, Plate.Id, Key(InputKey.Escape));
        Assert.NotNull(ui.Section);
        Step(ui, display);
        Step(ui, display, null, Key(InputKey.Escape));
        Assert.Null(ui.Section);

        ui.StartSection();
        Step(ui, display);
        Assert.Equal(SectionTools, ui.RingLabels);
        Assert.Empty(Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SectionLabel)));
        Assert.Null(ui.Section);
        Assert.Contains(DesktopUi.NewLabel, ui.RingLabels);
        Assert.Contains(DesktopUi.SectionLabel, ui.RingLabels);
    }

    // Item 3: a box of 40 by 24 by 10 mm cut at its middle on x keeps half of its volume, closed by one cap of 24 by 10 mm on the
    // plane, which looks along the side that goes.
    [Fact]
    public async Task A_Box_Cut_At_Its_Middle_Keeps_Half_Its_Volume_And_One_Cap()
    {
        var (positions, indices) = await Triangles(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });

        var cut = SectionMesh.Cut(positions, indices, (0, 0, 0), (1, 0, 0), solid: true);

        Assert.Equal(SectionMesh.Side.Cut, cut.Side);
        Assert.Equal(20 * 24 * 10, Volume(cut), 6);
        Assert.Equal(24 * 10, Area(cut.CapPositions, cut.CapIndices), 6);
        Assert.Equal(1, Loops(cut));
        Assert.All(Enumerable.Range(0, cut.CapPositions.Length / 3), i => Assert.Equal(0, cut.CapPositions[3 * i]));
        Assert.All(Enumerable.Range(0, cut.Positions.Length / 3), i => Assert.True(cut.Positions[3 * i] <= 0, $"A kept vertex at x = {cut.Positions[3 * i]} is on the side that goes."));
        Assert.All(Normals(cut.CapPositions, cut.CapIndices), normal => Assert.True(normal.X > 0 && normal.Y == 0 && normal.Z == 0, $"A piece of the cap looks along {normal}."));
    }

    // Item 3: a plate of 40 by 40 by 6 mm with a hole of radius 5 mm, cut on z through the plate, gives a cap with a hole: the
    // area of the square less the area of the polygon of the hole, two loops, and no piece of the cap at the centre of the hole.
    [Fact]
    public async Task A_Plate_With_A_Hole_Gives_A_Cap_With_A_Hole()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var plate = await Body(host, new CreateBoxCommand { SizeX = 40, SizeY = 40, SizeZ = 6 });
        var hole = await Body(host, new CreateCylinderCommand { Radius = 5, Height = 20 });
        var subtract = await host.Session.Apply(new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = hole });
        Assert.True(subtract.Outputs.TryGet<Guid>("bodyId", out var cutPlate), $"Subtract: {subtract.Error?.Code} {subtract.Error?.Message}");
        var tessellation = await Tessellation(host, cutPlate);

        var cut = SectionMesh.Cut(tessellation.Positions, tessellation.Indices, (0, 0, 1), (0, 0, 1), solid: true);

        Assert.Equal(SectionMesh.Side.Cut, cut.Side);
        var area = Area(cut.CapPositions, cut.CapIndices);
        Assert.True(area < 1600 - 70 && area > 1600 - (Math.PI * 25), $"The cap has an area of {area} square mm.");
        Assert.Equal(2, Loops(cut));
        Assert.DoesNotContain(Enumerable.Range(0, cut.CapIndices.Length / 3), t => Covers(cut.CapPositions, cut.CapIndices, t, 0, 0));
        Assert.Equal(area * 4, Volume(cut), 6);
    }

    // Items 3 and 4: the five panels of the bookcase of K10a cut on XZ through y = 0 with +y going. Each side and the two shelves
    // keep the part with y at most 0, closed by a cap of their width by their height on y = 0; the back, at y 194 to 200, is on
    // the side that goes. With the side flipped the back is whole.
    [Fact]
    public async Task The_Panels_Of_The_Bookcase_Keep_The_Front_Part_With_A_Cap()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var panels = Bookcase.Parts.Where(part => !part.Name.StartsWith("Module", StringComparison.Ordinal)).ToList();
        Assert.Equal(5, panels.Count);
        foreach (var part in panels)
        {
            var body = await Bookcase.Place(host, part);
            var tessellation = await Tessellation(host, body);
            var cut = SectionMesh.Cut(tessellation.Positions, tessellation.Indices, (0, 0, 0), (0, 1, 0), solid: true);
            var flipped = SectionMesh.Cut(tessellation.Positions, tessellation.Indices, (0, 0, 0), (0, -1, 0), solid: true);
            var (low, high) = (part.Y - (part.SizeY / 2), part.Y + (part.SizeY / 2));
            if (low >= 0)
            {
                Assert.Equal(SectionMesh.Side.Gone, cut.Side);
                Assert.Equal(SectionMesh.Side.Whole, flipped.Side);
                continue;
            }

            Assert.Equal(SectionMesh.Side.Cut, cut.Side);
            Assert.Equal(part.SizeX * -low * part.SizeZ, Volume(cut), 3);
            Assert.Equal(part.SizeX * part.SizeZ, Area(cut.CapPositions, cut.CapIndices), 3);
            Assert.Equal(1, Loops(cut));
            Assert.Equal(part.SizeX * high * part.SizeZ, Volume(flipped), 3);
        }
    }

    // Item 7: a mesh body can be open, so its triangles are clipped to the side that stays and it gets no cap.
    [Fact]
    public async Task A_Mesh_Body_Is_Clipped_With_No_Cap()
    {
        var (positions, indices) = await Triangles(new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });

        var cut = SectionMesh.Cut(positions, indices, (5, 0, 0), (1, 0, 0), solid: false);

        Assert.Equal(SectionMesh.Side.Cut, cut.Side);
        Assert.Empty(cut.CapIndices);
        Assert.All(Enumerable.Range(0, cut.Positions.Length / 3), i => Assert.True(cut.Positions[3 * i] <= 5 + 1e-12));
        // The kept surface: the face -x, and 25 of the 40 mm of the four faces along x.
        Assert.Equal((24 * 10) + (25 * 2 * (24 + 10)), Area(cut.Positions, cut.Indices), 6);
    }

    // Item 5: the hatch of a cap is lines at 45 degrees to the two axes of the plane, the spacing apart, each inside the cap from
    // edge to edge. A square of 10 by 10 mm on XZ with a spacing of 1 gives the lines x - z = k / sqrt(2) ... in its plane.
    [Fact]
    public void The_Hatch_Of_A_Cap_Is_Lines_At_45_Degrees_Inside_The_Cap()
    {
        IReadOnlyList<(double X, double Y, double Z)> square = [(0, 0, 0), (10, 0, 0), (10, 0, 10), (0, 0, 10)];

        var hatch = SectionMesh.Hatch([square], (0, 1, 0), 1);

        Assert.InRange(hatch.Count, 13, 15);
        foreach (var (a, b) in hatch)
        {
            Assert.True(a.Y == 0 && b.Y == 0, "A line of the hatch is off the plane.");
            Assert.Equal(Math.Abs(b.X - a.X), Math.Abs(b.Z - a.Z), 9);
            foreach (var end in new[] { a, b })
                Assert.True(end.X is >= -1e-9 and <= 10 + 1e-9 && end.Z is >= -1e-9 and <= 10 + 1e-9 && (Math.Abs(end.X) < 1e-9 || Math.Abs(end.X - 10) < 1e-9 || Math.Abs(end.Z) < 1e-9 || Math.Abs(end.Z - 10) < 1e-9),
                    $"The end {end} is not on the edge of the square.");
        }

        var parts = hatch.Select(line => (line.A.X - line.A.Z) / Math.Sqrt(2)).Order().ToList();
        foreach (var (first, second) in parts.Zip(parts.Skip(1)))
            Assert.Equal(1, Math.Abs(second - first), 9);
    }

    // Cuts of SectionView that the test holds (TASK-0449, findings V229, V230 and T189 of the third codebase review of
    // 2026-11-21). A held cut runs when the test completes it, on a thread of its own, not on the thread of the frame.
    private sealed class HeldCuts
    {
        private readonly List<Action> _held = [];

        public int Count => _held.Count;

        public Task<T> Hold<T>(Func<T> cut)
        {
            var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _held.Add(() =>
            {
                try
                {
                    done.SetResult(cut());
                }
                catch (Exception exception)
                {
                    done.SetException(exception);
                }
            });
            return done.Task;
        }

        public void Complete(int index)
        {
            var thread = new Thread(() => _held[index]());
            thread.Start();
            thread.Join();
        }
    }

    private static readonly ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) BoxOfTheCuts = ((-20, -12, -5), (20, 12, 5));

    // A body of the kernel as the section cuts it, with the given key of its mesh.
    private static async Task<SectionBody> Sectioned(EngineHost host, Guid body, Guid geometry)
    {
        var tessellation = await Tessellation(host, body);
        return new SectionBody(body, geometry, true, MeshVertex.Centre(tessellation.Positions), tessellation.Positions, tessellation.Indices);
    }

    private static void Free(Dictionary<string, GpuMesh> meshes)
    {
        foreach (var mesh in meshes.Values)
            mesh.Dispose();
    }

    // TASK-0449 (finding V229; ADR-0121 and ADR-0184 item 5): the cut finds the edge lines of its kept part and the hatch of its
    // cap, so the thread of the frame only puts the meshes on the GPU. Before TASK-0449 the search ran in Follow, on the thread
    // of the frame, with no limit of triangles.
    [VulkanImageFact]
    public async Task The_Edge_Lines_And_The_Hatch_Of_A_Cut_Are_Found_In_The_Cut_And_Not_On_The_Thread_Of_The_Frame()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var id = await Body(host, new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });
        var body = await Sectioned(host, id, Guid.NewGuid());
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            var meshes = new Dictionary<string, GpuMesh>();
            var held = new HeldCuts();
            var searches = new List<int>();
            try
            {
                var view = new SectionView { RunCut = held.Hold, Searched = () => { lock (searches) searches.Add(Environment.CurrentManagedThreadId); } };
                var cut = new SectionCut((0, 0, 0), (1, 0, 0));
                var frame = Environment.CurrentManagedThreadId;
                view.Follow(cut, [body], BoxOfTheCuts, meshes, device);
                Assert.Equal(1, held.Count);
                held.Complete(0);
                Assert.True(view.Follow(cut, [body], BoxOfTheCuts, meshes, device));

                Assert.Equal(SectionMesh.Side.Cut, view.ShownOf(id));
                Assert.True(meshes[SectionView.KeptId(id)].EdgeVertexCount > 0, "The kept part has no edge lines.");
                Assert.True(meshes[SectionView.CapId(id)].EdgeVertexCount > 0, "The cap has no hatch.");
                Assert.Equal(2, searches.Count);
                Assert.DoesNotContain(frame, searches);
            }
            finally
            {
                Free(meshes);
            }
        }

        Assert.Equal(messagesBefore, GraphicsDevice.ValidationMessageCount);
    }

    // TASK-0449 (findings V230 and T189; ADR-0184 item 5): a body runs one cut at a time, and keeps its last cut on the GPU until
    // its new cut is complete. Three new planes while a cut runs start no cut; when the cut ends, which is stale, the newest plane
    // starts, and the stale cut never reaches the GPU. Before TASK-0449 each plane started a cut at once, so quick taps on "Flip"
    // ran many cuts of each body together. A new mesh of the body while its cut runs takes the old cut off the GPU, because it is
    // a cut of the old mesh: the body shows whole until the cut of its new mesh. A body that goes while its cut runs keeps no
    // mesh of the section, also when that cut ends.
    [VulkanImageFact]
    public async Task A_Body_Runs_One_Cut_At_A_Time_And_Keeps_Its_Last_Cut_Until_The_New_One()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var id = await Body(host, new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });
        var body = await Sectioned(host, id, Guid.NewGuid());
        var (a, b, c, d, e) = (Plane(0), Plane(5), Plane(-5), Plane(10), Plane(-10));
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            var meshes = new Dictionary<string, GpuMesh>();
            var held = new HeldCuts();
            try
            {
                var view = new SectionView { RunCut = held.Hold };
                bool Follow(SectionCut cut, params SectionBody[] bodies) => view.Follow(cut, bodies, BoxOfTheCuts, meshes, device);
                Follow(a, body);
                held.Complete(0);
                Follow(a, body);
                var first = meshes[SectionView.KeptId(id)];

                Follow(b, body);
                Follow(c, body);
                Follow(d, body);
                Assert.Equal(2, view.Started);
                held.Complete(1);
                Follow(d, body);
                Assert.Same(first, meshes[SectionView.KeptId(id)]);
                Assert.Equal((SectionView.KeptId(id), SectionView.CapId(id)), view.Draws(id));
                Assert.Equal(3, view.Started);
                held.Complete(2);
                Assert.True(Follow(d, body));
                Assert.NotSame(first, meshes[SectionView.KeptId(id)]);
                Assert.Equal(SectionMesh.Side.Cut, view.ShownOf(id));

                // A new mesh of the body while its cut runs.
                Follow(e, body);
                var wider = await Sectioned(host, await Body(host, new CreateBoxCommand { SizeX = 60, SizeY = 24, SizeZ = 10 }), Guid.NewGuid()) with { Id = id };
                Assert.True(Follow(e, wider));
                Assert.Null(view.Draws(id));
                Assert.False(meshes.ContainsKey(SectionView.KeptId(id)));
                Assert.Equal(4, view.Started);
                held.Complete(3);
                Follow(e, wider);
                Assert.Null(view.Draws(id));
                Assert.Equal(5, view.Started);
                held.Complete(4);
                Assert.True(Follow(e, wider));
                Assert.Equal((SectionView.KeptId(id), SectionView.CapId(id)), view.Draws(id));

                // A body that goes while its cut runs.
                Follow(a, wider);
                Assert.Equal(6, view.Started);
                Assert.True(Follow(a));
                held.Complete(5);
                Follow(a);
                Assert.Empty(meshes);
                Assert.Null(view.ShownOf(id));
                Assert.Equal(6, view.Started);

                // TASK-0461 (finding V233 of the codebase review of 2026-11-23): a body that goes and comes back while its cut runs,
                // as "Section" off and on or Hide and Show do, starts no second cut. It waits for that cut, which is of its mesh and
                // its plane, so the cut goes on the GPU and no new cut starts. Until TASK-0461 the body started a second cut at once.
                Follow(a, wider);
                Assert.Equal(7, view.Started);
                Follow(a);
                Follow(a, wider);
                Assert.Equal(7, view.Started);
                held.Complete(6);
                Assert.True(Follow(a, wider));
                Assert.Equal((SectionView.KeptId(id), SectionView.CapId(id)), view.Draws(id));
                Assert.Equal(7, view.Started);

                // With another plane when it comes back, the body starts the cut of that plane when the cut that runs ends.
                Follow(b, wider);
                Assert.Equal(8, view.Started);
                view.Follow(null, [wider], BoxOfTheCuts, meshes, device);
                Follow(c, wider);
                Assert.Equal(8, view.Started);
                held.Complete(7);
                Follow(c, wider);
                Assert.Equal(9, view.Started);
                held.Complete(8);
                Assert.True(Follow(c, wider));
                Assert.Equal(SectionMesh.Side.Cut, view.ShownOf(id));
            }
            finally
            {
                Free(meshes);
            }
        }

        Assert.Equal(messagesBefore, GraphicsDevice.ValidationMessageCount);
        static SectionCut Plane(double x) => new((x, 0, 0), (1, 0, 0));
    }

    // TASK-0449 (finding V231; ADR-0184 item 5): the section cuts the visible bodies only. A hidden body is not drawn, so its cut
    // would only cost the time of the cut and the memory of the GPU.
    [VulkanImageFact]
    public async Task The_Section_Cuts_The_Visible_Bodies_Only()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var shown = await Body(host, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 });
        var hidden = await Body(host, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 20 });
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new SetBodyVisibilityCommand { BodyId = hidden, Visible = false })).Status);
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            var meshes = new Dictionary<string, GpuMesh>();
            try
            {
                using var bodies = new SessionBodies(new HostedSession(host));
                bodies.Apply(SessionScene.Create(), meshes, device);
                Assert.Equal(2, bodies.Live.Count);

                Assert.Equal([shown], bodies.SectionBodies.Select(body => body.Id));
            }
            finally
            {
                Free(meshes);
            }
        }

        Assert.Equal(messagesBefore, GraphicsDevice.ValidationMessageCount);
    }

    // The table of the bookcase of Engine.Tests/Assembly/BookcaseCase.cs (K10a), which this project cannot reference.
    // The_Bookcase_Is_The_Table_Of_K10a compares the two tables.
    internal static class Bookcase
    {
        internal sealed record Part(string Name, double SizeX, double SizeY, double SizeZ, double X, double Y, double Z);

        internal static readonly IReadOnlyList<Part> Parts =
        [
            new("Module 1 - volume indicatif", 800, 400, 1000, -1300, 0, 500),
            new("Module 2 - volume indicatif", 800, 400, 1000, 1300, 0, 500),
            new("Module 3 - volume indicatif", 400, 400, 950, 100, 0, 1875),
            new("Module 5 - volume indicatif", 400, 400, 550, 1300, 0, 2075),
            new("Module 6 - volume indicatif", 600, 400, 600, 200, 0, 300),
            new("Module 7 - volume indicatif", 800, 400, 400, -1300, 0, 1200),
            new("Module 8 - volume indicatif", 600, 400, 400, -600, 0, 800),
            new("Module 9 - volume indicatif", 800, 400, 600, -500, 0, 300),
            new("Module 10 - volume indicatif", 400, 400, 600, 700, 0, 300),
            new("Module 11 - volume indicatif", 400, 400, 400, 1300, 0, 1600),
            new("Module 12 - volume indicatif", 600, 400, 400, -600, 0, 1200),
            new("Module 14 - volume indicatif", 800, 400, 800, 700, 0, 1800),
            new("Module 15 - volume indicatif", 800, 400, 950, -1300, 0, 1875),
            new("Module 16 - volume indicatif", 800, 400, 950, -500, 0, 1875),
            new("Cote gauche", 18, 400, 800, -291, 0, 1000),
            new("Cote droit", 18, 400, 800, 891, 0, 1000),
            new("Bas", 1164, 394, 18, 300, -3, 609),
            new("Haut", 1164, 394, 18, 300, -3, 1391),
            new("Fond", 1164, 6, 800, 300, 197, 1000),
        ];

        // CreateBox, Translate and RenameBody of one part, as BookcaseCase.Build sends them; the handle of the body.
        internal static async Task<Guid> Place(EngineHost host, Part part)
        {
            var box = await Body(host, new CreateBoxCommand { SizeX = part.SizeX, SizeY = part.SizeY, SizeZ = part.SizeZ });
            var placed = await Body(host, new TranslateCommand { BodyId = box, Dx = part.X, Dy = part.Y, Dz = part.Z });
            Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new RenameBodyCommand { BodyId = placed, NewName = part.Name })).Status);
            return placed;
        }
    }

    // The table above has each line of the table of K10a, in its order.
    [Fact]
    public void The_Bookcase_Is_The_Table_Of_K10a()
    {
        var source = System.IO.File.ReadAllText(Path.Combine(Root(), "Engine.Tests", "Assembly", "BookcaseCase.cs"));
        var rows = System.Text.RegularExpressions.Regex.Matches(source, "new\\(\"[^\"]+\", [^)]*\\)").Select(match => match.Value).ToList();
        Assert.Equal(Bookcase.Parts.Select(part => FormattableString.Invariant($"new(\"{part.Name}\", {part.SizeX}, {part.SizeY}, {part.SizeZ}, {part.X}, {part.Y}, {part.Z})")), rows);
    }

    private static string Root()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (System.IO.File.Exists(Path.Combine(folder.FullName, "3DEngine.sln")))
                return folder.FullName;
        }

        throw new InvalidOperationException("No folder above the test holds 3DEngine.sln.");
    }

    // ADR-0184 items 1 to 5: the pictures of the bookcase with a section on XZ through its middle, at 1280 by 720 and on a portrait
    // phone, with the window in the state of the section. The cut keeps the front of each body and drops the back; the caps show
    // their hatch in the accent colour; the validation layer gives no message.
    [VulkanImageFact]
    public async Task The_Pictures_Show_The_Bookcase_With_A_Section_Through_Its_Middle()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var bodies = new Dictionary<string, Guid>();
        foreach (var part in Bookcase.Parts)
            bodies[part.Name] = await Bookcase.Place(host, part);

        foreach (var (width, height) in new[] { (1280u, 720u), (390u, 844u) })
        {
            using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
            var scene = SessionScene.Create();
            var camera = scene.Cameras[0];
            var display = new Vector2(width, height);
            byte[] whole, cut, picture;
            using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
            {
                using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
                using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
                using var panelRenderer = new UiRenderer(device, target.RenderPass);
                var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
                ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
                renderer.Update(scene, camera, (float)width / height);
                whole = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));

                // The window starts the section from the ring of the part, as a person does.
                ui.SectionBox = renderer.Union(hiddenToo: false);
                // The bookcase is 400 mm deep with its middle at y = 0 (each module from -200 to 200), so the middle offset of XZ
                // is 0, from the box and not from a missing box.
                Assert.Equal((-200d, 200d), (ui.SectionBox?.Min.Y, ui.SectionBox?.Max.Y));
                var data = UiDrawData.Empty;
                void Frame(params InputSample[] samples)
                {
                    ui.Route(samples);
                    data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, null, File, ringCentre: new Vector2(width / 2f, height * 0.5f), ringBody: 120f);
                }

                for (var i = 0; i < 3; i++)
                    Frame();
                var at = Mid(ui.ItemRectangle(DesktopUi.RingTextItem(DesktopUi.SectionLabel)) ?? throw new InvalidOperationException("No Section."));
                Frame(Pointer(at, PointerPhase.Down, 0));
                Frame(Pointer(at, PointerPhase.Up, 50));
                // The pointer leaves the ring, so no segment of the picture has the fill of a hover.
                Frame(Pointer(new Vector2(8f, height / 2f), PointerPhase.Move, 100));
                Frame();
                Assert.Empty(ui.TakeActions());
                var section = ui.Section ?? throw new InvalidOperationException("The tap started no section.");
                Assert.Equal(("XZ", 0d), (section.Plane.Name, section.Offset));

                renderer.Section = section.Cut;
                renderer.Update(scene, camera, (float)width / height);
                await renderer.SectionMeshes.Cuts;
                renderer.Update(scene, camera, (float)width / height);
                Assert.Equal(SectionMesh.Side.Gone, renderer.SectionMeshes.ShownOf(bodies["Fond"]));
                Assert.All(bodies.Where(body => body.Key != "Fond"), body => Assert.Equal(SectionMesh.Side.Cut, renderer.SectionMeshes.ShownOf(body.Value)));
                Assert.Equal(19, renderer.SectionMeshes.Started);
                // The back draws nothing; a cut body draws its kept part and its cap in the place of its mesh.
                Assert.Equal((null, null), renderer.SectionMeshes.Draws(bodies["Fond"]));
                Assert.Equal((SectionView.KeptId(bodies["Bas"]), SectionView.CapId(bodies["Bas"])), renderer.SectionMeshes.Draws(bodies["Bas"]));

                cut = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));
                picture = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0)));

                // A frame with the same plane starts no new cut.
                renderer.Update(scene, camera, (float)width / height);
                Assert.Equal(19, renderer.SectionMeshes.Started);
            }

            Save(whole, width, height, $"section-bookcase-whole-{width}x{height}.png");
            Save(cut, width, height, $"section-bookcase-scene-{width}x{height}.png");
            Save(picture, width, height, $"section-bookcase-{width}x{height}.png");
            Assert.Equal(0, Hatched(whole));
            Assert.True(Hatched(cut) > 200, $"The scene with the section has {Hatched(cut)} pixels of the hatch at {width} by {height}.");
            Assert.False(whole.AsSpan().SequenceEqual(cut), "The section did not change the picture.");
        }

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }

    // The pixels in the accent colour of the hatch: a body is at most (140, 166, 204) and an edge line is near white.
    private static int Hatched(byte[] pixels)
    {
        var count = 0;
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            if (pixels[i] < 120 && pixels[i + 1] > 190 && pixels[i + 2] > 220)
                count++;
        }

        return count;
    }

    private static void Save(byte[] pixels, uint width, uint height, string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        Png.Write(Path.Combine(folder, name), (int)width, (int)height, pixels);
    }

    private static async Task<(double[] Positions, uint[] Indices)> Triangles(Command command)
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var tessellation = await Tessellation(host, await Body(host, command));
        return (tessellation.Positions, tessellation.Indices);
    }

    private static async Task<Guid> Body(EngineHost host, Command command)
    {
        var result = await host.Session.Apply(command);
        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.True(result.Outputs.TryGet<Guid>("bodyId", out var bodyId), $"{command.Name} gave no body.");
        return bodyId;
    }

    private static async Task<Tessellation> Tessellation(EngineHost host, Guid body)
        => (await host.Session.Query<Tessellation>(new GetTessellationQuery { BodyId = body })).Result
           ?? throw new InvalidOperationException("The body has no tessellation.");

    // The volume of the kept part and its cap, a closed surface: the sum of a · (b × c) / 6 over the triangles.
    private static double Volume(SectionMesh.Result cut)
    {
        double Sum(double[] p, uint[] t)
        {
            var sum = 0d;
            for (var i = 0; i + 2 < t.Length; i += 3)
            {
                var (a, b, c) = (At(p, t[i]), At(p, t[i + 1]), At(p, t[i + 2]));
                sum += Vector3D.Dot(a, Vector3D.Cross(b, c)) / 6;
            }

            return sum;
        }

        return Sum(cut.Positions, cut.Indices) + Sum(cut.CapPositions, cut.CapIndices);
    }

    private static double Area(double[] p, uint[] t)
    {
        var sum = 0d;
        for (var i = 0; i + 2 < t.Length; i += 3)
            sum += Vector3D.Length(Vector3D.Cross(Vector3D.Minus(At(p, t[i + 1]), At(p, t[i])), Vector3D.Minus(At(p, t[i + 2]), At(p, t[i])))) / 2;
        return sum;
    }

    private static IEnumerable<(double X, double Y, double Z)> Normals(double[] p, uint[] t)
    {
        for (var i = 0; i + 2 < t.Length; i += 3)
            yield return Vector3D.Cross(Vector3D.Minus(At(p, t[i + 1]), At(p, t[i])), Vector3D.Minus(At(p, t[i + 2]), At(p, t[i])));
    }

    // The count of the closed loops of the open border of the kept part of a cut, which the cap closes: the edges of one triangle
    // only, joined end to end. Two vertices at one position are one vertex. The cut splits each edge of the tessellation at one
    // exact point, so the border has no T-vertex; the cap is a partition of convex pieces, which can have T-vertices, so the
    // loops come from the border of the kept part.
    private static int Loops(SectionMesh.Result cut)
    {
        var (p, t) = (cut.Positions, cut.Indices);
        var edges = new Dictionary<((double, double, double) A, (double, double, double) B), int>();
        for (var i = 0; i + 2 < t.Length; i += 3)
        {
            foreach (var (a, b) in new[] { (t[i], t[i + 1]), (t[i + 1], t[i + 2]), (t[i + 2], t[i]) })
            {
                var (from, to) = (At(p, a), At(p, b));
                if (edges.TryGetValue((to, from), out var opposite))
                {
                    if (opposite == 1)
                        edges.Remove((to, from));
                    else
                        edges[(to, from)] = opposite - 1;
                }
                else
                {
                    edges[(from, to)] = edges.GetValueOrDefault((from, to)) + 1;
                }
            }
        }

        var unused = edges.Keys.ToList();
        var loops = 0;
        while (unused.Count > 0)
        {
            var start = unused[0];
            var edge = start;
            unused.RemoveAt(0);
            while (edge.B != start.A)
            {
                var at = unused.FindIndex(next => next.A == edge.B);
                if (at < 0)
                    throw new InvalidOperationException($"The border is open at {edge.B}.");
                edge = unused[at];
                unused.RemoveAt(at);
            }

            loops++;
        }

        return loops;
    }

    // True when the triangle of index t holds the point (x, y) in its projection on z.
    private static bool Covers(double[] p, uint[] t, int triangle, double x, double y)
    {
        var (a, b, c) = (At(p, t[3 * triangle]), At(p, t[(3 * triangle) + 1]), At(p, t[(3 * triangle) + 2]));
        double Side((double X, double Y, double Z) u, (double X, double Y, double Z) v) => ((v.X - u.X) * (y - u.Y)) - ((v.Y - u.Y) * (x - u.X));
        var (s1, s2, s3) = (Side(a, b), Side(b, c), Side(c, a));
        return (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
    }

    private static (double X, double Y, double Z) At(double[] p, uint index) => (p[3 * index], p[(3 * index) + 1], p[(3 * index) + 2]);

    private static class Vector3D
    {
        public static (double X, double Y, double Z) Minus((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
            => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        public static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        public static double Length((double X, double Y, double Z) a) => Math.Sqrt(Dot(a, a));
    }
}
