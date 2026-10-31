using System.Collections.Concurrent;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Images;
using Engine.Core.Assets;
using ThreeDEngine.Core.Imaging;
using ThreeDEngine.Core.Rendering;

namespace ThreeDEngine.Desktop;

// ADR-0046 item 2: the projection of the live images of the session into the desktop. As
// SessionSketches does, it reads the live images and subscribes to the events of the session in
// one read, so it misses no image. The event of an image carries its record (ADR-0045 item 8), so
// the action of the subscription only puts the record in a queue, inside the commit. The render
// loop calls Apply, which reads the bytes of each new asset from the store, decodes them and gives
// them to the renderer one time.
internal sealed class SessionImages : IDisposable
{
    private readonly AssetStore? _store;
    private readonly ConcurrentQueue<(Guid ImageId, ImageRecord? Record)> _changes = new();
    private readonly IDisposable _subscription;
    private readonly Dictionary<Guid, ImageRecord> _live = new();
    private readonly HashSet<string> _uploaded = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failed = new(StringComparer.Ordinal);

    public SessionImages(EngineHost host, AssetStore? store)
    {
        _store = store;
        _subscription = host.Session.Read((document, _) =>
        {
            foreach (var image in document.Images)
                _changes.Enqueue((image.Id, image));
            return host.Subscribe(OnEvent);
        }).GetAwaiter().GetResult();
    }

    // Inside the commit: no call of the session, and no wait.
    private void OnEvent(EventRecord record)
    {
        if (record.Payload is not IReadOnlyDictionary<string, object?> payload || payload.GetValueOrDefault("imageId") is not Guid id)
            return;

        switch (record.Kind)
        {
            case "image.created":
            case "image.changed":
                _changes.Enqueue((id, new ImageRecord(
                    id,
                    (string)payload["asset"]!,
                    (Guid)payload["datumId"]!,
                    (double)payload["originU"]!,
                    (double)payload["originV"]!,
                    (int)payload["width"]!,
                    (int)payload["height"]!,
                    (double)payload["scale"]!)));
                break;
            case "image.removed":
                _changes.Enqueue((id, null));
                break;
        }
    }

    // Each live image, as the last call of Apply left them.
    public IReadOnlyCollection<ImageRecord> Live => _live.Values;

    // The quad of each live image whose asset the renderer holds.
    public IReadOnlyList<ImageQuad> Quads()
        => _live.Values.Where(image => _uploaded.Contains(image.Asset))
            .Select(ImagePlacement.Quad).OfType<ImageQuad>().ToList();

    // On the render loop. An asset that the store does not give, or whose bytes are not a PNG file
    // that the reader takes, is written as a warning one time, and its image is not drawn.
    public void Apply(Action<string, PngPicture> upload)
    {
        while (_changes.TryDequeue(out var change))
        {
            if (change.Record is { } image)
                _live[change.ImageId] = image;
            else
                _live.Remove(change.ImageId);
        }

        foreach (var asset in _live.Values.Select(image => image.Asset).Distinct().ToList())
        {
            if (_uploaded.Contains(asset) || _failed.Contains(asset))
                continue;
            if (Picture(asset) is { } picture)
            {
                upload(asset, picture);
                _uploaded.Add(asset);
            }
            else
                _failed.Add(asset);
        }
    }

    private PngPicture? Picture(string asset)
    {
        if (_store is null)
        {
            Console.WriteLine($"[WARN] The image of asset {asset} is not drawn: the desktop has no store of assets.");
            return null;
        }

        var (content, error) = _store.Read(asset).GetAwaiter().GetResult();
        if (content is null)
        {
            Console.WriteLine($"[WARN] The image of asset {asset} is not drawn: {error?.Code} {error?.Message}");
            return null;
        }

        try
        {
            return PngReader.Read(content);
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"[WARN] The image of asset {asset} is not drawn: {ex.Message}");
            return null;
        }
    }

    public void Dispose() => _subscription.Dispose();
}
