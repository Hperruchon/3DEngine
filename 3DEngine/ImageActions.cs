using Engine.Contracts;
using Engine.Contracts.Images;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using ThreeDEngine.Core.Imaging;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;

namespace ThreeDEngine.Desktop;

// ADR-0046 items 3 and 4: the commands of the window of the images. A place reads a PNG file,
// decodes it for its width and its height, puts its bytes in the store, and sends PlaceImage with
// the name that the store gives. A calibration takes two taps on the image, each as a pixel of the
// image, and then a real distance, and sends CalibrateImage. The desktop changes nothing itself.
internal sealed class ImageActions(DocumentSession session, AssetStore? store)
{
    private readonly List<(double X, double Y)> _points = [];

    // The image of the calibration, or null.
    public Guid? Calibrating { get; private set; }

    // The pixels of the taps of the calibration, at most two.
    public IReadOnlyList<(double X, double Y)> Points => _points;

    // The result of the last action, or null.
    public string? Message { get; private set; }

    public void Place(string path, Guid datum)
    {
        Message = PlaceFile(path.Trim().Trim('"'), datum);
    }

    private string PlaceFile(string path, Guid datum)
    {
        if (store is null)
            return "The desktop has no store of assets.";
        if (path.Length == 0 || !File.Exists(path))
            return $"No file '{path}' exists.";
        if (new FileInfo(path).Length > AssetStore.MaximumBytes)
            return $"The file is larger than {AssetStore.MaximumBytes} bytes, the limit of an asset (ADR-0044 item 3).";

        byte[] bytes;
        PngPicture picture;
        try
        {
            bytes = File.ReadAllBytes(path);
            picture = PngReader.Read(bytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return ex.Message;
        }

        var (name, _, error) = store.Put(new MemoryStream(bytes)).GetAwaiter().GetResult();
        if (name is null)
            return $"{error?.Code}: {error?.Message}";

        var result = session.Apply(new PlaceImageCommand { Asset = name, DatumId = datum, Width = picture.Width, Height = picture.Height })
            .GetAwaiter().GetResult();
        return result.Status == CommandStatus.Applied
            ? $"Placed {Path.GetFileName(path)}, {picture.Width} by {picture.Height} pixels, on {Datums.Find(datum)?.Name}"
            : $"{result.Error?.Code}: {result.Error?.Message}";
    }

    public void StartCalibration(Guid image)
    {
        (Calibrating, Message) = (image, null);
        _points.Clear();
    }

    public void Cancel()
    {
        (Calibrating, Message) = (null, null);
        _points.Clear();
    }

    // A tap of the scene during a calibration: the pixel of the image under it, on the plane of
    // the datum of the image. True when the calibration takes the tap, so the selection does not.
    public bool Tap(Camera camera, double x, double y, double width, double height, IReadOnlyCollection<ImageRecord> live)
    {
        if (Calibrating is not { } id)
            return false;
        if (live.FirstOrDefault(image => image.Id == id) is not { } image || Datums.Find(image.DatumId) is not { } datum)
        {
            Cancel();
            return false;
        }

        if (_points.Count == 2 || width <= 0 || height <= 0)
            return true;
        var (u, v) = ((datum.U.X, datum.U.Y, datum.U.Z), (datum.V.X, datum.V.Y, datum.V.Z));
        if (Picker.OnPlane(Picker.RayThrough(camera, x, y, width, height), (0.0, 0.0, 0.0), u, v) is { } at)
            _points.Add(ImagePlacement.Pixel(image, at));
        return true;
    }

    public void Calibrate(double distance)
    {
        if (Calibrating is not { } id || _points.Count != 2)
        {
            Message = "A calibration needs two taps on the image.";
            return;
        }

        var result = session.Apply(new CalibrateImageCommand
        {
            ImageId = id, X1 = _points[0].X, Y1 = _points[0].Y, X2 = _points[1].X, Y2 = _points[1].Y, Distance = distance,
        }).GetAwaiter().GetResult();
        if (result.Status != CommandStatus.Applied)
        {
            Message = $"{result.Error?.Code}: {result.Error?.Message}";
            return;
        }

        Cancel();
        Message = result.Outputs.TryGet<double>("scale", out var scale)
            ? $"Calibrated: {scale.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)} mm for each pixel"
            : "Calibrated";
    }
}
