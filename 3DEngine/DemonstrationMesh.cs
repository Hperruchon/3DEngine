using System.Numerics;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// The triangles of the cut body of the first demonstration (objective 6 of
// docs/CHARTER.md): two 10 x 10 x 10 boxes, the second moved by (5, 0, 0) and
// subtracted. TASK-0028 recorded this output of the query GetTessellation in its
// Outcome block. Phases R3 and R4 draw it as a fixed mesh (TASK-0069, TASK-0070);
// phase R5 replaces it with a mesh that the engine gives through its events and
// queries.
internal static class DemonstrationMesh
{
    public const string MeshId = "demonstration-cut";

    public static readonly double[] Positions =
    [
        -5, -5, -5, -5, -5, 5, -5, 5, -5, -5, 5, 5,
        0, -5, -5, 0, -5, 5, 0, 5, -5, 0, 5, 5,
    ];

    public static readonly uint[] Indices =
    [
        0, 2, 4, 0, 1, 3, 0, 5, 1, 0, 3, 2, 2, 3, 6, 1, 7, 3,
        0, 4, 5, 1, 5, 7, 2, 6, 4, 3, 7, 6, 5, 4, 7, 6, 7, 4,
    ];

    // The centre of the bounding box; the mesh is narrowed relative to it.
    public static (double X, double Y, double Z) Centre()
    {
        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
        for (var i = 0; i < Positions.Length; i += 3)
        {
            minX = Math.Min(minX, Positions[i]);
            maxX = Math.Max(maxX, Positions[i]);
            minY = Math.Min(minY, Positions[i + 1]);
            maxY = Math.Max(maxY, Positions[i + 1]);
            minZ = Math.Min(minZ, Positions[i + 2]);
            maxZ = Math.Max(maxZ, Positions[i + 2]);
        }

        return ((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
    }

    // The scene of R4: the cut body at its own place, and a second copy behind it and
    // above it, so that the depth test decides which one a person sees where they
    // overlap. The camera looks at the point between them.
    public static Scene CreateScene()
    {
        var centre = Centre();
        var scene = new Scene { Name = "First demonstration" };
        scene.Entities.Add(new Entity
        {
            Name = "Cut body",
            MeshId = MeshId,
            Transform = Transform.Identity with { Position = new Vector3((float)centre.X, (float)centre.Y, (float)centre.Z) },
        });
        scene.Entities.Add(new Entity
        {
            Name = "Second copy",
            MeshId = MeshId,
            Transform = Transform.Identity with { Position = new Vector3(2.0f, 4.0f, -7.0f) },
        });
        scene.Cameras.Add(new Camera
        {
            Name = "Orbit camera",
            Target = (0, 2, -3.5),
            Distance = 40,
            YawDegrees = 30,
            PitchDegrees = 20,
        });
        return scene;
    }
}
