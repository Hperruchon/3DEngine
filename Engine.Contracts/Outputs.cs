namespace Engine.Contracts;

public sealed record Outputs(IReadOnlyDictionary<string, object?> Values)
{
    public static Outputs Empty { get; } = new(new Dictionary<string, object?>());

    public bool TryGet<T>(string key, out T? value)
    {
        if (Values.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }
}
