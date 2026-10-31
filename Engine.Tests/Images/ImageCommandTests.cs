using Engine.Contracts;
using Engine.Contracts.Images;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;

namespace Engine.Tests.Images;

// An image on a datum and its calibration (ADR-0045, TASK-0152). ADR-0045 names this file in
// enforced-by. The record, the two commands, each refusal, the events and their order, undo and
// redo, the document file, and the rule of the bus for a handler that changes an image.
public sealed class ImageCommandTests : IDisposable
{
    private static readonly string Asset = new('c', 64);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-image-" + Guid.NewGuid().ToString("N"));

    public ImageCommandTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        return (new DocumentSession(kit), kit.Events);
    }

    private static PlaceImageCommand Place() => new()
    {
        Asset = Asset, DatumId = Datums.XZ.Id, Width = 640, Height = 480, OriginU = -20, OriginV = 5.5,
    };

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : Guid.Empty;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Task<IReadOnlyList<ImageRecord>> Images(DocumentSession session)
        => session.Read<IReadOnlyList<ImageRecord>>((document, _) => document.Images.ToList());

    // ADR-0045 items 1, 2 and 4: the record holds each input, the identifier of the command, a
    // scale of 1 by default, and the event carries the record.
    [Fact]
    public async Task A_Placed_Image_Has_Its_Record_And_Its_Event()
    {
        var (session, events) = NewSession();
        var place = Place();
        var id = await Applied(session, place, "imageId");

        var image = Assert.Single(await Images(session));
        var created = events.Snapshot().Single(record => record.Kind == "image.created");
        var payload = (IReadOnlyDictionary<string, object?>)created.Payload!;

        Assert.Equal(place.CommandId, id);
        Assert.Equal(new ImageRecord(id, Asset, Datums.XZ.Id, -20, 5.5, 640, 480, 1), image);
        Assert.Equal(
            [id, Asset, Datums.XZ.Id, -20.0, 5.5, 640, 480, 1.0],
            new[] { "imageId", "asset", "datumId", "originU", "originV", "width", "height", "scale" }.Select(key => payload[key]));
    }

    // ADR-0045 item 5: the scale is the distance over the pixels between the two points, and the
    // origin, the asset and the datum stay.
    [Fact]
    public async Task A_Calibration_Gives_The_Distance_Over_The_Pixels_And_Keeps_The_Origin()
    {
        var (session, events) = NewSession();
        var id = await Applied(session, Place(), "imageId");

        var result = await session.Apply(new CalibrateImageCommand { ImageId = id, X1 = 10, Y1 = 20, X2 = 310, Y2 = 420, Distance = 50 });

        var image = Assert.Single(await Images(session));
        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.True(result.Outputs.TryGet<double>("scale", out var scale));
        Assert.Equal(0.1, scale);
        Assert.Equal(new ImageRecord(id, Asset, Datums.XZ.Id, -20, 5.5, 640, 480, 0.1), image);
        var changed = (IReadOnlyDictionary<string, object?>)events.Snapshot().Single(record => record.Kind == "image.changed").Payload!;
        Assert.Equal(0.1, changed["scale"]);
    }

    // ADR-0045 item 6: each refusal gives its code, and the Document holds no image.
    [Fact]
    public async Task Each_Wrong_Place_Gives_Its_Code()
    {
        var (session, _) = NewSession();

        Assert.Equal(DiagnosticCodes.ImageAssetInvalid, await Refused(session, Place() with { Asset = Asset.ToUpperInvariant() }));
        Assert.Equal(DiagnosticCodes.ImageAssetInvalid, await Refused(session, Place() with { Asset = Asset + "\n" }));
        Assert.Equal(DiagnosticCodes.ImageAssetInvalid, await Refused(session, Place() with { Asset = Asset[1..] }));
        Assert.Equal(DiagnosticCodes.ImageDatumNotFound, await Refused(session, Place() with { DatumId = Guid.NewGuid() }));
        Assert.Equal(DiagnosticCodes.ImageSizeInvalid, await Refused(session, Place() with { Width = 0 }));
        Assert.Equal(DiagnosticCodes.ImageSizeInvalid, await Refused(session, Place() with { Height = -1 }));
        Assert.Equal(DiagnosticCodes.ImageSizeInvalid, await Refused(session, Place() with { Width = (long)int.MaxValue + 1 }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, Place() with { OriginU = double.NaN }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, Place() with { OriginV = double.PositiveInfinity }));
        Assert.Equal(DiagnosticCodes.ImageScaleInvalid, await Refused(session, Place() with { Scale = 0 }));
        Assert.Equal(DiagnosticCodes.ImageScaleInvalid, await Refused(session, Place() with { Scale = -2 }));
        Assert.Equal(DiagnosticCodes.ImageScaleInvalid, await Refused(session, Place() with { Scale = double.NaN }));
        Assert.Empty(await Images(session));
    }

    [Fact]
    public async Task Each_Wrong_Calibration_Gives_Its_Code_And_The_Scale_Stays()
    {
        var (session, _) = NewSession();
        var id = await Applied(session, Place() with { Scale = 0.5 }, "imageId");
        CalibrateImageCommand Calibrate() => new() { ImageId = id, X1 = 0, Y1 = 0, X2 = 3, Y2 = 4, Distance = 10 };

        Assert.Equal(DiagnosticCodes.ImageNotFound, await Refused(session, Calibrate() with { ImageId = Guid.NewGuid() }));
        Assert.Equal(DiagnosticCodes.ImageCalibrationInvalid, await Refused(session, Calibrate() with { X2 = 0, Y2 = 0 }));
        Assert.Equal(DiagnosticCodes.ImageCalibrationInvalid, await Refused(session, Calibrate() with { Distance = 0 }));
        Assert.Equal(DiagnosticCodes.ImageCalibrationInvalid, await Refused(session, Calibrate() with { Distance = -10 }));
        Assert.Equal(DiagnosticCodes.ImageCalibrationInvalid, await Refused(session, Calibrate() with { Distance = double.NaN }));
        Assert.Equal(DiagnosticCodes.ImageCalibrationInvalid, await Refused(session, Calibrate() with { X1 = double.NaN }));
        Assert.Equal(DiagnosticCodes.ImageCalibrationInvalid, await Refused(session, Calibrate() with { X2 = double.MaxValue, X1 = -double.MaxValue }));
        Assert.Equal(DiagnosticCodes.ImageScaleInvalid, await Refused(session, Calibrate() with { Distance = double.Epsilon, X2 = 3e100, Y2 = 4e100 }));
        Assert.Equal(0.5, Assert.Single(await Images(session)).Scale);
    }

    // ADR-0045 items 8 and 9: the events of each commit, and an undo and a redo of each step.
    [Fact]
    public async Task Undo_And_Redo_Give_The_Image_Before_And_After_Each_Step()
    {
        var (session, events) = NewSession();

        async Task<List<EventRecord>> Events(Command command)
        {
            var before = events.Snapshot().Count;
            var result = await session.Apply(command);
            Assert.Equal(CommandStatus.Applied, result.Status);
            return events.Snapshot().Skip(before).ToList();
        }

        var placed = await Events(Place());
        var id = (Guid)((IReadOnlyDictionary<string, object?>)placed[1].Payload!)["imageId"]!;
        var calibrated = await Events(new CalibrateImageCommand { ImageId = id, X1 = 0, Y1 = 0, X2 = 0, Y2 = 200, Distance = 50 });
        var undone = await Events(new UndoCommand());
        var afterUndo = Assert.Single(await Images(session));
        var undoneTwice = await Events(new UndoCommand());
        var afterSecondUndo = await Images(session);
        var redone = await Events(new RedoCommand());
        var redoneTwice = await Events(new RedoCommand());

        Assert.Equal(["command.applied", "image.created"], placed.Select(record => record.Kind));
        Assert.Equal(["command.applied", "image.changed"], calibrated.Select(record => record.Kind));
        Assert.Equal(["command.applied", "image.changed"], undone.Select(record => record.Kind));
        Assert.Equal(1.0, afterUndo.Scale);
        Assert.Equal(["command.applied", "image.removed"], undoneTwice.Select(record => record.Kind));
        Assert.Empty(afterSecondUndo);
        Assert.Equal(["command.applied", "image.created"], redone.Select(record => record.Kind));
        Assert.Equal(["command.applied", "image.changed"], redoneTwice.Select(record => record.Kind));
        Assert.Equal(0.25, Assert.Single(await Images(session)).Scale);
    }

    // ADR-0045 item 4: the document file holds the two commands, and an open gives the same
    // record with no read of the store.
    [Fact]
    public async Task An_Open_Of_The_File_Gives_The_Same_Image()
    {
        var path = Path.Combine(_folder, "image" + DocumentFile.Extension);
        var (live, _) = NewSession();
        DocumentFile.Create(live, path);
        var id = await Applied(live, Place(), "imageId");
        await Applied(live, new CalibrateImageCommand { ImageId = id, X1 = 1, Y1 = 1, X2 = 4, Y2 = 5, Distance = 7.5 }, "imageId");
        var expected = Assert.Single(await Images(live));
        live.Close();

        var (opened, _) = NewSession();
        await DocumentFile.Open(opened, path);
        var actual = Assert.Single(await Images(opened));
        opened.Close();

        Assert.Equal(expected, actual);
        Assert.Equal(1.5, actual.Scale);
    }

    // ADR-0045 item 6: a command sent again after the idempotency cache forgot it is refused, as
    // E86 and E95 give the commands of a sketch, and the Document keeps one image.
    [Fact]
    public async Task A_Command_Sent_Again_After_The_Cache_Forgot_It_Is_Refused()
    {
        var (session, _) = NewSession();
        var place = Place();
        var id = await Applied(session, place, "imageId");
        var calibrate = new CalibrateImageCommand { ImageId = id, X1 = 0, Y1 = 0, X2 = 10, Y2 = 0, Distance = 5 };
        await Applied(session, calibrate, "imageId");
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand());

        Assert.Equal(DiagnosticCodes.ImageIdInUse, await Refused(session, place));
        Assert.Equal(DiagnosticCodes.ImageIdInUse, await Refused(session, calibrate));
        Assert.Equal(0.5, Assert.Single(await Images(session)).Scale);
    }

    // ADR-0045 item 7: a handler keeps the asset and the datum of an image, so a handler that
    // changes one is a defect, and the bus throws before the commit.
    [Fact]
    public async Task The_Bus_Refuses_A_Handler_That_Changes_The_Asset_Or_The_Datum_Of_An_Image()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        kit.CommandRegistry.Register(new ImageChangingHandler());
        var session = new DocumentSession(kit);
        var id = await Applied(session, Place(), "imageId");

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.Apply(new ImageChange { ImageId = id, Asset = true }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.Apply(new ImageChange { ImageId = id, Asset = false }));

        Assert.Equal(new ImageRecord(id, Asset, Datums.XZ.Id, -20, 5.5, 640, 480, 1), Assert.Single(await Images(session)));
    }

    private sealed record ImageChange : Command
    {
        public override string Name => "ProbeImageChange";
        public override int SchemaVersion => 1;
        public required Guid ImageId { get; init; }
        public required bool Asset { get; init; }
    }

    private sealed class ImageChangingHandler : Engine.Contracts.Handlers.ICommandHandler
    {
        public string CommandName => "ProbeImageChange";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, Engine.Contracts.Schema.FieldSchema> Parameters { get; } = new Dictionary<string, Engine.Contracts.Schema.FieldSchema>();
        public IReadOnlyDictionary<string, Engine.Contracts.Schema.FieldSchema> Outputs { get; } = new Dictionary<string, Engine.Contracts.Schema.FieldSchema>();
        public Command Create(Engine.Contracts.Handlers.CommandInput input) => throw new NotSupportedException();

        public Task<Engine.Contracts.Handlers.CommandHandlerResult> Handle(Command command, Document document, Engine.Contracts.Geometry.IGeometryBackend backend, CancellationToken ct)
        {
            var change = (ImageChange)command;
            var image = document.Images.Single(candidate => candidate.Id == change.ImageId);
            var changed = change.Asset ? image with { Asset = new string('d', 64) } : image with { DatumId = Datums.YZ.Id };
            return Task.FromResult(Engine.Contracts.Handlers.CommandHandlerResult.Success(Engine.Contracts.Outputs.Empty, changedImages: [changed]));
        }
    }
}
