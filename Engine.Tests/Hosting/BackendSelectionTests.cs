using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Engine.Cli;
using Engine.Core;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Tests.Hosting;

// TASK-0036. Each host requires the native backend and stops with
// E-GEOM-BACKEND-INIT when its library does not load. Until TASK-0036 each host
// took the managed backend with no message (finding E3 of the codebase review of
// 2026-09-23). A probe that answers "not available" stands for a computer with
// no library; a run with the library hidden from the output folders showed the
// same result on the real programs.
public class BackendSelectionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly BackendOptions NoNativeLibrary = new(false, () => false);

    private readonly WebApplicationFactory<Program> _baseFactory;

    public BackendSelectionTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task The_Command_Line_Stops_With_E_GEOM_BACKEND_INIT_When_The_Library_Does_Not_Load()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await Engine.Cli.Cli.RunAsync(
            ["apply", "CreateBox", "--param", "sizeX=1", "--param", "sizeY=2", "--param", "sizeZ=3"],
            stdout, stderr, CancellationToken.None, NoNativeLibrary);

        Assert.Equal(Engine.Cli.Cli.ExitBackendUnavailable, exit);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.StartsWith(DiagnosticCodes.GeomBackendInit + ":", stderr.ToString());
        Assert.Contains(ManifoldGeometryBackend.NativeLibraryName, stderr.ToString());
    }

    [Fact]
    public async Task A_Test_Can_Give_The_Command_Line_The_Managed_Backend()
    {
        var stdout = new StringWriter();

        var exit = await Engine.Cli.Cli.RunAsync(
            ["apply", "CreateBox", "--param", "sizeX=1", "--param", "sizeY=2", "--param", "sizeZ=3"],
            stdout, new StringWriter(), CancellationToken.None, BackendOptions.ManagedForTests);

        Assert.Equal(Engine.Cli.Cli.ExitApplied, exit);
    }

    [Fact]
    public void The_Http_Host_Refuses_To_Build_Its_Engine_When_The_Library_Does_Not_Load()
    {
        var error = Assert.Throws<BackendUnavailableException>(
            () => new EngineHost(new EventBroadcaster(), new HostBackendOptions(false, () => false)));

        Assert.StartsWith(DiagnosticCodes.GeomBackendInit + ":", error.Message);
        Assert.Contains(ManifoldGeometryBackend.NativeLibraryName, error.Message);
    }

    [NativeManifoldFact]
    public async Task Schema_Backend_Gives_The_Name_And_The_Version_Of_The_Native_Backend()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });

        var json = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/schema/backend");

        Assert.Equal("manifold", json.GetProperty("name").GetString());
        Assert.Equal(ManifoldGeometryBackend.NativeVersion, json.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Schema_Backend_Names_The_Managed_Backend_When_A_Test_Selects_It()
    {
        using var factory = _baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Singleton(HostBackendOptions.ManagedForTests))));

        var json = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/schema/backend");

        Assert.Equal("managed", json.GetProperty("name").GetString());
    }

    // The version that /schema/backend gives must be the version of the native
    // package that the project file pins, so that the answer cannot drift. The
    // package can add a fourth part for a rebuild of the same source (3.5.2.1,
    // TASK-0048); the first three parts are the Manifold version.
    [Fact]
    public void The_Native_Version_Is_The_Version_Of_The_Pinned_Package()
    {
        var project = XDocument.Load(Governance.RepositoryFiles.Path(
            "Engine.Geometry.Manifold", "Engine.Geometry.Manifold.csproj"));

        var pinned = project.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Single(e => e.Attribute("Include")?.Value == "Engine.Geometry.Manifold.Native")
            .Attribute("Version")!.Value;

        Assert.Equal(ManifoldGeometryBackend.NativeVersion, string.Join('.', pinned.Split('.').Take(3)));
    }
}
