using Android.App;
using Android.Content.PM;
using Org.Libsdl.App;

namespace ThreeDEngine.Android;

// Phase R14e2 (ADR-0061 item 9, TASK-0226): the activity of the program. SDLActivity of ppy.SDL3-CS loads the native
// SDL and runs Main on its own thread, the main thread of SDL, as Program.Main runs the desktop. The activity keeps
// itself through a change of the screen, so a rotation gives a resize of the window and not a new activity.
[Activity(Label = "3DEngine", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation
        | ConfigChanges.UiMode)]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main() => PhoneHost.Run();
}
