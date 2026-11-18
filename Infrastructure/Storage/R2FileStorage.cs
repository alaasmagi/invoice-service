using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Contracts.External;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Infrastructure.Storage;

public class R2FileStorage(
    IAmazonS3 client,
    StorageOptions options,
    ILogger<R2FileStorage> logger,
    TimeProvider? timeProvider = null) : IFileStorage
{
    // SigV4 presigned URLs cannot outlive seven days.
    public static readonly TimeSpan MaxSignedUrlExpiry = TimeSpan.FromDays(7);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public static AmazonS3Config CreateClientConfig(StorageOptions options)
    {
        return new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            // R2 does not implement the SDK's default flexible checksums; only send them when an operation requires one.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };
    }

    public async Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        StorageKey.Validate(key);

        // R2 rejects the SDK's chunked streaming upload, so the length must be known up front.
        var body = content;
        if (!content.CanSeek)
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            body = buffer;
        }

        try
        {
            await Execute("Upload", key, ct, () => client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = options.BucketName,
                Key = key,
                InputStream = body,
                ContentType = contentType,
                AutoCloseStream = false,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            }, ct));
        }
        finally
        {
            if (!ReferenceEquals(body, content))
            {
                await body.DisposeAsync();
            }
        }

        logger.LogInformation("Storage upload succeeded (key={Key}).", key);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        StorageKey.Validate(key);

        var response = await Execute("Download", key, ct, () => client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.BucketName,
            Key = key
        }, ct));

        logger.LogDebug("Storage download opened (key={Key}).", key);
        // Disposing the response stream releases the underlying connection.
        return response.ResponseStream;
    }

    public async Task<string> GetSignedUrlAsync(string key, TimeSpan expiresIn, CancellationToken ct = default)
    {
        StorageKey.Validate(key);
        if (expiresIn <= TimeSpan.Zero || expiresIn > MaxSignedUrlExpiry)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresIn), expiresIn,
                $"Signed URL expiry must be greater than zero and at most {MaxSignedUrlExpiry.TotalDays} days.");
        }

        var url = await Execute("GetSignedUrl", key, ct, () => client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = _timeProvider.GetUtcNow().Add(expiresIn).UtcDateTime
        }));

        // The URL carries the access key id and a bearer signature, so only the key is logged.
        logger.LogDebug("Storage signed URL issued (key={Key}, expiresIn={ExpiresIn}).", key, expiresIn);
        return url;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        StorageKey.Validate(key);

        try
        {
            await Execute("Exists", key, ct, () => client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = options.BucketName,
                Key = key
            }, ct));
            return true;
        }
        catch (FileStorageObjectNotFoundException)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        StorageKey.Validate(key);

        await Execute("Delete", key, ct, () => client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = options.BucketName,
            Key = key
        }, ct));

        logger.LogInformation("Storage delete succeeded (key={Key}).", key);
    }

    private async Task<T> Execute<T>(string operation, string key, CancellationToken ct, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (TryTranslate(operation, key, exception, out var storageException))
        {
            if (storageException is FileStorageObjectNotFoundException)
            {
                logger.LogDebug("Storage {Operation} found no object (key={Key}).", operation, key);
            }
            else
            {
                logger.LogWarning(exception, "Storage {Operation} failed (key={Key}).", operation, key);
            }

            throw storageException;
        }
    }

    private static bool TryTranslate(string operation, string key, Exception exception, out FileStorageException storageException)
    {
        switch (exception)
        {
            case AmazonServiceException service when service.StatusCode == HttpStatusCode.NotFound ||
                                                     service.ErrorCode is "NoSuchKey" or "NotFound":
                storageException = new FileStorageObjectNotFoundException(operation, key, exception);
                return true;
            case AmazonServiceException service:
                storageException = new FileStorageException(operation, key,
                    $"Storage {operation} failed for key '{key}' ({(int)service.StatusCode} {service.ErrorCode}).",
                    exception);
                return true;
            case AmazonClientException:
            case HttpRequestException:
            case IOException:
            case OperationCanceledException: // HTTP timeout; caller cancellation is rethrown before this.
                storageException = new FileStorageException(operation, key,
                    $"Storage {operation} failed for key '{key}': the storage service could not be reached.",
                    exception);
                return true;
            default:
                storageException = null!;
                return false;
        }
    }
}
