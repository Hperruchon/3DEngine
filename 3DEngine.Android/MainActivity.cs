using Android.App;
using Android.Content.PM;
using Org.Libsdl.App;

namespace ThreeDEngine.Android;

// Phase R14e2 (ADR-0061 item 9, TASK-0226): the activity of the program. SDLActivity of ppy.SDL3-CS loads the native
// SDL and runs Main on its own thread, the main thread of SDL, as Program.Main runs the desktop. The activity keeps
// itself through a change of the screen, so a rotation gives a resize of the window and not a new activity.
// Phase R14e3 (ADR-0073 item 4, TASK-0227): the screen stays in portrait until the slice of the pre-rotation, because
// the swapchain takes the transform of the surface and the renderer does not turn its picture yet.
// Phase R14f3 (ADR-0074, TASK-0233): a fixed name of the activity, so that the pipeline starts it with the extra "probe".
[Activity(Name = "org.threedengine.phone.MainActivity", Label = "3DEngine", MainLauncher = true, Exported = true, ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation
        | ConfigChanges.UiMode)]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    // The extra "probe" runs the probe of the determinism hash before the session (ADR-0074 item 3).
    protected override void Main() => PhoneHost.Run(Intent?.GetBooleanExtra("probe", false) ?? false);
}
