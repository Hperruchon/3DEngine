using System.Net;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Assets;

// ADR-0044 (phase V1b, TASK-0147): the store of the assets, its two routes and its verb of the command
// line. Each test works in its own folder of scratch files, and removes it.
public sealed class AssetStoreTests : IDisposable
{
    // The SHA-256 of the three bytes "abc" (FIPS 180-2, appendix B.1).
    private const string Abc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private readonly string _folder = Directory.CreateTempSubdirectory("3de-assets-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Store => Path.Combine(_folder, "store");

    // ADR-0044 item 1: the name is the SHA-256 of the bytes, and a second put of the same bytes gives
    // the same name and one file, with no scratch file.
    [Fact]
    public async Task The_Name_Of_An_Asset_Is_The_Sha256_Of_Its_Bytes()
    {
        var store = new AssetStore(Store);

        var first = await store.Put(new MemoryStream("abc"u8.ToArray()));
        var second = await store.Put(new MemoryStream("abc"u8.ToArray()));
        var (content, error) = await store.Read(Abc);

        Assert.Equal((Abc, 3L, (Engine.Contracts.ErrorDetail?)null), first);
        Assert.Equal(first, second);
        Assert.Null(error);
        Assert.Equal("abc"u8.ToArray(), content);
        Assert.Equal([Abc], Directory.GetFiles(Store).Select(Path.GetFileName));
    }

    // ADR-0044 item 2: a name that is not 64 lower-case hexadecimal digits, a name that the store does
    // not hold, and a file whose bytes changed each give their code and no bytes.
    [Fact]
    public async Task A_Read_Refuses_A_Bad_Name_An_Absent_Asset_And_A_Changed_File()
    {
        var store = new AssetStore(Store);
        await store.Put(new MemoryStream("abc"u8.ToArray()));

        var upper = await store.Read(Abc.ToUpperInvariant());
        var path = await store.Read("../" + Abc[3..]);
        // Finding E113 of the codebase review of 2026-10-31 (c): a final line feed is not a name.
        var lineFeed = await store.Read(Abc + "\n");
        var absent = await store.Read(new string('0', 64));
        File.WriteAllBytes(Path.Combine(Store, Abc), "abd"u8.ToArray());
        var changed = await store.Read(Abc);

        Assert.Equal(DiagnosticCodes.AssetBadName, upper.Error?.Code);
        Assert.Equal(DiagnosticCodes.AssetBadName, path.Error?.Code);
        Assert.Equal(DiagnosticCodes.AssetBadName, lineFeed.Error?.Code);
        Assert.False(AssetStore.IsName(Abc + "\n"));
        Assert.Equal(DiagnosticCodes.AssetNotFound, absent.Error?.Code);
        Assert.Equal(DiagnosticCodes.AssetCorrupt, changed.Error?.Code);
        Assert.All(new[] { upper.Content, path.Content, lineFeed.Content, absent.Content, changed.Content }, Assert.Null);
    }

    // ADR-0044 item 3: an asset of 16 MiB is stored, and one byte more is refused with nothing kept.
    [Fact]
    public async Task An_Asset_Over_16_MiB_Is_Refused()
    {
        var store = new AssetStore(Store);

        var largest = await store.Put(new MemoryStream(new byte[AssetStore.MaximumBytes]));
        var larger = await store.Put(new MemoryStream(new byte[AssetStore.MaximumBytes + 1]));

        Assert.Null(largest.Error);
        Assert.Equal(DiagnosticCodes.AssetTooLarge, larger.Error?.Code);
        Assert.Null(larger.Name);
        Assert.Single(Directory.GetFiles(Store));
    }

    // ADR-0044 item 2: a folder that the store cannot make gives E-ASSET-WRITE-FAILED.
    [Fact]
    public async Task A_Store_That_Cannot_Write_Gives_Its_Code()
    {
        File.WriteAllText(Store, "a file where the folder must be");
        var store = new AssetStore(Store);

        var put = await store.Put(new MemoryStream("abc"u8.ToArray()));

        Assert.Equal(DiagnosticCodes.AssetWriteFailed, put.Error?.Code);
        Assert.Null(put.Name);
    }

    // ADR-0044 item 4: PUT /assets gives the name, GET /assets/{name} gives the bytes, and each error
    // has its status.
    [Fact]
    public async Task The_Routes_Put_And_Get_An_Asset()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new AssetStore(Store));
        builder.AddEngineSurface(_ => new EngineHost(new InProcessMeshBackend()));
        await using var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        using var put = await http.PutAsync("/assets", new ByteArrayContent("abc"u8.ToArray()));
        var state = JsonDocument.Parse(await put.Content.ReadAsStringAsync()).RootElement;
        var bytes = await http.GetByteArrayAsync($"/assets/{Abc}");
        using var bad = await http.GetAsync("/assets/ABC");
        using var absent = await http.GetAsync($"/assets/{new string('0', 64)}");

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(Abc, state.GetProperty("asset").GetString());
        Assert.Equal(3, state.GetProperty("size").GetInt64());
        Assert.Equal("abc"u8.ToArray(), bytes);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(DiagnosticCodes.AssetBadName, Code(await bad.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        Assert.Equal(DiagnosticCodes.AssetNotFound, Code(await absent.Content.ReadAsStringAsync()));
    }

    // ADR-0044 item 5: the command line puts a file and gets it back, and refuses a bad name.
    [Fact]
    public void The_Command_Line_Puts_And_Gets_An_Asset()
    {
        var file = Path.Combine(_folder, "photo.bin");
        File.WriteAllBytes(file, "abc"u8.ToArray());
        var copy = Path.Combine(_folder, "copy.bin");

        var (putExit, putOutput) = Run("asset", "put", file, "--folder", Store);
        var (getExit, getOutput) = Run("asset", "get", Abc, copy, "--folder", Store);
        var (badExit, badOutput) = Run("asset", "get", "ABC", copy, "--folder", Store);
        var (usageExit, _) = Run("asset", "put");

        Assert.Equal(0, putExit);
        Assert.Equal(Abc, JsonDocument.Parse(putOutput).RootElement.GetProperty("asset").GetString());
        Assert.Equal(0, getExit);
        Assert.Equal(3, JsonDocument.Parse(getOutput).RootElement.GetProperty("size").GetInt64());
        Assert.Equal("abc"u8.ToArray(), File.ReadAllBytes(copy));
        Assert.Equal(1, badExit);
        Assert.Equal(DiagnosticCodes.AssetBadName, Code(badOutput));
        Assert.Equal(2, usageExit);
    }

    // Findings E113 and E114 of the codebase review of 2026-10-31 (c) (TASK-0149): a name with a final
    // line feed, and a folder under a valid name, give a coded error from the store, the route and the
    // command line, and no exception.
    [Fact]
    public async Task A_Line_Feed_And_A_Folder_Under_A_Name_Give_Their_Codes_Everywhere()
    {
        var folder = new string('1', 64);
        Directory.CreateDirectory(Path.Combine(Store, folder));
        var store = new AssetStore(Store);
        await using var app = await StartSurface(store);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var read = await store.Read(folder);
        using var lineFeed = await http.GetAsync($"/assets/{Abc}%0A");
        using var held = await http.GetAsync($"/assets/{folder}");
        var (lineFeedExit, lineFeedOutput) = Run("asset", "get", Abc + "\n", Path.Combine(_folder, "copy.bin"), "--folder", Store);
        var (heldExit, heldOutput) = Run("asset", "get", folder, Path.Combine(_folder, "copy.bin"), "--folder", Store);

        Assert.Equal(DiagnosticCodes.AssetReadFailed, read.Error?.Code);
        Assert.Equal(HttpStatusCode.BadRequest, lineFeed.StatusCode);
        Assert.Equal(DiagnosticCodes.AssetBadName, Code(await lineFeed.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.InternalServerError, held.StatusCode);
        Assert.Equal(DiagnosticCodes.AssetReadFailed, Code(await held.Content.ReadAsStringAsync()));
        Assert.Equal((1, DiagnosticCodes.AssetBadName), (lineFeedExit, Code(lineFeedOutput)));
        Assert.Equal((1, DiagnosticCodes.AssetReadFailed), (heldExit, Code(heldOutput)));
    }

    // Findings E114 to E116: a stream that fails gives a code, a put of the right bytes repairs a
    // damaged file, and a cancelled put leaves no scratch file.
    [Fact]
    public async Task A_Put_Gives_A_Code_For_A_Failed_Stream_Repairs_A_Damaged_File_And_Leaves_No_Scratch_File()
    {
        var store = new AssetStore(Store);
        await store.Put(new MemoryStream("abc"u8.ToArray()));
        File.WriteAllBytes(Path.Combine(Store, Abc), "abd"u8.ToArray());

        var failed = await store.Put(new ThrowingStream());
        var repaired = await store.Put(new MemoryStream("abc"u8.ToArray()));
        var read = await store.Read(Abc);
        using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.Put(new CancelAtEnd("hello"u8.ToArray(), cancel), cancel.Token));

        Assert.Equal(DiagnosticCodes.AssetReadFailed, failed.Error?.Code);
        Assert.Equal((Abc, (string?)null), (repaired.Name, repaired.Error?.Code));
        Assert.Equal("abc"u8.ToArray(), read.Content);
        Assert.Equal([Abc], Directory.GetFiles(Store).Select(Path.GetFileName));
    }

    // Finding E118: a body over the request limit of the server gave a 413 with no envelope. A length
    // over the limit of an asset is refused before the read, and for a body with no length the route
    // lifts the limit of the server, so the store stops it at 16 MiB with the code of ADR-0044 item 3.
    // The route runs on a request of the test: over a socket the server answers and closes the
    // connection while the client still sends, and the client then fails before it reads the reply.
    [Fact]
    public async Task A_Body_Over_The_Limit_Gives_The_Envelope_And_The_Server_Sets_No_Limit_Of_Its_Own()
    {
        var store = new AssetStore(Store);
        var withLength = new DefaultHttpContext();
        withLength.Request.ContentLength = 31_000_000;
        withLength.Request.Body = new MemoryStream();
        var noLength = new DefaultHttpContext();
        var server = new ServerLimit();
        noLength.Features.Set<IHttpMaxRequestBodySizeFeature>(server);
        noLength.Request.Body = new MemoryStream(new byte[17_000_000]);

        var first = await Engine.Api.Http.Endpoints.AssetsEndpoint.Put(withLength.Request, store, CancellationToken.None);
        var second = await Engine.Api.Http.Endpoints.AssetsEndpoint.Put(noLength.Request, store, CancellationToken.None);

        Assert.All(new[] { first, second }, result =>
        {
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ((IStatusCodeHttpResult)result).StatusCode);
            Assert.Equal(DiagnosticCodes.AssetTooLarge, Code(JsonSerializer.Serialize(((IValueHttpResult)result).Value, Engine.Api.Http.Json.ApiJson.Options)));
        });
        Assert.Null(server.MaxRequestBodySize);
        Assert.False(Directory.Exists(Store) && Directory.EnumerateFileSystemEntries(Store).Any());
    }

