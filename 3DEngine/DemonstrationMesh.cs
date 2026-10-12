namespace ThreeDEngine.Desktop;

// The triangles of the cut body of the first demonstration (objective 6 of
// docs/CHARTER.md): two 10 x 10 x 10 boxes, the second moved by (5, 0, 0) and
// subtracted. TASK-0028 recorded this output of the query GetTessellation in its
// Outcome block. Roadmap phase R3 draws it as a fixed mesh (TASK-0069); phase R5
// replaces it with a mesh that the engine gives through its events and queries.
internal static class DemonstrationMesh
{
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

    // The centre of the bounding box, and the scale that puts each corner of the
    // box on the unit sphere. Both are presentation.
    public static ((double X, double Y, double Z) Origin, double Scale) Frame()
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

        var (dx, dy, dz) = ((maxX - minX) / 2, (maxY - minY) / 2, (maxZ - minZ) / 2);
        return (((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2), 1 / Math.Sqrt(dx * dx + dy * dy + dz * dz));
    }
}
