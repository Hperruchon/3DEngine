using Engine.Api.Http;
using Engine.Contracts.Images;
using Engine.Contracts.Sketch;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0047 items 3 and 4 (phase V3, TASK-0155), with no GPU: the desktop proposes the contour of a placed
// image, the person accepts it, and the sketch commands that exist record its points on the datum of
// the image, so the sketch is a sketch like each other one. ADR-0047 names this file in enforced-by.
public sealed class ContourAcceptTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-contour-" + Guid.NewGuid().ToString("N"));

    public ContourAcceptTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A light picture of 100 by 50 pixels with a dark part from (20, 10) to (79, 39).
    private string Part(byte region = 30)
    {
        var rgba = new byte[100 * 50 * 4];
        for (var y = 0; y < 50; y++)
        {
            for (var x = 0; x < 100; x++)
            {
                var value = x is >= 20 and <= 79 && y is >= 10 and <= 39 ? region : (byte)220;
                rgba.AsSpan(((y * 100) + x) * 4, 3).Fill(value);
                rgba[(((y * 100) + x) * 4) + 3] = 255;
            }
        }

        var path = Path.Combine(_folder, "part.png");
        Png.Write(path, 100, 50, rgba);
        return path;
    }

    private static async Task<ImageRecord> Placed(EngineHost host, ImageActions actions, string file)
    {
        actions.Place(file, Datums.XZ.Id);
        var image = Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
        Assert.Equal(Engine.Contracts.CommandStatus.Applied,
            (await host.Session.Apply(new CalibrateImageCommand { ImageId = image.Id, X1 = 0, Y1 = 0, X2 = 100, Y2 = 0, Distance = 50 })).Status);
        return Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
    }

    // ADR-0047 item 4: the pixel (x, y) of the image is at (U, V) = (OriginU + x Scale, OriginV + (Height - y)
    // Scale) on its datum, the inverse of the pixel of a tap.
    [Fact]
    public void The_Point_Of_A_Pixel_Is_The_Inverse_Of_The_Pixel_Of_A_Point()
    {
        var image = new ImageRecord(Guid.NewGuid(), new string('a', 64), Datums.XY.Id, -20, 5, 640, 480, 0.5);

        Assert.Equal((-20.0, 245.0), ImagePlacement.At(image, (0, 0)));
        Assert.Equal((300.0, 5.0), ImagePlacement.At(image, (640, 480)));
        Assert.Equal((123.5, 77.25), ImagePlacement.Pixel(image, ImagePlacement.At(image, (123.5, 77.25))));
    }

    // ADR-0047 items 3 and 4: the proposal has the four corners of the part, and its accept makes a sketch on
    // the datum of the image with a point at each corner and a line along each side.
    [Fact]
    public async Task An_Accepted_Proposal_Is_A_Sketch_On_The_Datum_Of_The_Image()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var actions = new ImageActions(host.Session, new AssetStore(Path.Combine(_folder, "store")));
        var image = await Placed(host, actions, Part());

        actions.Propose(image.Id, [image]);
        var proposal = actions.Proposal;
        var accepted = actions.Accept([image]);

        Assert.Equal([(20.5, 10.5), (79.5, 10.5), (79.5, 39.5), (20.5, 39.5)], proposal?.Pixels);
        Assert.NotNull(accepted);
        Assert.Equal(Datums.XZ.Id, accepted.Value.Datum);
        var sketch = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = accepted.Value.Sketch })).Result!;
        Assert.Equal(Datums.XZ.Id, sketch.DatumId);
        // The origin of the frame, then the four corners at 0.5 mm for each pixel.
        Assert.Equal([(10.25, 19.75), (39.75, 19.75), (39.75, 5.25), (10.25, 5.25)], sketch.Points.Skip(1).Select(point => (point.X, point.Y)));
        Assert.Equal(4, sketch.Lines.Count);
        Assert.Null(actions.Proposal);
        Assert.Equal("Accepted: a sketch of 4 points and 4 lines", actions.Message);
    }

    // ADR-0047 item 3: a reject drops the proposal and sends no command; an image of one grey gives no
    // proposal and a reason; an accept with no proposal sends no command.
    [Fact]
    public async Task A_Reject_And_An_Image_With_No_Region_Send_No_Command()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var actions = new ImageActions(host.Session, new AssetStore(Path.Combine(_folder, "store")));
        var image = await Placed(host, actions, Part());
        var before = await host.Session.Read((document, _) => document.Version);

        actions.Propose(image.Id, [image]);
        actions.Reject();
        var rejected = actions.Proposal;
        var none = actions.Accept([image]);
        var noProposal = actions.Message;

        var flat = Path.Combine(_folder, "flat.png");
        Png.Write(flat, 10, 10, Enumerable.Repeat((byte)128, 400).ToArray());
        actions.Place(flat, Datums.XY.Id);
        var flatImage = (await host.Session.Read((document, _) => document.Images.ToList())).Single(candidate => candidate.Id != image.Id);
        var placedFlat = await host.Session.Read((document, _) => document.Version);
        actions.Propose(flatImage.Id, [image, flatImage]);

        Assert.Null(rejected);
        Assert.Null(none);
        Assert.Equal("No proposal to accept.", noProposal);
        Assert.Equal(before + 1, placedFlat);
        Assert.Null(actions.Proposal);
        Assert.Equal("The image has no region that gives a contour.", actions.Message);
        Assert.Empty(await host.Session.Read((document, _) => document.Sketches.ToList()));
    }
}
