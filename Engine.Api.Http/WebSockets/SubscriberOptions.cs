namespace Engine.Api.Http.WebSockets;

// Tunables for Subscriber. Defaults match ADR-0005 / TASK-0010 §6 (1024-deep
// outbound queue, 30 s idle heartbeat). PumpGate is a test seam, and production
// keeps it null: the pump awaits the task of the gate before each send, so a test
// holds the pump and fills the outbound queue with a count and no timing. Until
// TASK-0154 the seam was a delay of the pump, and the lag test failed on a slow
// runner, whose posts came slower than the delay (register entry R-0047).
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
    Func<CancellationToken, Task>? PumpGate)
{
    public static SubscriberOptions Default { get; } = new(
        ChannelCapacity: Subscriber.DefaultChannelCapacity,
        HeartbeatInterval: Subscriber.DefaultHeartbeatInterval,
        PumpGate: null);

    public IReadOnlyList<string> PermittedOriginHosts { get; init; } = ["localhost", "127.0.0.1", "[::1]"];
}
