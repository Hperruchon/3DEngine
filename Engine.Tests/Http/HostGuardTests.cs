using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
    public void An_Address_That_Is_Not_Loopback_Is_Refused_With_A_Message(string urls)
    {
        var refusal = Program.LoopbackRefusal(urls);

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
        Assert.NotNull(Program.LoopbackRefusal(bound));
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

        var refusal = Program.ConfigurationRefusal(configuration);

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

        Assert.Null(Program.ConfigurationRefusal(configuration));
        Assert.Null(Program.ConfigurationRefusal(Configuration()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost:5187")]
    [InlineData("http://127.0.0.1:5187;http://[::1]:5187")]
    [InlineData("https://LOCALHOST:5188")]
    public void A_Loopback_Address_Is_Accepted(string? urls)
    {
        Assert.Null(Program.LoopbackRefusal(urls));
    }

    [Fact]
    public async Task The_Process_Refuses_To_Start_On_An_Address_That_Is_Not_Loopback()
    {
        using var process = StartHost("http://0.0.0.0:0");
        var errorText = process.StandardError.ReadToEndAsync();
        var outputText = process.StandardOutput.ReadToEndAsync();

        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("The host did not exit within 30 seconds. It bound to 0.0.0.0 and ran.");
        }

        Assert.Equal(1, process.ExitCode);
        Assert.Contains("loopback", await errorText, StringComparison.Ordinal);
        GC.KeepAlive(outputText);
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
        using var process = StartHost(arguments, httpPorts);
        var errorText = process.StandardError.ReadToEndAsync();
        var outputText = process.StandardOutput.ReadToEndAsync();

        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("The host did not exit within 30 seconds. It bound the address and ran.");
        }

        Assert.Equal(1, process.ExitCode);
        Assert.Contains("loopback", await errorText, StringComparison.Ordinal);

        // The refusal came before a bind. Without the check before the start, the
        // check after it also gives exit code 1, after a bind on each interface
        // (finding T27 of the review of 2026-10-09, TASK-0058).
        Assert.DoesNotContain("Now listening on", await outputText, StringComparison.Ordinal);
    }

    // Finding E41 of the review of 2026-10-09: the host reloaded
    // Kestrel:Endpoints when appsettings.json changed after the start, and started
    // the new endpoints with no check. The new endpoint here is a loopback
    // address, so the run before the correction bound nothing foreign.
    [Fact]
    public async Task A_Change_Of_The_Configuration_File_After_The_Start_Starts_No_Endpoint()
    {
        var folder = Directory.CreateTempSubdirectory("engine-api-http-").FullName;
        using var process = StartHost("--urls http://127.0.0.1:0", httpPorts: null, folder);
        try
        {
            await ListeningAddressAsync(process);

            var lines = new List<string>();
            _ = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
                    lock (lines) lines.Add(line);
            });

            File.WriteAllText(
                Path.Combine(folder, "appsettings.json"),
                """{ "Kestrel": { "Endpoints": { "E1": { "Url": "http://127.0.0.1:0" } } } }""");
            await Task.Delay(TimeSpan.FromSeconds(5));

            lock (lines)
            {
                Assert.DoesNotContain(lines, line =>
                    line.Contains("Config changed", StringComparison.Ordinal)
                    || line.Contains("Now listening on", StringComparison.Ordinal));
            }
        }
        finally
        {
            process.Kill(entireProcessTree: true);
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
        using var process = StartHost("http://127.0.0.1:0");
        try
        {
            var address = await ListeningAddressAsync(process);

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
            process.Kill(entireProcessTree: true);
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

    // The host binary and its runtime configuration are in the test output,
    // because Engine.Tests references Engine.Api.Http.
    private static Process StartHost(string urls) => StartHost($"--urls {urls}", httpPorts: null);

    // Each variable that gives an address is removed first, so that only the
    // arguments and the given port value reach the host.
    private static Process StartHost(string arguments, string? httpPorts, string? workingDirectory = null)
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

        return Process.Start(start)!;
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    // The address that the host reports on its standard output, within 30 seconds.
    private static async Task<string> ListeningAddressAsync(Process process)
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!limit.IsCancellationRequested)
        {
            var line = await process.StandardOutput.ReadLineAsync(limit.Token);
            if (line is null)
                break;

            var match = Regex.Match(line, @"Now listening on: (http://[^\s]+)");
            if (match.Success)
                return match.Groups[1].Value;
        }

        process.Kill(entireProcessTree: true);
        throw new InvalidOperationException("The host did not report its address within 30 seconds.");
    }
}
