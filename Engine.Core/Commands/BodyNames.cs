namespace Engine.Core.Commands;

// The rules of the name of a body (ADR-0028 item 3, TASK-0085). A name has 1 to 128 UTF-16
// units and no control character. The engine does not trim it, does not normalize it, and
// compares it with no rules of a culture; two bodies can have one name. An empty name clears
// the name of the body, so it is no name and not a name of length 0.
public static class BodyNames
{
    public const int MaximumLength = 128;

    // The reason to refuse a name, or null when the name is valid or empty.
    public static string? Refusal(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length > MaximumLength)
            return $"A name has at most {MaximumLength} UTF-16 units, and this one has {name.Length}.";

        for (var index = 0; index < name.Length; index++)
        {
            if (char.IsControl(name[index]))
                return $"A name has no control character, and this one has U+{(int)name[index]:X4} at index {index}.";
        }

        return null;
    }
}
