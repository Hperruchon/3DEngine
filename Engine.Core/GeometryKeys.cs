using System.Security.Cryptography;

namespace Engine.Core;

// ADR-0037 item 3: the key of a solid that a command makes for a body. It is a name-based
// guid, version 5 of RFC 9562, of the identifier of the command and the identifier of the
// body, so a replay gives the same key on each platform: SHA-1 and the byte order are
// integer operations only. An extrude stores its solid under Derive(command, command), and
// a rebuild under Derive(edit, body); no key is ever a body identifier, so a read of a solid
// by the body identifier fails loudly (ADR-0037 item 3).
public static class GeometryKeys
{
    // The namespace of the keys of this engine, a fixed random guid.
    private static readonly Guid Namespace = new("6f0a3c52-8d1e-4b7a-9c35-2e41d7f08b96");

    public static Guid Derive(Guid command, Guid body)
    {
        Span<byte> name = stackalloc byte[48];
        Namespace.TryWriteBytes(name[..16], bigEndian: true, out _);
        command.TryWriteBytes(name[16..32], bigEndian: true, out _);
        body.TryWriteBytes(name[32..], bigEndian: true, out _);

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(name, hash);

        // RFC 9562, section 5.5: the version in the high four bits of octet 6, and the
        // variant 10 in the high two bits of octet 8.
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash[..16], bigEndian: true);
    }
}
