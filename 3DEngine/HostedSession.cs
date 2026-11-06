using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;

namespace ThreeDEngine.Desktop;

// ADR-0072 item 2 (phase R14e1, TASK-0222): the EngineHost of the surface library as the session of the application.
// The desktop serves the surface on the same host, so the person and an agent work on one document (ADR-0019).
internal sealed class HostedSession(EngineHost host) : ISessionHost
{
    public EngineHost Host { get; } = host;

    public DocumentSession Session => Host.Session;

    public string? FileName => Host.FileName;

    public IDisposable Subscribe(Action<EventRecord> action) => Host.Subscribe(action);
}
