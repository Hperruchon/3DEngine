using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Preview;
using Engine.Core.Queries;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0187 (phase R25, TASK-0462): the preview of a proposal in the window. The desktop host asks the queue for the shape of each
// body with a new solid, and the fork reads its triangles while its backend lives. The window draws the new shapes in dashed
// amber lines, in the mode of the switch "Before, After, Both", and a panel at the lower right gives the table "measure,
// before, after", the state of the solver after the commands and the checks that change. ADR-0187 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ProposalPreviewWindowTests
{
    private static readonly DocumentLine File = new("bracket", null);

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    // The bracket of MixImageTests: the sketch "Plate" of 60 by 40 on XY with its two distances, extruded by 10, and a post of
    // 10 by 10 by 10 at z 15 to 25 above it. The proposal "Widen and thicken the bracket" sets the width to 75 and the distance
    // to 25, so the bracket gets a new solid and meets the post.
    private sealed record Bracket(Guid Sketch, Guid Width, Guid Body, Guid Post);

    private static async Task<Bracket> Build(Engine.Core.DocumentSession session)
    {
        var sketch = Out(await session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id, SketchName = "Plate" }), "sketchId");
        (double X, double Y)[] corners = [(-30, -20), (30, -20), (30, 20), (-30, 20)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        var width = Out(await session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 60 }), "constraintId");
        await session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[1], Value = 40 });
        var body = Out(await session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 10 }), "bodyId");
        await session.Apply(new RenameBodyCommand { BodyId = body, NewName = "Bracket" });
        var box = Out(await session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }), "bodyId");
        var post = Out(await session.Apply(new TranslateCommand { BodyId = box, Dx = 0, Dy = 0, Dz = 20 }), "bodyId");
        await session.Apply(new RenameBodyCommand { BodyId = post, NewName = "Post" });
        return new Bracket(sketch, width, body, post);
    }

    private static string Proposal(Bracket bracket)
        => $"{{\"summary\":\"Widen and thicken the bracket\",\"commands\":["
           + $"{{\"name\":\"SetSketchConstraintValue\",\"schemaVersion\":1,\"parameters\":{{\"sketchId\":\"{bracket.Sketch}\",\"constraintId\":\"{bracket.Width}\",\"value\":75}}}},"
           + Thicker(bracket, 25) + "]}";

    private static string Thicker(Bracket bracket, double distance)
        => $"{{\"name\":\"SetExtrudeDistance\",\"schemaVersion\":1,\"parameters\":{{\"extrudeId\":\"{bracket.Body}\",\"distance\":{distance}}}}}";

    // The desktop host of a test, with a document file in a new folder, as ProposedSheetTests starts it.
    private static async Task Hosted(Func<DesktopSurface, Task> test)
    {
        var folder = Path.Combine(Path.GetTempPath(), "3de-preview-window-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "documents"));
        try
        {
            string[] args =
            [
                "--port", "0",
                "--file", Path.Combine(folder, "documents", "bracket.3de"),
                $"--{DesktopSurface.DocumentFolderSetting}={Path.Combine(folder, "documents")}",
                $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(folder, "unsaved")}",
                $"--Engine:AssetFolder={Path.Combine(folder, "assets")}",
            ];
            var (surface, code, message) = DesktopEngineHost.Start(args, null);
            Assert.True(surface is not null, $"{code}: {message}");
            using var session = surface.Session;
            using (surface)
                await test(surface);
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    // Posts the proposal over HTTP, as an agent does, and waits for its preview at the version of the Document.
    private static async Task<Guid> Post(DesktopSurface surface, string json)
    {
        using var client = new HttpClient { BaseAddress = new Uri(surface.Address) };
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/proposals", content);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Created, text);
        var id = System.Text.Json.JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Assert.NotNull(await surface.Host.Proposals.Preview(id, limit.Token));
        return id;
    }

    // Items 1 to 3: the desktop host gives the window the last preview of the shown proposal with the shape of each body with a
    // new solid. The widened bracket has a new solid, so its shape comes with the measures, the state of the solver of "Plate"
    // and the pair with the post, which goes from apart to overlapping. The post keeps its solid, so it has no shape. The shape
    // is the tessellation of the bracket after an accept. A new version makes the kept preview not current until its new fork
    // ends; a reject takes the shapes away.
    [Fact]
    public async Task The_Host_Gives_The_Window_The_Preview_With_The_Shape_Of_Each_New_Solid()
    {
        await Hosted(async surface =>
        {
            var bracket = await Build(surface.Host.Session);
            var id = await Post(surface, Proposal(bracket));
            IProposalDesk desk = new HostedSession(surface.Host);
            var card = desk.Preview(id) ?? throw new InvalidOperationException("No preview.");

            Assert.True(card.Current);
            Assert.Null(card.Error);
            Assert.Equal([bracket.Body], card.Shapes.Select(shape => shape.BodyId));
            Assert.Equal(0, card.ShapesLeftOut);
            Assert.Equal([bracket.Body], card.Replaced);
            var measure = Assert.Single(card.Measures);
            Assert.Equal(60 * 40 * 10, measure.Before?.Mass?.Volume ?? 0, 6);
            var volume = measure.After.Mass?.Volume ?? throw new InvalidOperationException("No volume after.");
            var sketch = Assert.Single(card.Sketches);
            Assert.Equal(bracket.Sketch, sketch.SketchId);
            Assert.Equal(SketchStates.Under, sketch.After.State);
            var pair = Assert.Single(card.Checks);
            Assert.Equal((PairStates.Apart, PairStates.Overlapping), (pair.Before?.State, pair.After?.State));

            // The lines of the panel, with the name of the sketch from the frame. A pair is in the order of its identifiers.
            string Named(Guid body) => body == bracket.Body ? "Bracket" : "Post";
            var named = card with { SketchNames = new Dictionary<Guid, string> { [bracket.Sketch] = "Plate" } };
            var proposal = Assert.Single(desk.Waiting());
            var lines = DesktopUi.PreviewLinesOf(named, proposal, [new LiveBody(bracket.Body, "Extrude", "Bracket", true), new LiveBody(bracket.Post, "Box", "Post", true)], "mm");
            Assert.Equal(
                ["Measure", "Bracket", "Measure: volume in cubic mm", "Solver after", $"Plate: under, {sketch.After.Freedom} free", "Checks that change", $"{Named(pair.BodyIdA)} · {Named(pair.BodyIdB)}: apart → overlapping"],
                lines.Select(line => line.Text));
            var shownVolume = Math.Round(volume, MidpointRounding.AwayFromZero).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
            Assert.Equal(("24 000", shownVolume), (lines[1].Before, lines[1].After));

            // The shape is the tessellation of the bracket after the accept: the fork and the Document give one solid.
            var shape = Assert.Single(card.Shapes);
            Assert.Equal(Engine.Api.Http.ProposalStates.Accepted, (await surface.Host.Proposals.Accept(id))?.State);
            var accepted = (await surface.Host.Session.Query<Engine.Contracts.Geometry.Tessellation>(new GetTessellationQuery { BodyId = bracket.Body })).Result!;
            Assert.Equal(accepted.Positions, shape.Triangles.Positions);
            Assert.Equal(accepted.Indices, shape.Triangles.Indices);
            Assert.Equal(volume, (await surface.Host.Session.Query<MassProperties>(new GetMassPropertiesQuery { BodyId = bracket.Body })).Result?.Volume);
            Assert.Empty(desk.Preview(id)?.Shapes ?? []);

            // A second proposal, then a command of the person: the kept preview is not current until its new fork ends.
            var second = await Post(surface, $"{{\"summary\":\"Thicker\",\"commands\":[{Thicker(bracket, 30)}]}}");
            var first = desk.Preview(second) ?? throw new InvalidOperationException("No preview.");
            Assert.True(first.Current);
            await surface.Host.Session.Apply(new RenameBodyCommand { BodyId = bracket.Post, NewName = "Pillar" });
            using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await surface.Host.Proposals.Preview(second, limit.Token);
            var again = desk.Preview(second) ?? throw new InvalidOperationException("No preview.");
            Assert.True(again.Current);
            Assert.True(again.Version > first.Version);
            Assert.Equal(PreviewStates.Ready, (await surface.Host.Proposals.Preview(second))?.State);
            Assert.Equal(Engine.Api.Http.ProposalStates.Rejected, desk.Reject(second).State);
            Assert.Empty(desk.Preview(second)?.Shapes ?? []);
        });
    }

    // A preview of one changed body for the tests of the panel, with no host.
    private static readonly Guid Plate = Guid.Parse("87000000-0000-0000-0000-000000000001");
    private static readonly Guid Shown = Guid.Parse("87000000-0000-0000-0000-0000000000a1");

    private static PreviewCard Card(Guid proposal, bool current = true, string? error = null)
    {
        var before = new BodyRecord(new BodyHandle(Plate), "Extrude", "Plate", true, Guid.Parse("87000000-0000-0000-0000-0000000000b1"));
        var after = before with { Geometry = Guid.Parse("87000000-0000-0000-0000-0000000000b2") };
        return new PreviewCard(proposal, 12, current, error, null, null, new BodyDifferences([], [], [new BodyChange(before, after)]), [],
            [new MeasureChange(Plate, new BodyMeasure(new MassProperties(24000, new DatumPoint(0, 0, 5), false), null), new BodyMeasure(new MassProperties(30000, new DatumPoint(0, 0, 5), false), null))],
            [], [], 0);
    }

    private static ProposalCard Waiting(Guid id) => new(id, "Widen the plate", ["SetSketchConstraintValue a b 75"]);

    private static void Step(DesktopUi ui, Vector2 display, Guid proposal, PreviewCard? card, params InputSample[] frame)
    {
        ui.ShowProposalPreview(card);
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, [new LiveBody(Plate, "Extrude", "Plate", true)], null, File, proposals: [Waiting(proposal)]);
    }

    private static PointerSample Pointer(Vector2 at, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    private static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    // Items 4 and 6: at 1280 by 720 the panel is at the lower right, inside the display and above the status line, on no part of
    // the arc. It starts in the mode Both; a tap on a segment of the switch sets its mode and gives no action, so nothing goes to
    // the engine. Another proposal starts in Both again. "updating" shows while a later version waits for its preview.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1440, 900)]
    [InlineData(1024, 768)]
    public void The_Switch_Sets_The_Mode_Of_The_Scene_And_Sends_Nothing(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var proposal = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
            Step(ui, display, proposal, Card(proposal));

        var panel = ui.PreviewRectangle ?? throw new InvalidOperationException($"No panel at {display}.");
        Assert.True(panel.X >= 0f && panel.Y >= 0f && panel.Z <= display.X && panel.W <= display.Y, $"The panel {panel} is outside the display {display}.");
        var status = ui.ItemRectangle(DesktopUi.StatusLabel) ?? throw new InvalidOperationException("No status line.");
        Assert.True(panel.W <= status.Y, $"The panel {panel} is not above the status line {status}.");
        Assert.DoesNotContain(ui.ProposalCovers, cover => Meets(cover, panel));
        Assert.Equal(PreviewMode.Both, ui.PreviewMode);
        Assert.Equal(DesktopUi.PreviewTitleText, ui.PreviewTexts[DesktopUi.PreviewTitleItem]);
        Assert.Equal(("Plate", "24 000", "30 000"), (ui.PreviewTexts["Preview:measure:0"], ui.PreviewTexts["Preview:measure:0:before"], ui.PreviewTexts["Preview:measure:0:after"]));

        foreach (var (mode, _) in DesktopUi.PreviewModes)
        {
            var segment = ui.ItemRectangle(DesktopUi.PreviewSwitchItem(mode)) ?? throw new InvalidOperationException($"No segment {mode}.");
            Assert.True(segment.W - segment.Y >= DesktopUi.MinimumControlHeight - 0.5f);
            Assert.True(segment.X >= panel.X && segment.Z <= panel.Z);
            var at = new Vector2((segment.X + segment.Z) / 2f, (segment.Y + segment.W) / 2f);
            Assert.True(ui.OverPanel(at.X, at.Y));
            ui.TakeActions();
            Step(ui, display, proposal, Card(proposal), Pointer(at, PointerPhase.Down, 0));
            Step(ui, display, proposal, Card(proposal), Pointer(at, PointerPhase.Up, 50));
            Step(ui, display, proposal, Card(proposal));
            Assert.Empty(ui.TakeActions());
            Assert.Equal(mode, ui.PreviewMode);
        }

        // "updating" while a later version waits.
        Step(ui, display, proposal, Card(proposal, current: false));
        Assert.Equal($"{DesktopUi.PreviewTitleText} · {DesktopUi.PreviewUpdatingText}", ui.PreviewTexts[DesktopUi.PreviewTitleItem]);
        Assert.Equal(PreviewMode.Both, ui.PreviewMode);

        // Another proposal starts in Both.
        var tap = ui.ItemRectangle(DesktopUi.PreviewSwitchItem(PreviewMode.After))!.Value;
        var middle = new Vector2((tap.X + tap.Z) / 2f, (tap.Y + tap.W) / 2f);
        Step(ui, display, proposal, Card(proposal), Pointer(middle, PointerPhase.Down, 100));
        Step(ui, display, proposal, Card(proposal), Pointer(middle, PointerPhase.Up, 150));
        Assert.Equal(PreviewMode.After, ui.PreviewMode);
        // A small window shows no panel and draws Both; the mode of the switch comes back with the panel.
        Step(ui, new Vector2(900, 600), proposal, Card(proposal));
        Assert.Equal(PreviewMode.Both, ui.PreviewMode);
        Step(ui, display, proposal, Card(proposal));
        Assert.Equal(PreviewMode.After, ui.PreviewMode);
        var other = Guid.NewGuid();
        Step(ui, display, other, Card(other));
        Assert.Equal(PreviewMode.Both, ui.PreviewMode);

        // A failed preview says so, with no table.
        Step(ui, display, other, Card(other, error: DiagnosticCodes.ApiPreviewFailed));
        Assert.Equal(("The preview failed", DiagnosticCodes.ApiPreviewFailed), (ui.PreviewTexts["Preview:failed"], ui.PreviewTexts["Preview:failed:code"]));
        Assert.False(ui.PreviewTexts.ContainsKey("Preview:header"));
    }

    // Item 6 (desktop first): a portrait display, a small window and the sheet page show no panel, and the scene draws the mode
    // Both. A preview of another proposal than the shown one shows no panel.
    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(900, 600)]
    [InlineData(1280, 560)]
    public void A_Phone_And_A_Small_Window_Show_No_Panel_And_Draw_Both(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var proposal = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
            Step(ui, display, proposal, Card(proposal));

        Assert.Null(ui.PreviewRectangle);
        Assert.Empty(ui.PreviewTexts);
        Assert.Equal(PreviewMode.Both, ui.PreviewMode);
    }

    [Fact]
    public void A_Preview_Of_Another_Proposal_Shows_No_Panel()
    {
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var proposal = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
            Step(ui, display, proposal, Card(Guid.NewGuid()));
        Assert.Null(ui.PreviewRectangle);
    }

    // Item 6: a number of the table has groups of three digits, no decimal from 100 and at most two below it.
    [Theory]
    [InlineData(320000000d, "320 000 000")]
    [InlineData(66948.75, "66 949")]
    [InlineData(100d, "100")]
    [InlineData(12.345, "12.35")]
    [InlineData(0.5, "0.5")]
    [InlineData(-2500d, "-2 500")]
    public void A_Number_Of_The_Table_Has_Groups_Of_Three_Digits(double value, string shown)
        => Assert.Equal(shown, DesktopUi.Number(value));

    // Item 4: "After" does not draw each removed body, each changed body with a new solid and each changed body that the commands
    // hide; a rename keeps the body.
    [Fact]
    public void After_Replaces_The_Removed_Bodies_And_The_Bodies_With_A_New_Solid()
    {
        BodyRecord Record(int n, Guid? geometry = null, bool visible = true, string? name = null)
            => new(new BodyHandle(Guid.Parse($"88000000-0000-0000-0000-00000000000{n}")), "Box", name, visible, geometry ?? Guid.Parse($"88000000-0000-0000-0000-0000000000a{n}"));
        var removed = Record(1);
        var (renamedBefore, renamedAfter) = (Record(2), Record(2, name: "Shelf"));
        var (cutBefore, cutAfter) = (Record(3), Record(3, geometry: Guid.NewGuid()));
        var (shownBefore, hiddenAfter) = (Record(4), Record(4, visible: false));
        var card = new PreviewCard(Shown, 3, true, null, null, null,
            new BodyDifferences([Record(5)], [removed], [new BodyChange(renamedBefore, renamedAfter), new BodyChange(cutBefore, cutAfter), new BodyChange(shownBefore, hiddenAfter)]),
            [], [], [], [], 0);

        Assert.Equal(new HashSet<Guid> { removed.Handle.Id, cutAfter.Handle.Id, hiddenAfter.Handle.Id }, card.Replaced);
        Assert.Equal("Shelf", card.BodyName(renamedAfter.Handle.Id));
        Assert.Empty((card with { Bodies = null }).Replaced);
    }

    // Item 5: the projection of the shapes into the scene of the renderer. The meshes go up when the preparation on the thread
    // pool is complete; the meshes of the shown shapes stay until then; the same shapes do not go up again; and a card with no
    // shape takes the meshes away.
    [VulkanImageFact]
    public void The_Shapes_Go_Up_When_They_Are_Ready_And_The_Old_Shapes_Stay_Until_Then()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        var box = Cube(10);
        var wider = Cube(20);
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            var meshes = new Dictionary<string, GpuMesh>();
            var held = new List<TaskCompletionSource<PreviewMeshes.Prepared[]>>();
            var works = new List<Func<PreviewMeshes.Prepared[]>>();
            var projection = new PreviewMeshes
            {
                RunPrepare = work =>
                {
                    var done = new TaskCompletionSource<PreviewMeshes.Prepared[]>();
                    held.Add(done);
                    works.Add(work);
                    return done.Task;
                },
            };
            try
            {
                var first = Card(Shown) with { Shapes = [new PreviewShape(Plate, box)] };
                Assert.False(projection.Follow(first, meshes, device));
                Assert.Empty(meshes);
                held[0].SetResult(works[0]());
                Assert.True(projection.Follow(first, meshes, device));
                Assert.Equal([PreviewMeshes.MeshId(Plate)], meshes.Keys);
                Assert.True(meshes[PreviewMeshes.MeshId(Plate)].EdgeVertexCount > 0, "The shape has no edge lines.");
                Assert.Equal(((-5d, -5d, -5d), (5d, 5d, 5d)), projection.Bounds);
                Assert.False(projection.Follow(first, meshes, device));
                Assert.Equal(1, projection.Uploads);

                // A new preview: the old mesh stays until the new one is ready.
                var second = Card(Shown) with { Shapes = [new PreviewShape(Plate, wider)] };
                var old = meshes[PreviewMeshes.MeshId(Plate)];
                Assert.False(projection.Follow(second, meshes, device));
                Assert.Same(old, meshes[PreviewMeshes.MeshId(Plate)]);
                held[1].SetResult(works[1]());
                Assert.True(projection.Follow(second, meshes, device));
                Assert.NotSame(old, meshes[PreviewMeshes.MeshId(Plate)]);
                Assert.Equal(((-10d, -10d, -10d), (10d, 10d, 10d)), projection.Bounds);
                Assert.Single(projection.Scene.Entities);

                // No shape: the meshes go.
                Assert.True(projection.Follow(null, meshes, device));
                Assert.Empty(meshes);
                Assert.Empty(projection.Scene.Entities);
                Assert.Null(projection.Bounds);
                Assert.Equal(2, held.Count);
            }
            finally
            {
                device.WaitIdle();
                foreach (var mesh in meshes.Values)
                    mesh.Dispose();
            }
        }

        Assert.Equal(messagesBefore, GraphicsDevice.ValidationMessageCount);
    }

    // A cube of the size with its centre at the origin, as 12 triangles.
    private static Engine.Contracts.Geometry.Tessellation Cube(double size)
    {
        var h = size / 2;
        double[] positions = [-h, -h, -h, h, -h, -h, h, h, -h, -h, h, -h, -h, -h, h, h, -h, h, h, h, h, -h, h, h];
        uint[] indices = [0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7];
        return new Engine.Contracts.Geometry.Tessellation(positions, indices);
    }

    // Items 4 to 6: the pictures of the bracket and the post with the proposal at 1280 by 720: the scene in each mode with no
    // panel, and the window in the mode Both with the arc and the panel. "Before" has no amber line. "Both" has the new shape in
    // amber lines over the present bracket. "After" has the same amber lines and no present bracket. The validation layer gives
    // no message.
    [VulkanImageFact]
    public async Task The_Pictures_Show_The_New_Shape_In_Dashed_Amber_Lines_In_Each_Mode()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        const uint width = 1280, height = 720;
        await Hosted(async surface =>
        {
            var bracket = await Build(surface.Host.Session);
            var id = await Post(surface, Proposal(bracket));
            var desk = new HostedSession(surface.Host);
            var sketches = new[] { (await surface.Host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = bracket.Sketch })).Result! };
            var preview = (desk.Preview(id) ?? throw new InvalidOperationException("No preview.")) with { SketchNames = new Dictionary<Guid, string> { [bracket.Sketch] = "Plate" } };

            using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
            var scene = SessionScene.Create();
            var camera = scene.Cameras[0];
            var display = new Vector2(width, height);
            var pictures = new Dictionary<string, byte[]>();
            using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
            {
                using var target = new OffscreenTarget(device, new VkExtent2D(width, height));
                using var renderer = new SceneRenderer(device, target.RenderPass, desk);
                using var panelRenderer = new UiRenderer(device, target.RenderPass);
                var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
                ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
                renderer.Update(scene, camera, (float)width / height);
                var bounds = renderer.Bounds ?? throw new InvalidOperationException("No bounds.");
                // The camera frames the box of the bodies and of the wider bracket, so the new shape shows whole.
                camera.Frame((bounds.Min.X - 8, bounds.Min.Y, bounds.Min.Z), (bounds.Max.X + 8, bounds.Max.Y, bounds.Max.Z + 15), (float)width / height);

                foreach (var mode in new[] { PreviewMode.Before, PreviewMode.After, PreviewMode.Both })
                {
                    renderer.ProposalPreview = new PreviewDraw(preview, mode);
                    renderer.Update(scene, camera, (float)width / height);
                    await renderer.PreviewMeshes.Preparing;
                    renderer.Update(scene, camera, (float)width / height);
                    Assert.Equal([bracket.Body], renderer.PreviewMeshes.Shown);
                    pictures[mode.ToString()] = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));
                }

                // The window as the person sees it: the arc of the proposal on the ring of the bracket and the panel of the preview.
                var cards = ProposedValues.Named(desk.Waiting(), sketches, renderer.LiveBodies, null);
                var extentOf = renderer.BoxOf(bracket.Body) ?? throw new InvalidOperationException("The bracket has no mesh.");
                var middle = ((extentOf.Min.X + extentOf.Max.X) / 2, (extentOf.Min.Y + extentOf.Max.Y) / 2, (extentOf.Min.Z + extentOf.Max.Z) / 2);
                var centre = SketchOverlay.Segment(camera, width, height, middle, middle)?.A ?? throw new InvalidOperationException("The bracket is behind the eye.");
                var diagonal = SketchOverlay.Segment(camera, width, height, extentOf.Min, extentOf.Max) ?? throw new InvalidOperationException("The bracket is behind the eye.");
                var data = UiDrawData.Empty;
                for (var frame = 0; frame < 3; frame++)
                {
                    ui.ShowProposalPreview(preview);
                    ui.Route([]);
                    data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, bracket.Body, File, ringCentre: centre, ringBody: Vector2.Distance(diagonal.A, diagonal.B) / 2f, proposals: cards);
                }

                // Item 6: the panel ends left of each target of the ring of the bracket at its height.
                var panel = ui.PreviewRectangle ?? throw new InvalidOperationException("No panel.");
                Assert.NotEmpty(ui.RingLabels);
                foreach (var label in ui.RingLabels)
                    Assert.False(ui.RingTarget(label) is { } tool && Meets(tool, panel), $"The panel {panel} lies on the tool {label} of the ring.");
                renderer.ProposalPreview = new PreviewDraw(preview, ui.PreviewMode);
                pictures["window"] = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera,
                    overlay => panelRenderer.Record(overlay, extent, data, 0), bracket.Body));
            }

            foreach (var (name, pixels) in pictures)
                Save(pixels, width, height, $"preview-{name.ToLowerInvariant()}-{width}x{height}.png");
            var (before, after, both) = (Amber(pictures["Before"]), Amber(pictures["After"]), Amber(pictures["Both"]));
            Assert.True(before == 0, $"The scene before has {before} amber pixels.");
            Assert.True(both > 300, $"The scene with both has {both} amber pixels.");
            Assert.True(after > 300, $"The scene after has {after} amber pixels.");
            // The present bracket does not draw after: fewer pixels of a body than with both.
            var (bodyAfter, bodyBoth) = (Shaded(pictures["After"]), Shaded(pictures["Both"]));
            Assert.True(bodyAfter < bodyBoth - 2000, $"After has {bodyAfter} pixels of a body, and both has {bodyBoth}.");
            // The lines are dashed: along the edges of the new shape about half of the points are amber.
            var along = AmberAlong(pictures["After"], width, height, camera, preview.Shapes[0].Triangles);
            Assert.True(along is > 0.3 and < 0.8, $"{along:P0} of the points along the edges of the new shape are amber.");
        });

        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));
    }

    // The pixels in the amber of a proposal, #ffa040, with the soft border of a line: red high, green in the middle, blue low.
    private static int Amber(byte[] pixels)
    {
        var count = 0;
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            if (pixels[i] > 150 && pixels[i] > pixels[i + 1] + 40 && pixels[i + 1] > pixels[i + 2] + 30)
                count++;
        }

        return count;
    }

    // The pixels of a shaded body: blue above red by more than the ground, and brighter than the ground.
    private static int Shaded(byte[] pixels)
    {
        var count = 0;
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            if (pixels[i + 2] > 60 && pixels[i + 2] > pixels[i] + 20)
                count++;
        }

        return count;
    }

    // The share of the points along the middle of each feature edge of the shape whose pixel is amber. A dashed line keeps the
    // first half of each period of 10 pixels, so about half of the points are amber; a solid line gives nearly all of them.
    private static double AmberAlong(byte[] pixels, uint width, uint height, ThreeDEngine.Core.Models.Camera camera, Engine.Contracts.Geometry.Tessellation shape)
    {
        var (covered, samples) = (0, 0);
        var positions = shape.Positions;
        foreach (var (a, b) in EdgeLines.Feature(positions, shape.Indices))
        {
            var from = (positions[3 * a], positions[(3 * a) + 1], positions[(3 * a) + 2]);
            var to = (positions[3 * b], positions[(3 * b) + 1], positions[(3 * b) + 2]);
            if (SketchOverlay.Segment(camera, width, height, from, to) is not { } segment)
                continue;
            var length = Vector2.Distance(segment.A, segment.B);
            if (length < 40f)
                continue;
            for (var t = 0.1f; t <= 0.9f; t += 1f / length)
            {
                var point = Vector2.Lerp(segment.A, segment.B, t);
                var (column, row) = ((int)MathF.Round(point.X), (int)MathF.Round(point.Y));
                if (column < 0 || row < 0 || column >= width || row >= height)
                    continue;
                samples++;
                var i = ((row * (int)width) + column) * 4;
                if (pixels[i] > 150 && pixels[i] > pixels[i + 1] + 40 && pixels[i + 1] > pixels[i + 2] + 30)
                    covered++;
            }
        }

        Assert.True(samples > 200, $"Only {samples} points lie on the edges of the shape.");
        return (double)covered / samples;
    }

    private static void Save(byte[] pixels, uint width, uint height, string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        Png.Write(Path.Combine(folder, name), (int)width, (int)height, pixels);
    }
}
