using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Engine.Core.Enum;

public static class EnumBaseId
{
    private static readonly ConcurrentDictionary<Type, long> Cache = new();

    public static long GetFor(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        if (!enumType.GetTypeInfo().IsEnum)
            throw new ArgumentException("Type must be an enum.", nameof(enumType));

        return Cache.GetOrAdd(enumType, Compute);
    }

    private static long Compute(Type enumType)
    {
        // Assembly identity (omit version to keep stability across releases)
        var an = enumType.GetTypeInfo().Assembly.GetName();
        var name    = an.Name ?? string.Empty;
        var culture = an.CultureName ?? string.Empty;
        var pkt     = an.GetPublicKeyToken() ?? [];

        var pktHex = ToLowerHex(pkt);
        var asmKey = name + "|" + culture + "|" + pktHex;

        // Fully-qualified metadata-like name (nested types as dots)
        var fullName = enumType.FullName ?? enumType.Name;
        fullName = fullName.Replace('+', '.');
        var fq = "global::" + fullName;

        var key = asmKey + "||" + fq;
        return (long)Fnv1A64(Encoding.UTF8.GetBytes(key));
    }

    private static string ToLowerHex(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return string.Empty;
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var t in bytes)
            sb.Append(t.ToString("x2")); // lower-case hex

        return sb.ToString();
    }

    // FNV-1a 64-bit (deterministic on all platforms)
    private static ulong Fnv1A64(byte[] data)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime  = 1099511628211UL;

        var hash = offset;
        foreach (var t in data)
        {
            hash ^= t;
            hash *= prime;
        }
        return hash;
    }
}