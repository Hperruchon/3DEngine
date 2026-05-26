namespace Engine.Api.Http.WebSockets;

// Tunables for Subscriber. Defaults match ADR-0005 / TASK-0010 §6 (1024-deep
// outbound queue, 30 s idle heartbeat). PumpDelay is a test seam — production
// keeps it at TimeSpan.Zero. Setting it non-zero slows the pump deterministically
// so tests can saturate the outbound queue and exercise the lag path without
// timing flakiness.
internal sealed record SubscriberOptions(
    int ChannelCapacity,
    TimeSpan HeartbeatInterval,
    TimeSpan PumpDelay)
{
    public static SubscriberOptions Default { get; } = new(
        ChannelCapacity: Subscriber.DefaultChannelCapacity,
        HeartbeatInterval: Subscriber.DefaultHeartbeatInterval,
        PumpDelay: TimeSpan.Zero);
}
