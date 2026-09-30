namespace Engine.Api.Http.WebSockets;

// Tunables for Subscriber. Defaults match ADR-0005 / TASK-0010 §6 (1024-deep
// outbound queue, 30 s idle heartbeat). PumpDelay is a test seam — production
// keeps it at TimeSpan.Zero. Setting it non-zero slows the pump deterministically
// so tests can saturate the outbound queue and exercise the lag path without
// timing flakiness.
//
// PermittedOriginHosts lists the hosts that a browser Origin header may name on
// a WebSocket upgrade. The host serves a loopback address only (ADR-0019 §5),
// therefore the list holds the loopback names. A page from any other origin gets
// 403, so that a web page on the same computer cannot read the event stream
// (TASK-0044). The desktop host of ADR-0019 adds its own origin here and not in
// a literal.
internal sealed record SubscriberOptions(
    int ChannelCapacity,
    TimeSpan HeartbeatInterval,
    TimeSpan PumpDelay)
{
    public static SubscriberOptions Default { get; } = new(
        ChannelCapacity: Subscriber.DefaultChannelCapacity,
        HeartbeatInterval: Subscriber.DefaultHeartbeatInterval,
        PumpDelay: TimeSpan.Zero);

    public IReadOnlyList<string> PermittedOriginHosts { get; init; } = ["localhost", "127.0.0.1", "[::1]"];
}
