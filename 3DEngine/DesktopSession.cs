using Engine.Api.Http;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;
using Microsoft.Extensions.Configuration;

namespace ThreeDEngine.Desktop;

// Phase R14b (ADR-0061 item 9, TASK-0194): the document session of the desktop, which the host owns. The host makes it
// from the settings, mounts the HTTP and WebSocket surface on it as an option (DesktopSurface), and disposes it after the
// surface. Until R14b the container of the surface made the session and disposed it, so a host with no surface had no
// session, and an open of a file started the program again. A host for the phone mounts no surface (ADR-0061 item 4).
//
// The document file (ADR-0030, TASK-0092): the setting "file" (--file) names it, and the session opens it, or makes it
// when it does not exist. With no file the session writes into the folder of unsaved documents
// ("Desktop:UnsavedFolder") from its first command. A save and an open by name use the document folder
// ("Desktop:DocumentFolder"). The store of the assets is the folder of the setting "Engine:AssetFolder", or the default.
//
// The desktop is a composition root, so it chooses the native backend here (ADR-0014 section 4). As each host does, it
// stops when the native library does not load (TASK-0036).
internal sealed class DesktopSession : IDisposable
{
    private DesktopSession(EngineHost host, DesktopDocument document, AssetStore assets)
    {
        Host = host;
        Document = document;
        Assets = assets;
    }

    // The session of the document.
    public EngineHost Host { get; }

    // The document folder and the open of a file (ADR-0030 items 3 to 5).
    public DesktopDocument Document { get; }

    // ADR-0046 item 2: the store of the assets, so the window and an agent read the same bytes.
    public AssetStore Assets { get; }

    // The session for the settings, or null with an exit code and a message. A file that is not null replaces the
    // setting "file": the host gives the file of an open.
    public static (DesktopSession? Session, int ExitCode, string? Message) Open(IConfiguration configuration, string? file = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!ManifoldGeometryBackend.IsNativeAvailable())
            return (null, DesktopSurface.ExitBackendUnavailable, $"{DiagnosticCodes.GeomBackendInit}: {ManifoldGeometryBackend.UnavailableReason()}");

        var document = new DesktopDocument(configuration[DesktopSurface.DocumentFolderSetting] is { Length: > 0 } documents ? documents : DocumentFolder.Default);
        var options = new DocumentFileOptions(
            file ?? (configuration[DesktopSurface.FileSetting] is { Length: > 0 } named ? named : null),
            configuration[DesktopSurface.UnsavedFolderSetting] is { Length: > 0 } unsaved ? unsaved : DocumentFile.DefaultUnsavedFolder);
        var assets = new AssetStore(configuration["Engine:AssetFolder"] is { Length: > 0 } folder ? folder : AssetStore.Default);

        EngineHost host;
        try
        {
            host = new EngineHost(new ManifoldGeometryBackend(), options);
        }
        catch (IOException ex)
        {
            // ADR-0030 item 1: a DocumentFileException gives its code in its message.
            return (null, DesktopSurface.ExitFileNotOpened, $"The document file did not open: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, DesktopSurface.ExitStartFailed, $"The surface did not start: {ex.Message}");
        }

        foreach (var warning in host.OpenWarnings)
            Console.Error.WriteLine($"{warning.Code}: {warning.Message}");
        if (host.NewFileMessage is { } made)
            Console.Error.WriteLine(made);
        document.HeldFile = () => host.Session.FilePath;
        return (new DesktopSession(host, document, assets), 0, null);
    }

    // The session closes first and waits for a command that still runs, then the backend is freed (EngineHost.Dispose).
    public void Dispose() => Host.Dispose();
}
