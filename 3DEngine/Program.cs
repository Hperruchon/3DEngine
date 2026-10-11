namespace ThreeDEngine.Desktop;

public static class Program
{
    // The window and its event loop stay on the main thread, which SDL needs, so
    // Main is not async.
    public static int Main(string[] args) => DesktopEngineHost.Run(args);
}
