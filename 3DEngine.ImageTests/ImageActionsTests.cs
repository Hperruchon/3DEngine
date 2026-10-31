using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Images;
using Engine.Contracts.Sketch;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0046 items 2 to 4 (TASK-0153), with no GPU: the quad and the pixel of an image on its datum,
// the place of a file, each refusal of a place, the taps of a calibration, and the projection of
// the session with one upload for each asset.
public sealed class ImageActionsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-image-actions-" + Guid.NewGuid().ToString("N"));

    public ImageActionsTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Picture(string name, int width, int height)
    {
        var path = Path.Combine(_folder, name);
        Png.Write(path, width, height, new byte[width * height * 4]);
        return path;
    }

    // ADR-0045 item 2: the lower-left corner at the origin, the width along U and the height along V.
    [Fact]
    public void The_Quad_Of_An_Image_Follows_Its_Datum_And_Its_Scale()
    {
        var image = new ImageRecord(Guid.NewGuid(), new string('a', 64), Datums.XZ.Id, -20, 5, 640, 480, 0.5);

        var quad = ImagePlacement.Quad(image)!;

        Assert.Equal(image.Asset, quad.ImageId);
        Assert.Equal((-20.0, 0.0, 5.0), quad.Origin);
        Assert.Equal((320.0, 0.0, 0.0), quad.Right);
        Assert.Equal((0.0, 0.0, 240.0), quad.Up);
        Assert.Equal((0.0, 480.0), ImagePlacement.Pixel(image, (-20, 5)));
        Assert.Equal((640.0, 0.0), ImagePlacement.Pixel(image, (300, 245)));
        Assert.Null(ImagePlacement.Quad(image with { DatumId = Guid.NewGuid() }));
    }

    // ADR-0046 item 3: a place puts the bytes in the store and gives the size of the file to the
    // command, and each wrong file gives its reason and no image.
    [Fact]
    public async Task A_Place_Stores_The_File_And_Each_Wrong_File_Gives_Its_Reason()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var store = new AssetStore(Path.Combine(_folder, "store"));
        var actions = new ImageActions(host.Session, store);
        var file = Picture("plan.png", 30, 20);

        actions.Place($"\"{file}\"", Datums.YZ.Id);
        var placed = actions.Message;
        var images = await host.Session.Read((document, _) => document.Images.ToList());
        actions.Place(Path.Combine(_folder, "absent.png"), Datums.XY.Id);
        var absent = actions.Message;
        File.WriteAllText(Path.Combine(_folder, "text.png"), "not a picture");
        actions.Place(Path.Combine(_folder, "text.png"), Datums.XY.Id);
        var text = actions.Message;
        actions.Place(Picture("wide.png", 4097, 1), Datums.XY.Id);
        var wide = actions.Message;
        new ImageActions(host.Session, null).Place(file, Datums.XY.Id);

        var image = Assert.Single(images);
        Assert.Equal(new ImageRecord(image.Id, AssetStore.NameOf(File.ReadAllBytes(file)), Datums.YZ.Id, 0, 0, 30, 20, 1), image);
        Assert.Equal(File.ReadAllBytes(file), (await store.Read(image.Asset)).Content);
        Assert.Equal("Placed plan.png, 30 by 20 pixels, on YZ", placed);
        Assert.StartsWith("No file", absent);
        Assert.Contains("not a PNG file", text);
        Assert.Contains("from 1 to 4096", wide);
        Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
    }

    // ADR-0046 item 4: during a calibration a tap is a pixel of the image, a third tap is kept out,
    // and a tap with no calibration goes to the selection.
    [Fact]
    public async Task Two_Taps_And_A_Distance_Calibrate_The_Image()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var actions = new ImageActions(host.Session, new AssetStore(Path.Combine(_folder, "store")));
        actions.Place(Picture("plan.png", 100, 50), Datums.XY.Id);
        var image = Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
        var camera = new Camera { Target = (50, 25, 0), Distance = 200, YawDegrees = 0, PitchDegrees = 0 };

        var free = actions.Tap(camera, 100, 100, 800, 600, [image]);
        actions.StartCalibration(image.Id);
        var first = actions.Tap(camera, 300, 300, 800, 600, [image]);
        var second = actions.Tap(camera, 500, 300, 800, 600, [image]);
        var third = actions.Tap(camera, 600, 100, 800, 600, [image]);
        var points = actions.Points.ToList();
        actions.Calibrate(40);

        Assert.False(free);
        Assert.True(first && second && third);
        Assert.Equal(2, points.Count);
        Assert.Equal(points[0].Y, points[1].Y, 9);
        var expected = 40 / Math.Abs(points[1].X - points[0].X);
        var calibrated = Assert.Single(await host.Session.Read((document, _) => document.Images.ToList()));
        Assert.Equal(expected, calibrated.Scale, 12);
        Assert.Null(actions.Calibrating);
        Assert.StartsWith("Calibrated: ", actions.Message);

        actions.Calibrate(40);
        Assert.Equal("A calibration needs two taps on the image.", actions.Message);
    }

    // ADR-0046 item 2: the projection reads the live images and each event, uploads each asset one
    // time, gives a quad only for an asset that it uploaded, and warns one time for an asset that
    // is not a PNG file.
    [Fact]
    public async Task The_Projection_Uploads_Each_Asset_One_Time_And_Follows_Undo()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        var store = new AssetStore(Path.Combine(_folder, "store"));
        var actions = new ImageActions(host.Session, store);
        actions.Place(Picture("plan.png", 8, 8), Datums.XY.Id);
        using var images = new SessionImages(host, store);
        actions.Place(Picture("plan.png", 8, 8), Datums.XZ.Id);
        var (text, _, _) = await store.Put(new MemoryStream("not a picture"u8.ToArray()));
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(new PlaceImageCommand { Asset = text!, DatumId = Datums.YZ.Id, Width = 2, Height = 2 })).Status);

        var uploads = new List<string>();
        images.Apply((asset, _) => uploads.Add(asset));
        images.Apply((asset, _) => uploads.Add(asset));
        var live = images.Live.Count;
        var quads = images.Quads();
        await host.Session.Apply(new UndoCommand());
        await host.Session.Apply(new UndoCommand());
        images.Apply((asset, _) => uploads.Add(asset));

        Assert.Equal(3, live);
        Assert.Single(uploads);
        Assert.Equal(2, quads.Count);
        Assert.All(quads, quad => Assert.Equal(uploads[0], quad.ImageId));
        Assert.Single(images.Live);
        Assert.Single(images.Quads());
    }
}
