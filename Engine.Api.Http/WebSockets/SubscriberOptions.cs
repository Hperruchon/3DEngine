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

    // ADR-0005 section 5 (finding E17 of the codebase review of 2026-09-30, part 6, TASK-0481): the time that the endpoint waits
    // for the subscribe frame after the upgrade. A frame that does not come in it is missing, and the endpoint closes with
    // 1007 and E-API-WS-INVALID-SUBSCRIBE. A client sends the frame at once after the upgrade. TASK-0484 (main run 46113600404):
    // the limit is the interval of the heartbeat, 30 seconds. With 10 seconds a test of the test server that sent its frame at
    // once got the close on a Windows runner, probably because the threads of the pool were busy for more than 10 seconds.
    public TimeSpan SubscribeTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
