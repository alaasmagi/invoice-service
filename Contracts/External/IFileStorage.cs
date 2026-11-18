namespace Contracts.External;

public interface IFileStorage
{
    Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken ct = default);

    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);

    Task<string> GetSignedUrlAsync(string key, TimeSpan expiresIn, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
