using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// The scene of a session at the start: its orbit camera only. SessionBodies brings
// each live body of the session into it (roadmap phase R5). The desktop and the image
// test start from this one scene (TASK-0076).
internal static class SessionScene
{
    public static Scene Create()
    {
        var scene = new Scene { Name = "Session" };
        scene.Cameras.Add(new Camera { Name = "Orbit camera", Distance = 40, YawDegrees = 30, PitchDegrees = 20 });
        return scene;
    }
}
