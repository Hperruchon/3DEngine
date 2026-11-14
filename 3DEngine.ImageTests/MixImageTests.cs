using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0093 (the redesign, slice j, TASK-0304): the scene of the render "mix" of the designer brief at 1440 x 900, the size
// at which the owner judges each slice of the redesign: a bracket of 60 x 40 x 10, selected, framed as the button Frame
// does, with its ring and its values. The picture is kept for the owner, beside the render.
// Phase R16q (TASK-0325): the sketch has the name "Plate", and the history arc of the bracket shows at the lower left of its
// ring: "Bracket", then "Plate on XY".
// Phase R16r (TASK-0326): a second picture holds one open proposal of an agent, "Widen the bracket to 75 mm", with one
// command SetSketchConstraintValue on the distance of 60 of the sketch: the amber arc above the ring, with Reject and Accept.
// Phase R16s (TASK-0327): a third picture holds the sketch "Plate" active, as a tap on its step of the history arc makes it:
// under its body, with its rows at the left, its ring, and a chip on each of its two lengths.
// Phase R16t (TASK-0334): a fourth picture holds the proposal of the second picture with the proposed value of its command,
// as the host gives it: the chip of the width shows "60.000 → 75.000 mm" as a pending chip, and the line of the arc names the value,
// "Plate C1  60 → 75 mm"; the chip has three decimals (ADR-0115 item 1, TASK-0340), and the line keeps its form.
[Collection(DearImGui.Collection)]
public class MixImageTests
{
    private const uint Width = 1440;
    private const uint Height = 900;

