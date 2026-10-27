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

// The image test of the first demonstration (ADR-0025, TASK-0076). It sends the four
// commands of the demonstration to a session, and draws the live bodies with the
// renderer of the desktop into an offscreen target of a device with no window. Then it
// compares the picture by structure (ADR-0025 item 5), with no reference picture:
//
// 1. The cut body has 14 vertices and 24 triangles.
// 2. Each pixel outside the silhouette that the CPU projects from the tessellation has
//    the colour of the clear, and each pixel inside it has another colour. A pixel
//    within 2 pixels of an edge is not compared.
// 3. The centre of each face of the notch has the shade that the shader gives to the
//    normal of that face, and the three shades differ.
//
// The validation layer checks the synchronization, and each message fails the test.
// ADR-0025 names this file in enforced-by.
public class FirstDemonstrationImageTests
{
    private const uint Width = 320;
    private const uint Height = 240;

    // The colour of a lit face, and the direction of the light in the space of the
    // view: the values of 3DEngine.Vulkan/Shaders/mesh.frag.
    private static readonly Vector3 BaseColour = new(0.55f, 0.65f, 0.80f);
    private static readonly Vector3 Light = Vector3.Normalize(new Vector3(0.3f, 0.5f, 1.0f));

    // A point at the centre of each face of the notch, and the normal of that face
    // (ADR-0025 item 1: the cut removes the cube from (0, 0, 0) to (5, 5, 5)).
    private static readonly ((double X, double Y, double Z) Point, Vector3 Normal)[] NotchFaces =
    [
        ((0.0, 2.5, 2.5), Vector3.UnitX),
        ((2.5, 0.0, 2.5), Vector3.UnitY),
        ((2.5, 2.5, 0.0), Vector3.UnitZ),
    ];

    [VulkanImageFact]
    public async Task The_First_Demonstration_Shows_A_Notch()
    {
        var messagesBefore = GraphicsDevice.ValidationMessageCount;
        using var host = new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);

        var cut = await FirstDemonstration(host);
        var tessellation = (await host.Session.Query<Tessellation>(new GetTessellationQuery { BodyId = cut })).Result!;

        var scene = SessionScene.Create();
        var camera = scene.Cameras[0];
        byte[] pixels;
        bool synchronizationChecked;

        using (var device = new GraphicsDevice("3DEngine.ImageTests", enableValidation: true, window: null, synchronizationValidation: true))
        {
            synchronizationChecked = device.SynchronizationValidationEnabled;
            using var target = new OffscreenTarget(device, new VkExtent2D(Width, Height));
            using var renderer = new SceneRenderer(device, target.RenderPass, host);

            renderer.Update(scene, camera, (float)Width / Height);
            pixels = target.Render((commandBuffer, framebuffer, extent) => renderer.Record(commandBuffer, framebuffer, extent, scene, camera));
        }

        var picture = SavePicture(pixels);

        if (VulkanImageFactAttribute.InContinuousIntegration)
            Assert.True(synchronizationChecked, "In continuous integration the validation layer must check the synchronization.");
        // The assertion gives the text of each message, so that the annotation of the
        // pipeline shows it to a person with no sign-in (finding V17 of the codebase
        // review of 2026-10-17, TASK-0079). Until TASK-0079 it gave a count only.
        Assert.True(GraphicsDevice.ValidationMessageCount == messagesBefore,
            $"The validation layer gave {GraphicsDevice.ValidationMessageCount - messagesBefore} messages:\n"
            + string.Join("\n", GraphicsDevice.ValidationMessages.Skip(messagesBefore)));

        Assert.Equal(14, tessellation.VertexCount);
        Assert.Equal(24, tessellation.TriangleCount);

