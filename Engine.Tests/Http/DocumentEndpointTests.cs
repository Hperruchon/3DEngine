using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// ADR-0030 items 2, 4 and 5 (TASK-0092): the document folder, the routes of the
// document, and the check of a file before an open. Each test works in its own
// folder of scratch files, and removes it.
public sealed class DocumentEndpointTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-document-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Documents => Path.Combine(_folder, "documents");

    // A name whose file name, with .3de, has 255 characters, and one character more (E81).
    private const string LongestName =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string LongName = LongestName + "b";

    private static async Task<WebApplication> StartSurface(EngineHost host, DocumentFolder folder)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(folder);
        builder.AddEngineSurface(_ => host);
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> Post(HttpClient http, string route, object body)
    {
        using var response = await http.PostAsJsonAsync(route, body);
        return (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone());
    }

    [Fact]
    public async Task A_Host_With_No_File_Gives_No_Name()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host, new DocumentFolder(Documents));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var state = JsonDocument.Parse(await http.GetStringAsync("/document")).RootElement;

        Assert.Equal(JsonValueKind.Null, state.GetProperty("name").ValueKind);
        Assert.Equal("mm", state.GetProperty("lengthUnit").GetString());
        Assert.Equal(0, state.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task A_Save_Moves_The_Unsaved_File_Into_The_Document_Folder()
    {
        // The document folder does not exist yet; the save makes it.
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test", new DocumentFileOptions(null, Path.Combine(_folder, "unsaved")));
        await using var app = await StartSurface(host, new DocumentFolder(Documents));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        await Post(http, "/commands", new { name = "NoOp", schemaVersion = 1, parameters = new { echo = "x" } });
        var unsaved = host.Session.FilePath!;
        Assert.Equal(JsonValueKind.Null, JsonDocument.Parse(await http.GetStringAsync("/document")).RootElement.GetProperty("name").ValueKind);

        var (status, body) = await Post(http, "/document/save", new { name = "plate" });
        var state = JsonDocument.Parse(await http.GetStringAsync("/document")).RootElement;

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("plate.3de", body.GetProperty("name").GetString());
        Assert.Equal("plate.3de", state.GetProperty("name").GetString());
        Assert.Equal(1, state.GetProperty("version").GetInt64());
        Assert.False(File.Exists(unsaved));
        Assert.Equal(Path.Combine(Path.GetFullPath(Documents), "plate.3de"), host.Session.FilePath);
        host.Session.Close();
        Assert.Equal(2, File.ReadAllLines(Path.Combine(Documents, "plate.3de")).Length);
    }

    [Fact]
    public async Task A_Save_To_A_Name_That_Exists_Gives_A_Conflict()
    {
        Directory.CreateDirectory(Documents);
        File.WriteAllText(Path.Combine(Documents, "taken.3de"), "keep");
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test", new DocumentFileOptions(null, Path.Combine(_folder, "unsaved")));
        await using var app = await StartSurface(host, new DocumentFolder(Documents));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var (status, body) = await Post(http, "/document/save", new { name = "taken.3de" });

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal(DiagnosticCodes.IoSaveFailed, body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("keep", File.ReadAllText(Path.Combine(Documents, "taken.3de")));
        host.Session.Close();
    }

    // ADR-0030 item 2: a name is a plain file name in the document folder.
    [Theory]
    [InlineData("../outside")]
    [InlineData("sub/plate")]
    [InlineData("sub\\plate")]
    [InlineData("C:plate")]
    [InlineData("..")]
    [InlineData("")]
    [InlineData("pla\nte")]
    // E81 of the second codebase review of 2026-10-28: the rules of a file name of Windows, on
    // each operating system.
    [InlineData("CON")]
    [InlineData("nul")]
    [InlineData("com1.3de")]
    [InlineData("LPT9.part")]
    [InlineData("Aux .old")]
    [InlineData("a ")]
    [InlineData("a.")]
    [InlineData("a .3de")]
    [InlineData(".3de")]
    [InlineData("a*b")]
    [InlineData("a?b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a|b")]
    [InlineData("a\"b")]
    [InlineData(LongName)]
    public async Task A_Name_That_Is_Not_A_Plain_File_Name_Is_Refused(string name)
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test", new DocumentFileOptions(null, Path.Combine(_folder, "unsaved")));
        await using var app = await StartSurface(host, new DocumentFolder(Documents, (_, _) => Task.FromResult<ErrorDetail?>(null)));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        foreach (var route in new[] { "/document/save", "/document/open" })
        {
            var (status, body) = await Post(http, route, new { name });

            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Equal(DiagnosticCodes.ApiBadRequest, body.GetProperty("error").GetProperty("code").GetString());
        }

        Assert.False(File.Exists(Path.Combine(_folder, "outside.3de")));
        host.Session.Close();
    }

    [Theory]
    [InlineData("plate", "plate.3de")]
    [InlineData("plate.3de", "plate.3de")]
    [InlineData("plate.3DE", "plate.3DE")]
    [InlineData("a b.c", "a b.c.3de")]
    [InlineData("CONSOLE", "CONSOLE.3de")]
    [InlineData("com10", "com10.3de")]
    [InlineData("nul-plate", "nul-plate.3de")]
    [InlineData(LongestName, LongestName + ".3de")]
    public void A_Plain_Name_Resolves_In_The_Folder_With_The_Extension(string name, string file)
    {
        var folder = new DocumentFolder(Documents);

        Assert.Equal(Path.Combine(Path.GetFullPath(Documents), file), folder.Resolve(name, out var refusal));
        Assert.Null(refusal);
    }

    // E79 of the second codebase review of 2026-10-28: a folder with a final separator
    // refused each name.
    [Fact]
    public void A_Folder_With_A_Final_Separator_Resolves_A_Plain_Name()
    {
        var folder = new DocumentFolder(Documents + Path.DirectorySeparatorChar);

        Assert.Equal(Path.Combine(Path.GetFullPath(Documents), "plate.3de"), folder.Resolve("plate", out var refusal));
        Assert.Null(refusal);
    }

    // E85: engine-api-http with no --file refused a save with HTTP 409, and an open with 501.
    [Fact]
    public async Task A_Save_On_A_Host_With_No_File_Is_Not_Supported()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host, new DocumentFolder(Documents));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var (status, body) = await Post(http, "/document/save", new { name = "plate" });

        Assert.Equal(HttpStatusCode.NotImplemented, status);
        Assert.Equal(DiagnosticCodes.ApiNotSupported, body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(File.Exists(Path.Combine(Documents, "plate.3de")));
    }

    // E80: a name with a lone surrogate gave HTTP 500 with no body.
    [Fact]
    public async Task A_Name_That_Is_Not_Well_Formed_Text_Is_Refused()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test", new DocumentFileOptions(null, Path.Combine(_folder, "unsaved")));
        await using var app = await StartSurface(host, new DocumentFolder(Documents, (_, _) => Task.FromResult<ErrorDetail?>(null)));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        foreach (var route in new[] { "/document/save", "/document/open" })
        {
            using var content = new StringContent("{\"name\":\"\\ud800\"}", System.Text.Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(route, content);
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(DiagnosticCodes.ApiBadRequest, body.GetProperty("error").GetProperty("code").GetString());
        }

        host.Session.Close();
    }

    // ADR-0030 item 4: engine-api-http cannot start itself again.
    [Fact]
    public async Task An_Open_On_A_Host_That_Cannot_Start_Again_Is_Not_Supported()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host, new DocumentFolder(Documents));
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var (status, body) = await Post(http, "/document/open", new { name = "plate" });

        Assert.Equal(HttpStatusCode.NotImplemented, status);
        Assert.Equal(DiagnosticCodes.ApiNotSupported, body.GetProperty("error").GetProperty("code").GetString());
    }

    // ADR-0030 item 2: a host with no folder of its own, as engine-api-http, reads the
    // setting Engine:DocumentFolder and cannot start itself again.
    [Fact]
    public async Task A_Surface_With_No_Folder_Of_Its_Own_Reads_The_Setting_And_Refuses_An_Open()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test", new DocumentFileOptions(null, Path.Combine(_folder, "unsaved")));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration["Engine:DocumentFolder"] = Documents;
        builder.AddEngineSurface(_ => host);
        await using var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        Directory.CreateDirectory(Documents);

        var (opened, _) = await Post(http, "/document/open", new { name = "plate" });
        var (saved, _) = await Post(http, "/document/save", new { name = "plate" });

        Assert.Equal(HttpStatusCode.NotImplemented, opened);
        Assert.Equal(HttpStatusCode.OK, saved);
        Assert.Equal(Path.Combine(Path.GetFullPath(Documents), "plate.3de"), host.Session.FilePath);
        host.Session.Close();
    }

    [Fact]
    public async Task An_Open_Gives_The_Path_To_The_Host_And_Its_Answer_To_The_Agent()
    {
        var asked = new List<string>();
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var folder = new DocumentFolder(Documents, (path, _) =>
        {
            asked.Add(path);
            return Task.FromResult<ErrorDetail?>(path.EndsWith("bad.3de", StringComparison.Ordinal)
                ? new ErrorDetail(DiagnosticCodes.IoLoadFailed, "no")
                : null);
        });
        await using var app = await StartSurface(host, folder);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var (accepted, _) = await Post(http, "/document/open", new { name = "plate" });
        var (refused, body) = await Post(http, "/document/open", new { name = "bad" });

        Assert.Equal(HttpStatusCode.Accepted, accepted);
        Assert.Equal(HttpStatusCode.Conflict, refused);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal([Path.Combine(Path.GetFullPath(Documents), "plate.3de"), Path.Combine(Path.GetFullPath(Documents), "bad.3de")], asked);
    }

    // ADR-0030 item 5: the check opens the file in a scratch session, also a file that
    // a session of this process holds open, and gives the code of a file that does not open.
    [Fact]
    public async Task The_Check_Before_An_Open_Reads_A_Held_File_And_Gives_The_Code_Of_A_Bad_One()
    {
        var path = Path.Combine(_folder, "held.3de");
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        DocumentFile.Create(session, path);
        await session.Apply(new NoOpCommand { Echo = "x" });

        var good = await DocumentFile.Check(path, new InProcessMeshBackend());
        var otherBackend = await DocumentFile.Check(path, NullGeometryBackend.Instance);
        session.Close();
        var bad = Path.Combine(_folder, "bad.3de");
        File.WriteAllText(bad, "{\"format\":\"another\"}\n");
        var badHeader = await DocumentFile.Check(bad, new InProcessMeshBackend());

        Assert.Null(good);
        Assert.Equal(DiagnosticCodes.IoBackendMismatch, otherBackend!.Code);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, badHeader!.Code);
        Assert.Equal(2, File.ReadAllLines(path).Length);
    }
}
