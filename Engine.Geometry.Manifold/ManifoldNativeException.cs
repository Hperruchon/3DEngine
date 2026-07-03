namespace Engine.Geometry.Manifold;

// Thrown when a native Manifold operation fails or returns a degenerate result.
// The command/query handlers translate this to E-GEOM-NATIVE-OP once that code is
// registered and the diagnostic-code home is settled (TASK-0012 §5). Skeleton stage:
// carries context only.
public sealed class ManifoldNativeException : Exception
{
    public ManifoldNativeException(string message) : base(message) { }

    public ManifoldNativeException(string message, Exception inner) : base(message, inner) { }
}
