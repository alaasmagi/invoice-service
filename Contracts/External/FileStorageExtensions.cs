namespace Contracts.External;

public static class FileStorageExtensions
{
    public static readonly TimeSpan DefaultSignedUrlExpiry = TimeSpan.FromMinutes(15);

    public static Task<string> GetSignedUrlAsync(this IFileStorage storage, string key, CancellationToken ct = default)
    {
        return storage.GetSignedUrlAsync(key, DefaultSignedUrlExpiry, ct);
    }
}
