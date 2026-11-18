using System.Text;

namespace Infrastructure.Storage;

public static class StorageKey
{
    // S3 and R2 limit object keys to 1024 bytes of UTF-8.
    public const int MaxLengthInBytes = 1024;

    public static string Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Storage key must not be empty.", nameof(key));
        }

        if (key.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException("Storage key must be relative, not an absolute URL.", nameof(key));
        }

        if (key.StartsWith('/') || key.EndsWith('/'))
        {
            throw new ArgumentException("Storage key must not start or end with '/'.", nameof(key));
        }

        if (key.Contains('\\') || key.Any(char.IsControl))
        {
            throw new ArgumentException("Storage key must not contain backslashes or control characters.", nameof(key));
        }

        foreach (var segment in key.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                throw new ArgumentException("Storage key must not contain empty, '.' or '..' segments.", nameof(key));
            }
        }

        if (Encoding.UTF8.GetByteCount(key) > MaxLengthInBytes)
        {
            throw new ArgumentException($"Storage key must be at most {MaxLengthInBytes} bytes.", nameof(key));
        }

        return key;
    }
}