    // The request limit of Kestrel, 30 MB by default.
    private sealed class ServerLimit : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => false;

        public long? MaxRequestBodySize { get; set; } = 30_000_000;
    }

    private static async Task<WebApplication> StartSurface(AssetStore store)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(store);
        builder.AddEngineSurface(_ => new EngineHost(new InProcessMeshBackend()));
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        return app;
    }

    // A stream whose second read fails, as a client that resets its connection.
    private sealed class ThrowingStream() : MemoryStream(new byte[200_000])
    {
        private int _calls;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (++_calls == 2)
                throw new IOException("The connection was reset.");
            return await base.ReadAsync(buffer, ct);
        }
    }

    // A stream that cancels the put after its last read, so the cancellation comes in the write.
    private sealed class CancelAtEnd(byte[] bytes, CancellationTokenSource cancel) : MemoryStream(bytes)
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            var read = await base.ReadAsync(buffer, ct);
            if (read == 0)
                await cancel.CancelAsync();
            return read;
        }
    }

    private static (int Exit, string Output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var exit = global::Engine.Cli.Cli.Run(args, stdout, new StringWriter());
        return (exit, stdout.ToString());
    }

    private static string? Code(string json) => JsonDocument.Parse(json).RootElement.GetProperty("error").GetProperty("code").GetString();
}
