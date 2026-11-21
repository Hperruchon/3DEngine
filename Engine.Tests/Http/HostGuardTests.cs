using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Tests.Http;

// docs/CHARTER.md, target consumers: Engine.Api.Http binds to localhost only.
// ADR-0019 §5: the desktop listens on the loopback address only. Finding E15 of
// the codebase review of 2026-09-30: no source checked the address, a WebSocket
// upgrade with a foreign Origin was accepted, and a command with a foreign Host
// header was applied. Register entry R-0028.
//
// Two tests spawn the real host, because the test server and Kestrel do not
// agree on when a request is a WebSocket request. An Origin check placed
// before the WebSocket middleware passed the in-process test and let a foreign
// Origin through on Kestrel. Only a run on the real host sees that.
// The two tests that spawn the real host share one collection with the heartbeat
// test, so that a process start does not run beside a measurement of time.
[Collection("real host")]
public class HostGuardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HostGuardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("http://0.0.0.0:0")]
    [InlineData("http://*:5000")]
    [InlineData("http://+:5000")]
    [InlineData("http://192.168.1.10:5000")]
    [InlineData("http://127.0.0.1:5000;http://0.0.0.0:5001")]
    // Finding E40 of the review of 2026-10-09: System.Uri reads the host of these
    // two as loopback, and Kestrel reads "evil@localhost" as a host name, which
    // binds each interface (TASK-0058).
    [InlineData("http://evil@localhost:5000")]
    [InlineData("http://x@127.0.0.1:5000")]
    // Finding E48 of the review of 2026-10-11: the check removed each bracket
    // before it read the address, and Kestrel read these forms as a host name, which
    // binds each interface (TASK-0062).
    [InlineData("http://[127.0.0.1]:5000")]
    [InlineData("http://[[::1]]:5000")]
    [InlineData("http://[::1:5000")]
    [InlineData("http://127.0.0.1]:5000")]
    [InlineData("http://]127.0.0.1[:5000")]
    [InlineData("http://[127.0.0.1:5000")]
    [InlineData("http://[[127.0.0.1]]:5000")]
    public void An_Address_That_Is_Not_Loopback_Is_Refused_With_A_Message(string urls)
    {
        var refusal = EngineSurface.LoopbackRefusal(urls);

        Assert.NotNull(refusal);
        Assert.Contains("loopback", refusal, StringComparison.Ordinal);
    }

    // The form in which Kestrel reports an address that it bound on each
    // interface. The check after the start reads this form (TASK-0051).
    [Theory]
    [InlineData("http://[::]:5000")]
    [InlineData("http://0.0.0.0:5000")]
    public void A_Bound_Address_On_Each_Interface_Is_Refused(string bound)
    {
        Assert.NotNull(EngineSurface.LoopbackRefusal(bound));
    }

    // Finding E23 of the review of 2026-10-04: the guard read the key "urls" only.
    // Kestrel also reads each endpoint URL and the two port keys (TASK-0051).
    [Theory]
    [InlineData("Kestrel:Endpoints:E1:Url", "http://192.0.2.1:5880")]
    [InlineData("Kestrel:Endpoints:E1:Url", "http://0.0.0.0:5880")]
    [InlineData("http_ports", "5880")]
    [InlineData("https_ports", "5881")]
    [InlineData("urls", "http://192.0.2.1:5880")]
    public void Each_Source_Of_A_Foreign_Address_In_The_Configuration_Is_Refused(string key, string value)
    {
        var configuration = Configuration((key, value));

        var refusal = EngineSurface.ConfigurationRefusal(configuration);

        Assert.NotNull(refusal);
        Assert.Contains("loopback", refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void Loopback_Addresses_In_Each_Source_Are_Accepted()
    {
        var configuration = Configuration(
            ("urls", "http://127.0.0.1:5187"),
            ("Kestrel:Endpoints:E1:Url", "http://localhost:5188"),
            ("Kestrel:Endpoints:E2:Url", "http://[::1]:5189"));

        Assert.Null(EngineSurface.ConfigurationRefusal(configuration));
        Assert.Null(EngineSurface.ConfigurationRefusal(Configuration()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost:5187")]
    [InlineData("http://127.0.0.1:5187;http://[::1]:5187")]
    [InlineData("https://LOCALHOST:5188")]
    public void A_Loopback_Address_Is_Accepted(string? urls)
    {
        Assert.Null(EngineSurface.LoopbackRefusal(urls));
    }

    // Finding E24 of the codebase review of 2026-10-04 (R-0033, TASK-0321): the guard accepted each loopback
    // address, and the Host filter accepts three names only, so a host on one of these addresses refused each
    // request. The guard now refuses them at the start.
    [Theory]
    [InlineData("http://127.0.0.2:5000")]
    [InlineData("http://127.1:5000")]
    [InlineData("http://[0:0:0:0:0:0:0:1]:5000")]
    public void A_Loopback_Address_That_The_Host_Filter_Refuses_Is_Refused(string urls)
    {
        Assert.NotNull(EngineSurface.LoopbackRefusal(urls));
    }

    // Finding E24 of the codebase review of 2026-10-04 (R-0033, TASK-0321): the Host filter read
    // SubscriberOptions.Default, and the Origin check read the instance of the container. Both read the
    // instance of the container now.
    [Fact]
    public async Task The_Host_Filter_Reads_The_List_Of_The_Container()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton(SubscriberOptions.Default with { PermittedOriginHosts = ["127.0.0.1"] }))));
        var http = factory.CreateClient();

        var named = new HttpRequestMessage(HttpMethod.Get, "/schema/events");
        named.Headers.Host = "localhost";
        var numbered = new HttpRequestMessage(HttpMethod.Get, "/schema/events");
        numbered.Headers.Host = "127.0.0.1";

        Assert.Equal(HttpStatusCode.BadRequest, (await http.SendAsync(named)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.SendAsync(numbered)).StatusCode);
    }

    [Fact]
    public async Task The_Process_Refuses_To_Start_On_An_Address_That_Is_Not_Loopback()
    {
        using var host = StartHost("http://0.0.0.0:0");

        // A host that binds to 0.0.0.0 and runs listens before an exit, and fails here.
        await Exited(host);

        Assert.Equal(1, host.Process.ExitCode);
        Assert.Contains("loopback", host.Error, StringComparison.Ordinal);
    }

    // Before TASK-0051 the host gave no refusal for these two sources. It tried to
    // bind the endpoint address, and it bound each interface for the port key.
    // 192.0.2.1 is a documentation address (RFC 5737), so no computer holds it,
    // and the run before the correction bound nothing.
    [Theory]
    [InlineData("--Kestrel:Endpoints:E1:Url=http://192.0.2.1:5880", null)]
    [InlineData("", "5880")]
    public async Task The_Process_Refuses_A_Foreign_Address_From_Each_Source(string arguments, string? httpPorts)
    {
        using var host = StartHost(arguments, httpPorts);

        await Exited(host);

        Assert.Equal(1, host.Process.ExitCode);
        Assert.Contains("loopback", host.Error, StringComparison.Ordinal);

        // The refusal came before a bind. Without the check before the start, the
        // check after it also gives exit code 1, after a bind on each interface
        // (finding T27 of the review of 2026-10-09, TASK-0058).
        Assert.DoesNotContain("Now listening on", host.Output, StringComparison.Ordinal);
    }

    // Finding E51 of the review of 2026-10-11: a port in use ended a host with an
    // exception and the exit code of a crash. The host now stops with the
    // documented code 4 (TASK-0062).
    [Fact]
    public async Task A_Second_Host_On_A_Port_In_Use_Stops_With_Exit_Code_4()
    {
        using var first = StartHost("http://127.0.0.1:0");
        try
        {
            var address = await ListeningAddressAsync(first);

            using var second = StartHost(address);
            await Exited(second);

            Assert.Equal(4, second.Process.ExitCode);
            Assert.Contains("in use", second.Error, StringComparison.Ordinal);
        }
        finally
        {
            first.Process.Kill(entireProcessTree: true);
        }
    }

    // Finding E41 of the review of 2026-10-09: the host reloaded
    // Kestrel:Endpoints when appsettings.json changed after the start, and started
    // the new endpoints with no check. The new endpoint here is a loopback
    // address, so the run before the correction bound nothing foreign.
    [Fact]
    public async Task A_Change_Of_The_Configuration_File_After_The_Start_Starts_No_Endpoint()
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-").FullName;
        using var host = StartHost("--urls http://127.0.0.1:0", httpPorts: null, folder);
        try
        {
            await ListeningAddressAsync(host);
            var output = host.Output;
            var start = output.IndexOf('\n', output.IndexOf("Now listening on", StringComparison.Ordinal)) + 1;

            File.WriteAllText(
                Path.Combine(folder, "appsettings.json"),
                """{ "Kestrel": { "Endpoints": { "E1": { "Url": "http://127.0.0.1:0" } } } }""");

            // TASK-0449: the wait of 5 s can only let a fault pass, never fail a correct host, so no time decides a test that
            // passes (ADR-0173 item 4).
            await Task.Delay(TimeSpan.FromSeconds(5));

            var later = host.Output[start..];
            Assert.DoesNotContain("Config changed", later, StringComparison.Ordinal);
            Assert.DoesNotContain("Now listening on", later, StringComparison.Ordinal);
        }
        finally
        {
            host.Process.Kill(entireProcessTree: true);
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
                // A file that the stopped process still held stays in the folder of Path.GetTempPath.
            }
        }
    }

    [Fact]
    public async Task On_The_Real_Host_A_WebSocket_Upgrade_With_A_Foreign_Origin_Is_Refused()
    {
        using var host = StartHost("http://127.0.0.1:0");
        try
        {
            var address = await ListeningAddressAsync(host);

            using var foreign = new ClientWebSocket();
            foreign.Options.SetRequestHeader("Origin", "https://evil.example");
            var error = await Assert.ThrowsAsync<WebSocketException>(
                () => foreign.ConnectAsync(new Uri(address.Replace("http://", "ws://") + "/events"), CancellationToken.None));
            Assert.Contains("403", error.Message, StringComparison.Ordinal);

            using var loopback = new ClientWebSocket();
            loopback.Options.SetRequestHeader("Origin", "http://localhost:3000");
            await loopback.ConnectAsync(new Uri(address.Replace("http://", "ws://") + "/events"), CancellationToken.None);
            Assert.Equal(WebSocketState.Open, loopback.State);
        }
        finally
        {
            host.Process.Kill(entireProcessTree: true);
        }
    }

    [Fact]
    public async Task A_WebSocket_Upgrade_With_A_Foreign_Origin_Is_Refused()
    {
        var client = _factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Origin = "https://evil.example";
        var uri = new UriBuilder(_factory.Server.BaseAddress) { Scheme = "ws", Path = "/events" }.Uri;

        var error = await Assert.ThrowsAnyAsync<Exception>(() => client.ConnectAsync(uri, CancellationToken.None));

        Assert.Contains("403", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_WebSocket_Upgrade_With_A_Loopback_Origin_Is_Accepted()
    {
        var client = _factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Origin = "http://localhost:1234";
        var uri = new UriBuilder(_factory.Server.BaseAddress) { Scheme = "ws", Path = "/events" }.Uri;

        using var socket = await client.ConnectAsync(uri, CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);
    }

    [Fact]
    public async Task A_Command_With_A_Foreign_Host_Header_Is_Refused()
    {
        var http = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/commands")
        {
            Content = JsonContent.Create(new { name = "NoOp", schemaVersion = 1, parameters = new { echo = "x" } }),
        };
        request.Headers.Host = "evil.example";

        var response = await http.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_Command_With_The_Loopback_Host_Is_Accepted()
    {
        var http = _factory.CreateClient();

        var response = await http.PostAsJsonAsync("/commands", new { name = "NoOp", schemaVersion = 1, parameters = new { echo = "x" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Finding E59 of the codebase review of 2026-10-12: a failure of the start other
    // than a port in use ended the host with an unhandled exception and the exit code
    // of a crash. A Kestrel endpoint named localhost with port 0 is such a failure on
    // each operating system; the host now stops with the documented code 5 (TASK-0067).
    [Fact]
    public async Task A_Host_Whose_Start_Fails_Stops_With_Exit_Code_5()
    {
        using var host = StartHost("--Kestrel:Endpoints:Http:Url=http://localhost:0", httpPorts: null);
        await Exited(host);

        Assert.Equal(5, host.Process.ExitCode);
        Assert.Contains("The host did not start", host.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", host.Error, StringComparison.Ordinal);
    }

    // Finding E68 of the codebase review of 2026-10-17: a configuration file that the
    // framework cannot read ended the host with an unhandled exception and the exit code
    // of a crash. The host now stops with the documented code 5 (TASK-0079).
    [Fact]
    public async Task A_Host_With_A_Configuration_File_That_It_Cannot_Read_Stops_With_Exit_Code_5()
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-").FullName;
        File.WriteAllText(Path.Combine(folder, "appsettings.json"), "{ this is not json");
        using var host = StartHost("--urls http://127.0.0.1:0", httpPorts: null, folder);
        try
        {
            await Exited(host);
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
                // A file that the stopped process still held stays in the folder of Path.GetTempPath.
            }
        }

        var error = host.Error;
        Assert.True(host.Process.ExitCode == 5, $"The exit code is {host.Process.ExitCode}. The error output: {error}");
        Assert.Contains("The host did not start", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", error, StringComparison.Ordinal);
    }

    // ADR-0015 items 4 and 10 (TASK-0091): with --file, each applied command is in the
    // file before its result. The test kills the host after the results, so no stop
    // writes anything, and a second host opens the file to the same body.
    [Fact]
    public async Task A_Host_With_A_File_Keeps_Each_Applied_Command_After_A_Kill()
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-file-").FullName;
        var path = Path.Combine(folder, "plate.3de");
        try
        {
            Guid bodyId;
            using (var first = StartHost($"--urls http://127.0.0.1:0 --file \"{path}\"", httpPorts: null))
            {
                using var http = new HttpClient { BaseAddress = new Uri(await ListeningAddressAsync(first)) };
                bodyId = (await PostAsync(http, "/commands", new { name = "CreateBox", schemaVersion = 1, parameters = new { sizeX = 1.0, sizeY = 2.0, sizeZ = 3.0 } }))
                    .GetProperty("outputs").GetProperty("bodyId").GetGuid();
                var renamed = await PostAsync(http, "/commands", new { name = "RenameBody", schemaVersion = 1, parameters = new { bodyId, name = "Plate" } });
                Assert.Equal("Applied", renamed.GetProperty("status").GetString());
                await Stopped(first);
            }

            Assert.Equal(3, (await ReadLinesWhenFree(path)).Length);

            using var second = StartHost($"--urls http://127.0.0.1:0 --file \"{path}\"", httpPorts: null);
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(await ListeningAddressAsync(second)) };
                var info = (await PostAsync(http, "/queries", new { name = "GetBodyInfo", schemaVersion = 1, parameters = new { bodyId = bodyId.ToString() } }))
                    .GetProperty("result");
                Assert.Equal("Plate", info.GetProperty("name").GetString());
            }
            finally
            {
                await Stopped(second);
            }
        }
        finally
        {
            await DeleteFolder(folder);
        }
    }

    // ADR-0015 item 10: with no --file the document is in memory only, and the host
    // says so before it serves a request (the owner, 2026-10-28).
    [Fact]
    public async Task A_Host_With_No_File_Says_That_The_Document_Is_In_Memory_Only()
    {
        using var host = StartHost("--urls http://127.0.0.1:0", httpPorts: null);
        try
        {
            await ListeningAddressAsync(host);
        }
        finally
        {
            await Stopped(host);
        }

        Assert.Contains("has no document file", host.Error, StringComparison.Ordinal);
    }

    // E77 of the second codebase review of 2026-10-28: a save moves the file of --file, and a
    // start again with the same --file made an empty document with no message. A start that
    // makes the file now says so; a start that opens it says nothing (ADR-0015 item 10).
    [Fact]
    public async Task A_Host_That_Makes_The_File_Of_File_Says_So()
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-file-").FullName;
        var path = Path.Combine(folder, "work.3de");
        try
        {
            var made = await ErrorOutputOfAStart($"--urls http://127.0.0.1:0 --file \"{path}\"");
            var opened = await ErrorOutputOfAStart($"--urls http://127.0.0.1:0 --file \"{path}\"");

            Assert.Contains("did not exist, so the host made a new empty document file", made, StringComparison.Ordinal);
            Assert.DoesNotContain("did not exist", opened, StringComparison.Ordinal);
        }
        finally
        {
            await DeleteFolder(folder);
        }
    }

    // The error output of a host from its start until it listens; the host is then killed.
    private static async Task<string> ErrorOutputOfAStart(string arguments)
    {
        using var host = StartHost(arguments, httpPorts: null);
        try
        {
            await ListeningAddressAsync(host);
        }
        finally
        {
            await Stopped(host);
        }

        return host.Error;
    }

    // The host is killed, and each output is read to its end.
    private static async Task Stopped(Host host)
    {
        host.Process.Kill(entireProcessTree: true);
        await host.Process.WaitForExitAsync();
        Assert.True(await host.Progresses(host.Reads), $"The outputs of the host did not end in {NoProgress.TotalSeconds} s after its exit.");
    }

    // A --file that names a folder gave exit code 5 until TASK-0099 (finding E82 of the
    // second codebase review of 2026-10-28).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_Host_Whose_File_Does_Not_Open_Stops_With_Exit_Code_6(bool fileIsAFolder)
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-file-").FullName;
        var path = Path.Combine(folder, "bad.3de");
        if (fileIsAFolder)
            Directory.CreateDirectory(path);
        else
            File.WriteAllText(path, "{\"format\":\"another-document\",\"formatVersion\":1,\"lengthUnit\":\"mm\"}\n");
        try
        {
            using var host = StartHost($"--urls http://127.0.0.1:0 --file \"{path}\"", httpPorts: null);
            await Exited(host);

            var error = host.Error;
            Assert.True(host.Process.ExitCode == 6, $"The exit code is {host.Process.ExitCode}. The error output: {error}");
            Assert.Contains("E-IO-LOAD-FAILED", error, StringComparison.Ordinal);
        }
        finally
        {
            await DeleteFolder(folder);
        }
    }

    // Windows can hold the file of a killed host for a short time after its exit (run
    // 40682786957), so the cleanup tries for 5 s and then leaves the folder in the
    // folder of Path.GetTempPath.
    private static async Task DeleteFolder(string folder)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
                return;
            }
            catch (IOException)
            {
                await Task.Delay(250);
            }
        }
    }

    // Finding T87 of the codebase review of 2026-10-29 (TASK-0107): the run 40839708477 on
    // Windows read the file of a killed host before the process released it. The read tries
    // again, and the last try gives its exception. TASK-0449 (ADR-0173 item 4): the tries last
    // NoProgress (60 s), the time against a wait for ever, and no longer 10 s.
    private static async Task<string[]> ReadLinesWhenFree(string path)
    {
        for (var attempt = 0; attempt < NoProgress.TotalMilliseconds / 250; attempt++)
        {
            try
            {
                return File.ReadAllLines(path);
            }
            catch (IOException)
            {
                await Task.Delay(250);
            }
        }
        return File.ReadAllLines(path);
    }

    [Fact]
    public async Task A_File_That_Another_Handle_Holds_Is_Read_When_The_Handle_Closes()
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-held-").FullName;
        var path = Path.Combine(folder, "plate.3de");
        try
        {
            await File.WriteAllLinesAsync(path, ["a", "b", "c"]);
            using var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.Throws<IOException>(() => File.ReadAllLines(path));
            using var release = new Timer(_ => held.Dispose(), null, 500, Timeout.Infinite);

            Assert.Equal(["a", "b", "c"], await ReadLinesWhenFree(path));
        }
        finally
        {
            await DeleteFolder(folder);
        }
    }

    private static async Task<JsonElement> PostAsync(HttpClient http, string route, object body)
    {
        using var response = await http.PostAsJsonAsync(route, body);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    // The host binary and its runtime configuration are in the test output,
    // because Engine.Tests references Engine.Api.Http.
    private static Host StartHost(string urls) => StartHost($"--urls {urls}", httpPorts: null);

    // Each variable that gives an address is removed first, so that only the
    // arguments and the given port value reach the host.
    private static Host StartHost(string arguments, string? httpPorts, string? workingDirectory = null)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "engine-api-http.dll");
        Assert.True(File.Exists(dll), $"{dll} is absent.");

        // The working directory is the content root of the host, where it reads
        // appsettings.json.
        var start = new ProcessStartInfo("dotnet", $"\"{dll}\" {arguments}")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory,
        };
        foreach (var name in new[] { "ASPNETCORE_URLS", "DOTNET_URLS", "ASPNETCORE_HTTP_PORTS", "ASPNETCORE_HTTPS_PORTS", "DOTNET_HTTP_PORTS", "DOTNET_HTTPS_PORTS" })
            start.Environment.Remove(name);
        if (httpPorts is not null)
            start.Environment["ASPNETCORE_HTTP_PORTS"] = httpPorts;

        return new Host(Process.Start(start)!);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    // The address that the host reports on its standard output. A host that exits first, or that makes no progress for
    // NoProgress, fails the test.
    private static async Task<string> ListeningAddressAsync(Host host)
    {
        var exit = host.Process.WaitForExitAsync();
        if (!await host.Progresses(Task.WhenAny(host.Listening, exit)))
        {
            host.Process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"The host made no progress for {NoProgress.TotalSeconds} s and did not report its address. Its error output: {host.Error}");
        }

        if (host.Listening.IsCompleted)
            return await host.Listening;
        throw new InvalidOperationException($"The host exited with code {host.Process.ExitCode} before it reported its address. Its error output: {host.Error}");
    }

    // The host exits. A host that listens first, or that makes no progress for NoProgress, fails the test. Then each output
    // is read to its end.
    private static async Task Exited(Host host)
    {
        var exit = host.Process.WaitForExitAsync();
        if (!await host.Progresses(Task.WhenAny(exit, host.Listening)))
        {
            host.Process.Kill(entireProcessTree: true);
            Assert.Fail($"The host made no progress for {NoProgress.TotalSeconds} s and did not exit.");
        }

        if (!exit.IsCompleted)
        {
            host.Process.Kill(entireProcessTree: true);
            Assert.Fail($"The host listened on {await host.Listening} and did not exit.");
        }

        await exit;
        Assert.True(await host.Progresses(host.Reads), $"The outputs of the host did not end in {NoProgress.TotalSeconds} s after its exit.");
    }

    // ADR-0173 item 4 (TASK-0449, item 4 of P0.180): no time limit decides a test that passes. A host makes progress while it
    // writes a line or uses processor time. A host that does neither for NoProgress fails its test, and the hang limit of the
    // step "Test" stops a host that never ends. Until TASK-0449 a limit of 30 s from the start of each host decided these
    // tests: on 2026-11-21 one to three of them failed at that limit in each full local run, where many tests share the
    // processors, and each passed alone.
    private static readonly TimeSpan NoProgress = TimeSpan.FromSeconds(60);

    // A host process, with its two outputs read line by line from its start.
    private sealed class Host : IDisposable
    {
        private readonly StringBuilder _output = new();
        private readonly StringBuilder _error = new();
        private readonly TaskCompletionSource<string> _listening = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private long _lines;

        public Host(Process process)
        {
            Process = process;
            Reads = Task.WhenAll(Read(process.StandardOutput, _output, listens: true), Read(process.StandardError, _error, listens: false));
        }

        public Process Process { get; }

        // Complete when both outputs are at their end.
        public Task Reads { get; }

        // The address of the first line "Now listening on".
        public Task<string> Listening => _listening.Task;

        public string Output
        {
            get
            {
                lock (_output)
                    return _output.ToString();
            }
        }

        public string Error
        {
            get
            {
                lock (_error)
                    return _error.ToString();
            }
        }

        // True when the task completes; false when the host writes no line and uses no processor time for NoProgress.
        public async Task<bool> Progresses(Task done)
        {
            var (mark, still) = (Mark(), Stopwatch.StartNew());
            while (!done.IsCompleted)
            {
                await Task.WhenAny(done, Task.Delay(TimeSpan.FromSeconds(1)));
                var now = Mark();
                if (now != mark)
                    (mark, still) = (now, Stopwatch.StartNew());
                else if (still.Elapsed >= NoProgress && !done.IsCompleted)
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            try
            {
                Process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process has no state to stop.
            }

            Process.Dispose();
        }

        // The count of the lines and the processor time of the host; an exited host has a fixed mark.
        private (long Lines, TimeSpan Processor) Mark()
        {
            var lines = Interlocked.Read(ref _lines);
            try
            {
                Process.Refresh();
                return (lines, Process.HasExited ? TimeSpan.Zero : Process.TotalProcessorTime);
            }
            catch (InvalidOperationException)
            {
                return (lines, TimeSpan.Zero);
            }
        }

        private async Task Read(StreamReader reader, StringBuilder text, bool listens)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                lock (text)
                    text.Append(line).Append('\n');
                Interlocked.Increment(ref _lines);
                if (listens && Regex.Match(line, @"Now listening on: (http://[^\s]+)") is { Success: true } match)
                    _listening.TrySetResult(match.Groups[1].Value);
            }
        }
    }
}
