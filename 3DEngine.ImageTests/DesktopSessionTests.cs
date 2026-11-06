using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;
using Microsoft.Extensions.Configuration;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// Phase R14b (ADR-0061 item 9, TASK-0194): the host owns the document session, the surface mounts on it as an option, and
// an open of a file stays in the same process. These tests run the session and the surface of the desktop with no window.
[Collection("desktop sessions")]
public sealed class DesktopSessionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-session-" + Guid.NewGuid().ToString("N"));

    public DesktopSessionTests() => Directory.CreateDirectory(Path.Combine(_folder, "documents"));

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Documents => Path.Combine(_folder, "documents");

    private string[] Arguments(string file) =>
    [
        "--port", "0",
        "--file", file,
        $"--{DesktopSurface.DocumentFolderSetting}={Documents}",
        $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(_folder, "unsaved")}",
        $"--Engine:AssetFolder={Path.Combine(_folder, "assets")}",
    ];

    private static async Task<int> Bodies(EngineHost host)
        => await host.Session.Read((document, _) => document.Bodies.Count);

    // A session with no surface, as on the phone (ADR-0061 item 4): a command of the person applies, and the file holds it.
    [Fact]
    public async Task A_Session_With_No_Surface_Applies_A_Command_And_Its_File_Holds_It()
    {
        var file = Path.Combine(Documents, "plate.3de");
        var settings = new ConfigurationBuilder().AddCommandLine(Arguments(file)).Build();

        var (session, code, message) = DesktopSession.Open(settings);
        Assert.True(session is not null, $"{code}: {message}");
        using (session)
            Assert.Equal(CommandStatus.Applied, (await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 })).Status);

        var (again, _, _) = DesktopSession.Open(settings);
        using (again)
            Assert.Equal(1, await Bodies(again!.Host));
    }

    // ADR-0030 item 4, amended by ADR-0061: an open that a check accepted ends the window loop of the first session. The
    // surface and the session of the first file are disposed, and the next session holds the second file, in the same
    // process. When its window closes with no open, the run ends with the code 0.
    [Fact]
    public async Task An_Open_Gives_The_Next_Session_The_File_In_The_Same_Process()
    {
        var first = Path.Combine(Documents, "first.3de");
        var second = Path.Combine(Documents, "second.3de");
        using (var backend = new ManifoldGeometryBackend())
        {
            var maker = new DocumentSession(EngineHosting.CreateDefault(backend));
            DocumentFile.Create(maker, second);
            await maker.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
            await maker.Apply(new CreateBoxCommand { SizeX = 4, SizeY = 5, SizeZ = 6 });
            maker.Close();
        }

        var shown = new List<(string? File, int Bodies, int Process)>();
        var hosts = new List<EngineHost>();
        void Show(DesktopSurface surface)
        {
            hosts.Add(surface.Host);
            shown.Add((surface.Host.Session.FilePath, Bodies(surface.Host).GetAwaiter().GetResult(), Environment.ProcessId));
            if (shown.Count == 1)
                Assert.Null(surface.Document.Folder.Open(second, CancellationToken.None).GetAwaiter().GetResult());
        }

        var exit = DesktopEngineHost.RunSessions(Arguments(first), DesktopEngineHost.Start, Show);

        Assert.Equal(0, exit);
        Assert.Equal(2, shown.Count);
        Assert.Equal((first, 0), (shown[0].File, shown[0].Bodies));
        Assert.Equal((second, 2), (shown[1].File, shown[1].Bodies));
        Assert.Equal(Environment.ProcessId, shown[1].Process);
        Assert.NotSame(hosts[0], hosts[1]);

        // The host disposed the first session: its file is free for a write.
        using (File.Open(first, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }
    }

    private static async Task Made(string file, int boxes)
    {
        using var backend = new ManifoldGeometryBackend();
        var maker = new DocumentSession(EngineHosting.CreateDefault(backend));
        DocumentFile.Create(maker, file);
        for (var i = 0; i < boxes; i++)
            await maker.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        maker.Close();
    }

    // Finding E149 of the codebase review of 2026-11-06 (TASK-0197): the open read the pending open, awaited the check and
    // then wrote, with no lock, so two opens at the same time were both accepted and the second one won. Now exactly one
    // open is accepted, and the other gives E-IO-LOAD-FAILED.
    [Fact]
    public async Task Two_Opens_At_The_Same_Time_Give_One_Accepted_Open()
    {
        var (second, third) = (Path.Combine(Documents, "second.3de"), Path.Combine(Documents, "third.3de"));
        await Made(second, 1);
        await Made(third, 2);
        var (surface, code, message) = DesktopEngineHost.Start(Arguments(Path.Combine(Documents, "first.3de")), null);
        Assert.True(surface is not null, $"{code}: {message}");
        using var session = surface.Session;
        using (surface)
        {
            var opens = await Task.WhenAll(
                Task.Run(() => surface.Document.Folder.Open(second, CancellationToken.None)),
                Task.Run(() => surface.Document.Folder.Open(third, CancellationToken.None)));

            Assert.Single(opens, error => error is null);
            Assert.Single(opens, error => error?.Code == DiagnosticCodes.IoLoadFailed);
            Assert.Equal(opens[0] is null ? second : third, surface.Document.RequestedOpen);
        }
    }

    // Finding E150 of the codebase review of 2026-11-06 (TASK-0197), Q3 A: the check accepted an open, and then the next
    // session did not start, for example because another program took the file. The run ended with the code of that
    // start, and the window closed with the first document already closed. ADR-0030 item 5 keeps the person in the window,
    // so a session on the previous file starts again, and the error goes to the standard error.
    [Fact]
    public async Task A_Next_Session_That_Does_Not_Start_Gives_The_Previous_File_Again()
    {
        var (first, second) = (Path.Combine(Documents, "first.3de"), Path.Combine(Documents, "second.3de"));
        await Made(first, 1);
        await Made(second, 2);
        var shown = new List<(string? File, int Bodies)>();
        void Show(DesktopSurface surface)
        {
            shown.Add((surface.Host.Session.FilePath, Bodies(surface.Host).GetAwaiter().GetResult()));
            if (shown.Count == 1)
                Assert.Null(surface.Document.Folder.Open(second, CancellationToken.None).GetAwaiter().GetResult());
        }

        (DesktopSurface?, int, string?) Start(string[] args, string? file)
            => file == second ? (null, DesktopSurface.ExitStartFailed, "The file is held.") : DesktopEngineHost.Start(args, file);

        var exit = DesktopEngineHost.RunSessions(Arguments(first), Start, Show);

        Assert.Equal(0, exit);
        Assert.Equal([(first, 1), (first, 1)], shown);
    }

    private string[] NoFile() =>
    [
        "--port", "0",
        $"--{DesktopSurface.DocumentFolderSetting}={Documents}",
        $"--{DesktopSurface.UnsavedFolderSetting}={Path.Combine(_folder, "unsaved")}",
        $"--Engine:AssetFolder={Path.Combine(_folder, "assets")}",
    ];

    // Finding V91 of the codebase review of 2026-11-06 (b) (TASK-0225), Q2 A: the start again of E150 kept the path of a
    // document of the unsaved folder and opened it as a named file, or ended the window when its journal did not exist.
    // A start with no file, an open of a second file, and a next session that does not start: with one command the
    // document starts again from its journal with no name; with no command it starts again as a new document, and no
    // empty file stays in the unsaved folder.
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public async Task The_Start_Again_Keeps_A_Document_Of_The_Unsaved_Folder(int boxes)
    {
        var second = Path.Combine(Documents, "second.3de");
        await Made(second, 2);
        var shown = new List<(string? Name, int Bodies)>();
        void Show(DesktopSurface surface)
        {
            if (shown.Count == 0)
            {
                for (var i = 0; i < boxes; i++)
                    Assert.Equal(CommandStatus.Applied, surface.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 }).GetAwaiter().GetResult().Status);
            }
            shown.Add((surface.Host.FileName, Bodies(surface.Host).GetAwaiter().GetResult()));
            if (shown.Count == 1)
                Assert.Null(surface.Document.Folder.Open(second, CancellationToken.None).GetAwaiter().GetResult());
        }

        (DesktopSurface?, int, string?) Start(string[] args, string? file)
            => file == second ? (null, DesktopSurface.ExitStartFailed, "The file is held.") : DesktopEngineHost.Start(args, file);

        var exit = DesktopEngineHost.RunSessions(NoFile(), Start, Show);

        Assert.Equal(0, exit);
        Assert.Equal([(null, boxes), (null, boxes)], shown);
        var unsaved = Path.Combine(_folder, "unsaved");
        Assert.Equal(boxes, Directory.Exists(unsaved) ? Directory.GetFiles(unsaved).Length : 0);
    }

    // Finding V92 of the codebase review of 2026-11-06 (b) (TASK-0225): a window that does not open, or a loop that fails,
    // gave an unhandled exception or the exit code 0. A show that fails gives ExitWindowFailed, and the run disposes the
    // session.
    [Fact]
    public void A_Show_That_Fails_Gives_The_Code_Of_The_Window()
    {
        var exit = DesktopEngineHost.RunSessions(Arguments(Path.Combine(Documents, "first.3de")), DesktopEngineHost.Start, (Func<DesktopSurface, bool>)(_ => false));

        Assert.Equal(DesktopSurface.ExitWindowFailed, exit);
        using (File.Open(Path.Combine(Documents, "first.3de"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }
    }

    // The surface mounts on the session and does not own it: after the stop of the surface, the session still applies a
    // command, and the host disposes it.
    [Fact]
    public async Task The_Surface_Mounts_On_A_Session_That_It_Does_Not_Dispose()
    {
        var (surface, code, message) = DesktopEngineHost.Start(Arguments(Path.Combine(Documents, "mounted.3de")), null);
        Assert.True(surface is not null, $"{code}: {message}");
        using var session = surface.Session;
        surface.Dispose();

        Assert.Equal(CommandStatus.Applied, (await session.Host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 })).Status);
    }
}