        var problems = new List<string>();
        problems.AddRange(SilhouetteProblems(pixels, tessellation, camera));
        problems.AddRange(NotchProblems(pixels, camera));
        Assert.True(problems.Count == 0, $"The picture {picture} differs from its structure:\n  " + string.Join("\n  ", problems.Take(20)));
    }

    // The four commands of the first demonstration (ADR-0025 item 1). The result is the
    // identifier of the cut body.
    internal static async Task<Guid> FirstDemonstration(EngineHost host)
    {
        var boxA = await Apply(host, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 });
        var boxB = await Apply(host, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 });
        var movedB = await Apply(host, new TranslateCommand { BodyId = boxB, Dx = 5, Dy = 5, Dz = 5 });
        return await Apply(host, new SubtractCommand { MinuendBodyId = boxA, SubtrahendBodyId = movedB });
    }

    private static async Task<Guid> Apply(EngineHost host, Command command)
    {
        var result = await host.Session.Apply(command);
        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.True(result.Outputs.TryGet<Guid>("bodyId", out var bodyId), $"{command.Name} gave no body.");
        return bodyId;
    }

    // The silhouette that the CPU projects from the tessellation, against the colour of
    // the clear (ADR-0025 item 5, second check).
    private static IEnumerable<string> SilhouetteProblems(byte[] pixels, Tessellation tessellation, Camera camera)
    {
        var triangles = new List<(Vector2 A, Vector2 B, Vector2 C)>();
        for (var index = 0; index < tessellation.Indices.Length; index += 3)
        {
            triangles.Add((
                ToPixel(Vertex(tessellation, tessellation.Indices[index]), camera),
                ToPixel(Vertex(tessellation, tessellation.Indices[index + 1]), camera),
                ToPixel(Vertex(tessellation, tessellation.Indices[index + 2]), camera)));
        }

        bool Covered(float x, float y) => triangles.Any(t => Inside(new Vector2(x, y), t.A, t.B, t.C));

        var (inside, outside) = (0, 0);
        for (var row = 0; row < Height; row++)
        {
            for (var column = 0; column < Width; column++)
            {
                // The centre of the pixel, and eight points 2 pixels from it.
                var (x, y) = (column + 0.5f, row + 0.5f);
                var samples = new[] { -2f, 0f, 2f }.SelectMany(dx => new[] { -2f, 0f, 2f }.Select(dy => Covered(x + dx, y + dy))).ToArray();
                var background = IsBackground(pixels, column, row);

                if (samples.All(covered => covered))
                {
                    inside++;
                    if (background)
                        yield return $"the pixel ({column}, {row}) is inside the silhouette and has the colour of the clear";
                }
                else if (samples.All(covered => !covered))
                {
                    outside++;
                    if (!background)
                        yield return $"the pixel ({column}, {row}) is outside the silhouette and has the colour {Colour(pixels, column, row)}";
                }
            }
        }

        // A frame that misses the body, or fills the picture, compares nothing.
        if (inside < Width * Height / 20 || outside < Width * Height / 20)
            yield return $"the silhouette covers {inside} pixels and leaves {outside}; each must be at least 5 % of the picture";
    }

    // The shade at the centre of each face of the notch (ADR-0025 item 5, third check).
    private static IEnumerable<string> NotchProblems(byte[] pixels, Camera camera)
    {
        var expected = NotchFaces.Select(face => ExpectedShade(face.Normal, camera)).ToArray();

        for (var first = 0; first < expected.Length; first++)
        {
            for (var second = first + 1; second < expected.Length; second++)
            {
                if (Distance(expected[first], expected[second]) < 10)
                    yield return $"the faces of the notch with the normals {NotchFaces[first].Normal} and {NotchFaces[second].Normal} have the same shade from this camera, so the check cannot tell them apart";
            }
        }

        for (var index = 0; index < NotchFaces.Length; index++)
        {
            var pixel = ToPixel(NotchFaces[index].Point, camera);
            var (column, row) = ((int)pixel.X, (int)pixel.Y);
            var actual = Colour(pixels, column, row);
            if (Distance(actual, expected[index]) > 3)
                yield return $"the centre of the face of the notch with the normal {NotchFaces[index].Normal}, at the pixel ({column}, {row}), has the colour {actual} and not {expected[index]}";
        }
    }

    // The colour that mesh.frag gives to a face with this normal in the space of the scene.
    private static (int R, int G, int B) ExpectedShade(Vector3 normal, Camera camera)
    {
        var viewNormal = Vector3.Normalize(Vector3.TransformNormal(normal, camera.View()));
        var colour = BaseColour * (0.35f + 0.65f * Math.Abs(Vector3.Dot(viewNormal, Light)));
        return ((int)MathF.Round(colour.X * 255), (int)MathF.Round(colour.Y * 255), (int)MathF.Round(colour.Z * 255));
    }

    // A point of the scene in pixels of the picture: x to the right, y down from the top
    // row. The renderer works relative to the eye, and the viewport has y up (ADR-0022).
    private static Vector2 ToPixel((double X, double Y, double Z) point, Camera camera)
    {
        var eye = camera.Eye();
        var relative = new Vector4((float)(point.X - eye.X), (float)(point.Y - eye.Y), (float)(point.Z - eye.Z), 1f);
        var clip = Vector4.Transform(relative, camera.View() * camera.Projection((float)Width / Height));
        var (ndcX, ndcY) = (clip.X / clip.W, clip.Y / clip.W);
        return new Vector2((ndcX + 1f) / 2f * Width, (1f - ndcY) / 2f * Height);
    }

    private static (double X, double Y, double Z) Vertex(Tessellation tessellation, uint index)
        => (tessellation.Positions[index * 3], tessellation.Positions[index * 3 + 1], tessellation.Positions[index * 3 + 2]);

    // A point is inside a triangle of either orientation when it is on one side of
    // each of its three edges.
    private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        static float Side(Vector2 p, Vector2 a, Vector2 b) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        var (ab, bc, ca) = (Side(p, a, b), Side(p, b, c), Side(p, c, a));
        return (ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0);
    }

    private static bool IsBackground(byte[] pixels, int column, int row)
    {
        var background = SceneRenderer.Background;
        return Colour(pixels, column, row) == ((int)(background.R * 255), (int)(background.G * 255), (int)(background.B * 255));
    }

    private static (int R, int G, int B) Colour(byte[] pixels, int column, int row)
    {
        var offset = (row * (int)Width + column) * 4;
        return (pixels[offset], pixels[offset + 1], pixels[offset + 2]);
    }

    private static int Distance((int R, int G, int B) a, (int R, int G, int B) b)
        => Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));

    // The picture goes to the folder of IMAGE_TEST_OUTPUT, which the pipeline keeps when
    // the test fails, or to the folder of the test.
    private static string SavePicture(byte[] pixels)
    {
        var folder = Environment.GetEnvironmentVariable("IMAGE_TEST_OUTPUT") is { Length: > 0 } output
            ? output
            : Path.Combine(AppContext.BaseDirectory, "pictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "first-demonstration.png");
        Png.Write(path, (int)Width, (int)Height, pixels);
        return path;
    }
}
