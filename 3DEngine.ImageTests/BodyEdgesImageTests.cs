using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using Vortice.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0109 (phase R18b, TASK-0333): the edges of a body as lines. The feature edges of a box are its twelve edges, and the
// seams between the facets of a cylinder are not edges. In a picture, each visible edge of a box is a bright line; when the
// box is selected, each visible edge is a solid line in the colour of the selection, and each hidden edge is dashed. The
// test projects each edge of the box on the CPU and reads the pixels on the line, with no reference picture. ADR-0109
// names this file in its enforced-by field.
public sealed class BodyEdgesImageTests
{
    private const uint Width = 800;
    private const uint Height = 600;

    // The part of an edge that the test reads: the middle, away from the corners, where two edges meet.
    private const float Margin = 0.1f;

    [Fact]
    public async Task The_Feature_Edges_Of_A_Box_Are_Its_Twelve_Edges()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var box = await Body(host, new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });
        var tessellation = await Tessellation(host, box);

        var edges = EdgeLines.Feature(tessellation.Positions, tessellation.Indices);

        Assert.Equal(12, edges.Count);
        // Each edge joins two corners that differ in one coordinate only.
        foreach (var (a, b) in edges)
        {
            var same = Enumerable.Range(0, 3).Count(axis => tessellation.Positions[(3 * a) + axis] == tessellation.Positions[(3 * b) + axis]);
            Assert.Equal(2, same);
        }

        Assert.Equal(12 * EdgeLines.VerticesPerEdge * EdgeLines.FloatsPerVertex, EdgeLines.Vertices(new float[tessellation.Positions.Length], edges).Length);
    }

    [Fact]
    public async Task The_Seams_Between_The_Facets_Of_A_Cylinder_Are_Not_Edges()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var cylinder = await Body(host, new CreateCylinderCommand { Radius = 10, Height = 20 });
        var tessellation = await Tessellation(host, cylinder);

        var edges = EdgeLines.Feature(tessellation.Positions, tessellation.Indices);

        // Each feature edge is on the rim of a cap: both ends have the height of the bottom or of the top.
        var heights = Enumerable.Range(0, tessellation.Positions.Length / 3).Select(i => tessellation.Positions[(3 * i) + 2]).ToList();
        var (bottom, top) = (heights.Min(), heights.Max());
        Assert.NotEmpty(edges);
        Assert.All(edges, edge =>
        {
            var (za, zb) = (tessellation.Positions[(3 * edge.A) + 2], tessellation.Positions[(3 * edge.B) + 2]);
            Assert.True(za == zb && (za == bottom || za == top), $"The edge from z = {za} to z = {zb} is not on a rim.");
        });
    }

    [VulkanImageFact]
    public async Task Each_Visible_Edge_Of_A_Box_Is_A_Bright_Line()
    {
        var (pixels, visible, _) = await Draw(selected: false, "edges-plain.png");

        foreach (var (a, b) in visible)
        {
            var samples = Samples(a, b).ToList();
            var bright = samples.Count(point => Near(pixels, point, IsBright));
            Assert.True(bright >= samples.Count * 9 / 10,
                $"The visible edge from {a} to {b} has {bright} bright pixels of {samples.Count}; the picture is {Picture("edges-plain.png")}.");
        }
    }

    [VulkanImageFact]
    public async Task The_Selected_Box_Has_Solid_Visible_Edges_And_Dashed_Hidden_Edges()
    {
        var (pixels, visible, hidden) = await Draw(selected: true, "edges-selected.png");

        foreach (var (a, b) in visible)
        {
            var samples = Samples(a, b).ToList();
            var orange = samples.Count(point => Near(pixels, point, IsOrange));
            Assert.True(orange >= samples.Count * 9 / 10,
                $"The visible edge from {a} to {b} has {orange} orange pixels of {samples.Count}; the picture is {Picture("edges-selected.png")}.");
        }

        Assert.NotEmpty(hidden);
        foreach (var (a, b) in hidden)
        {
            // A sample near a visible edge is not read: the two lines can cross in the picture.
            var samples = Samples(a, b).Where(point => visible.All(edge => DistanceToSegment(point, edge.A, edge.B) > 4f)).ToList();
            Assert.True(samples.Count >= 30, $"The hidden edge from {a} to {b} has {samples.Count} samples away from the visible edges.");
            var marks = samples.Select(point => Near(pixels, point, IsOrange)).ToList();
            var on = marks.Count(mark => mark);
            var changes = marks.Zip(marks.Skip(1)).Count(pair => pair.First != pair.Second);
            Assert.True(on >= samples.Count / 4 && on <= samples.Count * 3 / 4 && changes >= 4,
                $"The hidden edge from {a} to {b} has {on} orange samples of {samples.Count} and {changes} changes, so it is not dashed; the picture is {Picture("edges-selected.png")}.");
        }
    }

    // Draws a box, selected or not, and gives the picture, the visible edges and the hidden edges in pixels. An edge is
    // hidden when one of its ends is the corner of the box that is farthest from the eye. The camera must see three faces of
    // the box, so exactly the three edges at that corner are hidden.
    private static async Task<(byte[] Pixels, List<(Vector2 A, Vector2 B)> Visible, List<(Vector2 A, Vector2 B)> Hidden)> Draw(bool selected, string name)
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var box = await Body(host, new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 });
        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) bounds;
        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, new HostedSession(host));
            renderer.Update(scene, camera, (float)Width / Height);
            bounds = renderer.BoxOf(box) ?? throw new InvalidOperationException("The box has no mesh.");
            pixels = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera, selected: selected ? box : null));
        }

        Save(pixels, name);
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        var eye = camera.Eye();
        Assert.True(Outside(eye.X, bounds.Min.X, bounds.Max.X) && Outside(eye.Y, bounds.Min.Y, bounds.Max.Y) && Outside(eye.Z, bounds.Min.Z, bounds.Max.Z),
            $"The eye {eye} does not see three faces of the box {bounds}.");
        var far = (Far(eye.X, bounds.Min.X, bounds.Max.X), Far(eye.Y, bounds.Min.Y, bounds.Max.Y), Far(eye.Z, bounds.Min.Z, bounds.Max.Z));

        var corners = new List<(double X, double Y, double Z)>();
        foreach (var x in new[] { bounds.Min.X, bounds.Max.X })
            foreach (var y in new[] { bounds.Min.Y, bounds.Max.Y })
                foreach (var z in new[] { bounds.Min.Z, bounds.Max.Z })
                    corners.Add((x, y, z));

        var (visible, hidden) = (new List<(Vector2, Vector2)>(), new List<(Vector2, Vector2)>());
        for (var i = 0; i < corners.Count; i++)
        {
            for (var j = i + 1; j < corners.Count; j++)
            {
                var (p, q) = (corners[i], corners[j]);
                var differ = (p.X != q.X ? 1 : 0) + (p.Y != q.Y ? 1 : 0) + (p.Z != q.Z ? 1 : 0);
                if (differ != 1)
                    continue;
                var segment = (ToPixel(p, camera), ToPixel(q, camera));
                (p == far || q == far ? hidden : visible).Add(segment);
            }
        }

        return (pixels, visible, hidden);
    }

    private static bool Outside(double value, double min, double max) => value < min || value > max;

    private static double Far(double eye, double min, double max) => eye < min ? max : min;

    // The pixels at each step of one pixel along the middle of a segment.
    private static IEnumerable<(int Column, int Row)> Samples(Vector2 a, Vector2 b)
    {
        var length = Vector2.Distance(a, b);
        var steps = (int)(length * (1 - (2 * Margin)));
        for (var i = 0; i <= steps; i++)
        {
            var point = Vector2.Lerp(a, b, Margin + (i / length));
            yield return ((int)point.X, (int)point.Y);
        }
    }

    private static float DistanceToSegment((int Column, int Row) pixel, Vector2 a, Vector2 b)
    {
        var p = new Vector2(pixel.Column + 0.5f, pixel.Row + 0.5f);
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + (t * ab));
    }

    // The colour of an edge line: near white; the shaded face of a body has a red of at most 0.55 of 255.
    private static bool IsBright((int R, int G, int B) colour) => colour.R >= 180 && colour.G >= 180 && colour.B >= 180;

    // The colour of the selection: much more red than blue, brighter than the transparent fill of the selected body.
    private static bool IsOrange((int R, int G, int B) colour) => colour.R >= 180 && colour.R - colour.B >= 100;

    // A point of the scene in pixels of the picture: x to the right, y down from the top row, as in
    // FirstDemonstrationImageTests.
    private static Vector2 ToPixel((double X, double Y, double Z) point, Camera camera)
    {
        var eye = camera.Eye();
        var relative = new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1f);
        var clip = Vector4.Transform(relative, camera.View() * camera.Projection((float)Width / Height));
        var (ndcX, ndcY) = (clip.X / clip.W, clip.Y / clip.W);
        return new Vector2((ndcX + 1f) / 2f * Width, (1f - ndcY) / 2f * Height);
    }

    // A sample has a colour when the pixel of the point or one of its eight neighbours has it: the line has a width of
    // 1.5 to 2 pixels and a soft border, so the pixel of the point can be at its border.
    private static bool Near(byte[] pixels, (int Column, int Row) point, Func<(int R, int G, int B), bool> test)
        => new[] { -1, 0, 1 }.Any(dx => new[] { -1, 0, 1 }.Any(dy => test(Colour(pixels, (point.Column + dx, point.Row + dy)))));

    private static (int R, int G, int B) Colour(byte[] pixels, (int Column, int Row) point)
    {
        var offset = ((point.Row * (int)Width) + point.Column) * 4;
        return (pixels[offset], pixels[offset + 1], pixels[offset + 2]);
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

    // The picture goes to the folder of IMAGE_TEST_OUTPUT, which the pipeline keeps, or to the folder of the test.
    private static string Picture(string name)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, name);
    }

    private static void Save(byte[] pixels, string name) => Png.Write(Picture(name), (int)Width, (int)Height, pixels);
}
