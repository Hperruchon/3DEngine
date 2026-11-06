using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Org.Libsdl.App;

namespace ThreeDEngine.Android;

// Phase R14e2 (ADR-0061 item 9, TASK-0226): the activity of the program. SDLActivity of ppy.SDL3-CS loads the native
// SDL and runs Main on its own thread, the main thread of SDL, as Program.Main runs the desktop. The activity keeps
// itself through a change of the screen, so a rotation gives a resize of the window and not a new activity.
// Phase R14e3 (ADR-0073 item 4, TASK-0227): the screen stays in portrait until the slice of the pre-rotation, because
// the swapchain takes the transform of the surface and the renderer does not turn its picture yet.
// Phase R14f3 (ADR-0074, TASK-0233): a fixed name of the activity, so that the pipeline starts it with the extra "probe".
// ADR-0079 (TASK-0247): the activity may start again in its process, and it names the weight of the font and the
// grammatical gender as changes that it takes itself.
[Activity(Name = "org.threedengine.phone.MainActivity", Label = "3DEngine", MainLauncher = true, Exported = true, ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation
        | ConfigChanges.UiMode | ConfigChanges.Density | ConfigChanges.FontScale | ConfigChanges.Locale
        | ConfigChanges.LayoutDirection | ConfigChanges.ColorMode | ConfigChanges.Touchscreen | ConfigChanges.Mcc
        | ConfigChanges.Mnc | ConfigChanges.FontWeightAdjustment | ConfigChanges.GrammaticalGender)]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    // Finding V105 of the codebase review of 2026-11-06 (d) (TASK-0243): on a freshly booted emulator the process ended
    // with the code 0 and no line, which a new start of the activity explains: SDL ends the process when an activity
    // starts again in it. Each create, change of the configuration and destroy writes a line, so that the next failure
    // names its cause; the configuration changes above hold each change that the emulator gave no line for.
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Console.Error.WriteLine($"3DEngine: the activity is created{(savedInstanceState is null ? string.Empty : ", with a saved state")}.");
        base.OnCreate(savedInstanceState);

        // ADR-0079 item 1 (TASK-0247): SDLActivity loaded SDL in base.OnCreate, so the hint can be set now; a later create of
        // the activity in this process then goes on, where SDL ended the process with the code 0 (run 42289169473).
        PhoneHost.AllowNewStart();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        Console.Error.WriteLine($"3DEngine: the configuration of the activity changed: {newConfig}.");
        base.OnConfigurationChanged(newConfig);
    }

    protected override void OnDestroy()
    {
        Console.Error.WriteLine("3DEngine: the activity is destroyed.");
        base.OnDestroy();
    }

    // The extra "probe" runs the probe of the determinism hash before the session (ADR-0074 item 3).
    protected override void Main() => PhoneHost.Run(Intent?.GetBooleanExtra("probe", false) ?? false);
}