    private static Guid Out(Engine.Contracts.CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    [VulkanImageFact]
    public Task The_Selected_Bracket_Of_The_Render_Draws_At_1440_By_900() => Draw("mix-selected.png", null);

    // TASK-0326: the same scene while one proposal of an agent waits. The parameters are the names of the schema of the handler.
    [VulkanImageFact]
    public Task The_Bracket_With_A_Proposal_Draws_At_1440_By_900() => Draw("mix-proposal.png", (sketch, constraint) =>
    {
        using var parameters = System.Text.Json.JsonDocument.Parse($"{{\"sketchId\":\"{sketch}\",\"constraintId\":\"{constraint}\",\"value\":75}}");
        var line = ProposalCard.Line("SetSketchConstraintValue", parameters.RootElement.EnumerateObject().Select(member => KeyValuePair.Create(member.Name, member.Value)));
        return [new ProposalCard(Guid.Parse("74000000-0000-0000-0000-000000000001"), "Widen the bracket to 75 mm", [line])];
    });

    // TASK-0334: the same scene as the second picture, with the proposed value of the command, named from the frame.
    [VulkanImageFact]
    public Task The_Bracket_With_A_Pending_Chip_Draws_At_1440_By_900() => Draw("mix-pending.png", (sketch, constraint) =>
    {
        using var parameters = System.Text.Json.JsonDocument.Parse($"{{\"sketchId\":\"{sketch}\",\"constraintId\":\"{constraint}\",\"value\":75}}");
        var members = parameters.RootElement.EnumerateObject().Select(member => KeyValuePair.Create(member.Name, member.Value.Clone())).ToList();
        return [new ProposalCard(Guid.Parse("74000000-0000-0000-0000-000000000001"), "Widen the bracket to 75 mm", [ProposalCard.Line("SetSketchConstraintValue", members)],
            Values: [ProposedValues.Of("SetSketchConstraintValue", members)])];
    }, pending: true);

    // TASK-0327: the same scene while the sketch of the bracket is active. The values come from the method of the host.
    [VulkanImageFact]
    public Task The_Bracket_With_Its_Sketch_Active_Draws_At_1440_By_900() => Draw("mix-sketch.png", null, sketchActive: true);

    // Phase R18j (TASK-0359): the picture of the fourth test at each size of the brief that is not 1440 by 900: a small laptop,
    // a tablet on its side and upright, and a phone, for the comparison of docs/proposals/2026-11-14-the-render-at-other-sizes.md.
    [VulkanImageFact]
    public Task The_Bracket_With_A_Pending_Chip_Draws_At_1280_By_720() => Sized(1280, 720);

    [VulkanImageFact]
    public Task The_Bracket_With_A_Pending_Chip_Draws_At_1024_By_768() => Sized(1024, 768);

    [VulkanImageFact]
    public Task The_Bracket_With_A_Pending_Chip_Draws_At_768_By_1024() => Sized(768, 1024);

    [VulkanImageFact]
    public Task The_Bracket_With_A_Pending_Chip_Draws_At_390_By_844() => Sized(390, 844);

    private static Task Sized(uint frameWidth, uint frameHeight) => Draw($"mix-pending-{frameWidth}x{frameHeight}.png", (sketch, constraint) =>
    {
        using var parameters = System.Text.Json.JsonDocument.Parse($"{{\"sketchId\":\"{sketch}\",\"constraintId\":\"{constraint}\",\"value\":75}}");
        var members = parameters.RootElement.EnumerateObject().Select(member => KeyValuePair.Create(member.Name, member.Value.Clone())).ToList();
        return [new ProposalCard(Guid.Parse("74000000-0000-0000-0000-000000000001"), "Widen the bracket to 75 mm", [ProposalCard.Line("SetSketchConstraintValue", members)],
            Values: [ProposedValues.Of("SetSketchConstraintValue", members)])];
    }, pending: true, frameWidth: frameWidth, frameHeight: frameHeight);

    private static async Task Draw(string name, Func<Guid, Guid, IReadOnlyList<ProposalCard>>? proposal, bool sketchActive = false, bool pending = false, uint frameWidth = Width, uint frameHeight = Height)
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new Engine.Geometry.Manifold.ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id, SketchName = "Plate" }), "sketchId");
        (double X, double Y)[] corners = [(-30, -20), (30, -20), (30, 20), (-30, 20)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        var width = Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 60 }), "constraintId");
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[1], Value = 40 });
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 10 }), "bodyId");
        await host.Session.Apply(new RenameBodyCommand { BodyId = body, NewName = "Bracket" });
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;
        var features = (await host.Session.Query<FeaturesInfo>(new GetFeaturesQuery())).Result;

        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        var display = new Vector2(frameWidth, frameHeight);
        byte[] pixels;
        IReadOnlyList<ProposalCard>? proposals;
        IReadOnlyList<EdgeValues.Value> sketchValues = [];
        IReadOnlyList<EdgeValues.Value> values;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(frameWidth, frameHeight));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            using var panelRenderer = new UiRenderer(device, target.RenderPass);
            var (atlasWidth, atlasHeight, rgba) = ui.FontAtlas();
            ui.SetFontTexture(panelRenderer.CreateTexture(atlasWidth, atlasHeight, rgba));
            renderer.Update(scene, camera, (float)frameWidth / frameHeight);
            var bounds = renderer.Bounds ?? throw new InvalidOperationException("No bounds.");
            camera.Frame(bounds.Min, bounds.Max, (float)frameWidth / frameHeight);
            renderer.Update(scene, camera, (float)frameWidth / frameHeight);

            var extent = renderer.BoxOf(body) ?? throw new InvalidOperationException("The body has no mesh.");
            var middle = ((extent.Min.X + extent.Max.X) / 2, (extent.Min.Y + extent.Max.Y) / 2, (extent.Min.Z + extent.Max.Z) / 2);
            var centre = SketchOverlay.Segment(camera, frameWidth, frameHeight, middle, middle)?.A ?? throw new InvalidOperationException("The body is behind the eye.");
            var diagonal = SketchOverlay.Segment(camera, frameWidth, frameHeight, extent.Min, extent.Max) ?? throw new InvalidOperationException("The body is behind the eye.");
            var size = Vector2.Distance(diagonal.A, diagonal.B) / 2f;
            values = EdgeValues.Of(body, 10, faces.Edges, [info], camera, frameWidth, frameHeight, "mm");
            var history = HistorySteps.Of(features, renderer.LiveBodies, [info], body);
            // TASK-0334: as NativeThreeDEngine.Render does, the proposed values take their names from the frame.
            proposals = ProposedValues.Named(proposal?.Invoke(sketch, width), [info], renderer.LiveBodies, values);

            // TASK-0327: as NativeThreeDEngine.Render does for an active sketch that makes a live extrude (ADR-0085 items 2 to 4):
            // the sketch under its body, its overlay and its window, the ring at the centre of its points, and its values.
            SketchOverlay.Strokes? strokes = null;
            SketchPanel? sketchPanel = null;
            if (sketchActive)
            {
                var actions = new SketchActions(host.Session);
                actions.Edit(sketch);
                var lift = SketchLift.Under(sketch, SketchOverlay.PlaneOf(info)!, extent.Min, extent.Max) ?? throw new InvalidOperationException("No move.");
                actions.Lift = lift;
                var seen = lift.Apply(info);
                strokes = SketchOverlay.Of([seen], camera, frameWidth, frameHeight, new SketchOverlay.Focus(sketch, [], null));
                // ADR-0120 items 2 and 3 (TASK-0347): the sketch is under the body that it makes, and its row "Makes" gives that body.
                strokes = strokes with { Lines = [.. strokes.Lines, .. lift.Links(info, camera, frameWidth, frameHeight)], Under = true };
                sketchPanel = new SketchPanel([info], info, actions.Tool.Kind, false, [], null, [new HistoryStep(body, "Extrude", "Bracket", "correct", null, HistoryTap.SelectBody)]);
                var shown = seen.Points.Select(point => SketchOverlay.Pixel(seen, camera, frameWidth, frameHeight, (point.X, point.Y))).OfType<Vector2>().ToList();
                var (low, high) = (shown.Aggregate(Vector2.Min), shown.Aggregate(Vector2.Max));
                (centre, size) = ((low + high) / 2f, Vector2.Distance(low, high) / 2f);
                sketchValues = NativeThreeDEngine.SketchValuesOf(actions, [info], camera, frameWidth, frameHeight, "mm");
            }

            var data = UiDrawData.Empty;
            for (var frame = 0; frame < 3; frame++)
                data = ui.Frame(1f / 60f, display, Vector2.One, renderer.LiveBodies, body, new DocumentLine("bracket", null), sketches: strokes, sketchPanel: sketchPanel,
                    ringCentre: centre, ringBody: size, values: values, history: history, proposals: proposals, sketchValues: sketchValues);

            pixels = target.Render((commandBuffer, framebuffer, frameExtent) =>
                renderer.Record(commandBuffer, framebuffer, frameExtent, scene, camera,
                    overlay => panelRenderer.Record(overlay, frameExtent, data, 0), body));
        }

        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var picture = Path.Combine(folder, name);
        Png.Write(picture, (int)frameWidth, (int)frameHeight, pixels);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages.");
        Assert.True(pixels.Length == frameWidth * frameHeight * 4, $"The picture {picture} has {pixels.Length} bytes.");
        if (sketchActive)
        {
            // TASK-0327: a chip on each of the two lengths of the sketch, and no chip of the extrude.
            Assert.Equal(["C1  60.000 mm", "C2  40.000 mm"], sketchValues.Select(value => value.Text));
            Assert.All(sketchValues, value => Assert.NotNull(ui.ItemRectangle(value.Key)));
            Assert.All(values, value => Assert.Null(ui.ItemRectangle(value.Key)));
            // ADR-0120 (TASK-0347): the label of the plate, its caption and the row "Makes".
            foreach (var item in new[] { DesktopUi.PlateLabelItem, DesktopUi.PlateCaptionItem, DesktopUi.MakesItem })
                Assert.NotNull(ui.ItemRectangle(item));
        }
        // TASK-0359: a portrait display has no history arc (ADR-0104 item 3), and its proposal is a sheet (ADR-0092 item 7).
        else if (frameWidth >= frameHeight)
        {
            Assert.NotNull(ui.ItemRectangle(DesktopUi.HistoryItem(body)));
            Assert.NotNull(ui.ItemRectangle(DesktopUi.HistoryItem(sketch)));
        }

        if (proposals is not null)
        {
            // Finding V180 of the codebase review of 2026-11-14 (b) (TASK-0362): the arc of a landscape display and the sheet
            // of a portrait display both give Reject and Accept.
            Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalRejectItem));
            Assert.NotNull(ui.ItemRectangle(DesktopUi.ProposalAcceptItem));

            if (pending)
            {
                // TASK-0334: the chip of the width is pending with the change, and the line names the value.
                Assert.Contains((EdgeValues.ConstraintKey(width), "60.000 → 75.000 mm", true), ui.Chips);
                Assert.Equal("Plate C1  60 → 75 mm", ui.ProposalTexts[DesktopUi.ProposalLineItem(0)]);

                // ADR-0135 item 4 (phase R18o, TASK-0368): on a portrait display the chip of the width lies on no window and
                // on no row, so the person reads the change. Until TASK-0368 the sheet lay on it.
                if (frameWidth < frameHeight && ui.ItemRectangle(EdgeValues.ConstraintKey(width)) is { } chip)
                {
                    static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;
                    Assert.DoesNotContain(ui.PanelRectangles, panel => Meets(chip, panel));
                    Assert.False(ui.RowsArea is { } rows && Meets(chip, rows), $"The chip {chip} lies on the rows {ui.RowsArea}.");
                }
            }
            else
                Assert.StartsWith("SetSketchConstraintValue ", ui.ProposalTexts[DesktopUi.ProposalLineItem(0)], StringComparison.Ordinal);
        }
    }
}
