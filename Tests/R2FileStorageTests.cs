using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Contracts.External;
using Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace Tests;

public class R2FileStorageTests
{
    private const string Key = "8a6f0c3e-2b1d-4c5e-9f7a-1b2c3d4e5f60/2026/abc.pdf";
    private const string SecretAccessKey = "super-secret-value";
    private const string AccessKeyId = "access-key-id-value";

    private static readonly StorageOptions Options = new()
    {
        Endpoint = "https://account.r2.cloudflarestorage.com",
        AccessKeyId = AccessKeyId,
        SecretAccessKey = SecretAccessKey,
        BucketName = "invoice-service"
    };

    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IAmazonS3> _client = new(MockBehavior.Strict);
    private readonly ListLogger<R2FileStorage> _logger = new();

    private R2FileStorage CreateSut() => new(_client.Object, Options, _logger, new FixedTimeProvider(Now));

    private static AmazonS3Exception S3Error(HttpStatusCode status, string errorCode) =>
        new($"AWS says {errorCode}", ErrorType.Sender, errorCode, "request-id", status);

    [Fact]
    public void CreateClientConfig_UsesPathStyleAgainstConfiguredEndpoint_WithAutoRegion()
    {
        var config = R2FileStorage.CreateClientConfig(Options);

        Assert.True(config.ForcePathStyle);
        Assert.StartsWith(Options.Endpoint, config.ServiceURL);
        Assert.Equal("auto", config.AuthenticationRegion);
    }

    [Fact]
    public async Task UploadAsync_PutsObjectUnderCallerKey_AndReturnsKey()
    {
        PutObjectRequest? sent = null;
        _client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new PutObjectResponse());
        using var content = new MemoryStream([1, 2, 3]);

        var stored = await CreateSut().UploadAsync(content, Key, "application/pdf");

        Assert.Equal(Key, stored);
        Assert.NotNull(sent);
        Assert.Equal("invoice-service", sent.BucketName);
        Assert.Equal(Key, sent.Key);
        Assert.Equal("application/pdf", sent.ContentType);
        Assert.Same(content, sent.InputStream);
        Assert.False(sent.AutoCloseStream);
        Assert.True(content.CanRead); // The caller still owns its stream.
    }

    [Fact]
    public async Task UploadAsync_BuffersNonSeekableStream()
    {
        byte[]? uploaded = null;
        _client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) =>
            {
                Assert.True(request.InputStream.CanSeek);
                using var copy = new MemoryStream();
                request.InputStream.CopyTo(copy);
                uploaded = copy.ToArray();
            })
            .ReturnsAsync(new PutObjectResponse());

        await CreateSut().UploadAsync(new NonSeekableStream([4, 5, 6]), Key, "application/pdf");

        Assert.Equal(new byte[] { 4, 5, 6 }, uploaded);
    }

    [Fact]
    public async Task UploadAsync_WrapsS3Error_InStorageException()
    {
        var s3Error = S3Error(HttpStatusCode.Forbidden, "AccessDenied");
        _client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(s3Error);

        var exception = await Assert.ThrowsAsync<FileStorageException>(() =>
            CreateSut().UploadAsync(new MemoryStream([1]), Key, "application/pdf"));

        Assert.Equal("Upload", exception.Operation);
        Assert.Equal(Key, exception.Key);
        Assert.Same(s3Error, exception.InnerException);
        Assert.Contains("403", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "NoSuchKey")]
    [InlineData(HttpStatusCode.NotFound, "NotFound")]
    public async Task DownloadAsync_MissingObject_ThrowsNotFound(HttpStatusCode status, string errorCode)
    {
        _client.Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(S3Error(status, errorCode));

        var exception = await Assert.ThrowsAsync<FileStorageObjectNotFoundException>(() =>
            CreateSut().DownloadAsync(Key));

        Assert.Equal("Download", exception.Operation);
        Assert.Equal(Key, exception.Key);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsResponseStream()
    {
        var body = new MemoryStream([7, 8]);
        _client.Setup(c => c.GetObjectAsync(
                It.Is<GetObjectRequest>(r => r.BucketName == "invoice-service" && r.Key == Key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = body });

        await using var stream = await CreateSut().DownloadAsync(Key);

        Assert.Same(body, stream);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenMetadataFound()
    {
        _client.Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse());

        Assert.True(await CreateSut().ExistsAsync(Key));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_OnNotFound()
    {
        // HEAD responses carry no body, so R2 reports a bare 404 without an error code.
        _client.Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(S3Error(HttpStatusCode.NotFound, ""));

        Assert.False(await CreateSut().ExistsAsync(Key));
    }

    [Fact]
    public async Task ExistsAsync_ThrowsStorageException_OnOtherErrors()
    {
        _client.Setup(c => c.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(S3Error(HttpStatusCode.InternalServerError, "InternalError"));

        var exception = await Assert.ThrowsAsync<FileStorageException>(() => CreateSut().ExistsAsync(Key));

        Assert.IsNotType<FileStorageObjectNotFoundException>(exception);
        Assert.Equal("Exists", exception.Operation);
    }

    [Fact]
    public async Task DeleteAsync_WrapsTransportFailure()
    {
        var transportError = new HttpRequestException("connection refused");
        _client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(transportError);

        var exception = await Assert.ThrowsAsync<FileStorageException>(() => CreateSut().DeleteAsync(Key));

        Assert.Equal("Delete", exception.Operation);
        Assert.Same(transportError, exception.InnerException);
    }

    [Fact]
    public async Task DeleteAsync_WrapsSdkClientFailure()
    {
        _client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonClientException("unable to sign"));

        var exception = await Assert.ThrowsAsync<FileStorageException>(() => CreateSut().DeleteAsync(Key));

        Assert.IsType<AmazonClientException>(exception.InnerException);
    }

    [Fact]
    public async Task Operations_WrapHttpTimeout_WhenCallerDidNotCancel()
    {
        _client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("HTTP timeout"));

        await Assert.ThrowsAsync<FileStorageException>(() => CreateSut().DeleteAsync(Key));
    }

    [Fact]
    public async Task Operations_PropagateCallerCancellation_Unwrapped()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(() => CreateSut().DeleteAsync(Key, cts.Token));
    }

    [Fact]
    public async Task GetSignedUrlAsync_DefaultsToFifteenMinutes()
    {
        GetPreSignedUrlRequest? sent = null;
        _client.Setup(c => c.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => sent = request)
            .ReturnsAsync("https://signed.example/abc");

        IFileStorage storage = CreateSut();
        var url = await storage.GetSignedUrlAsync(Key);

        Assert.Equal("https://signed.example/abc", url);
        Assert.NotNull(sent);
        Assert.Equal(HttpVerb.GET, sent.Verb);
        Assert.Equal(Key, sent.Key);
        Assert.Equal(Now.AddMinutes(15).UtcDateTime, sent.Expires);
    }

    [Fact]
    public async Task GetSignedUrlAsync_UsesCallerExpiry()
    {
        GetPreSignedUrlRequest? sent = null;
        _client.Setup(c => c.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => sent = request)
            .ReturnsAsync("https://signed.example/abc");

        await CreateSut().GetSignedUrlAsync(Key, TimeSpan.FromHours(2));

        Assert.Equal(Now.AddHours(2).UtcDateTime, sent!.Expires);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    [InlineData(7 * 24 * 60 * 60 + 1)]
    public async Task GetSignedUrlAsync_RejectsOutOfRangeExpiry(int seconds)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateSut().GetSignedUrlAsync(Key, TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData("https://account.r2.cloudflarestorage.com/invoice-service/a.pdf")]
    [InlineData("/user/2026/a.pdf")]
    [InlineData("")]
    public async Task Operations_RejectInvalidKeys_WithoutCallingS3(string key)
    {
        // Strict mock: any S3 call would fail the test.
        await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().DeleteAsync(key));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().ExistsAsync(key));
    }

    [Fact]
    public async Task Logs_KeyAndOperation_NeverCredentialsOrSignedUrl()
    {
        const string signedUrl = "https://signed.example/abc?X-Amz-Credential=" + AccessKeyId + "&X-Amz-Signature=sig";
        _client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        _client.Setup(c => c.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(signedUrl);
        _client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(S3Error(HttpStatusCode.Forbidden, "AccessDenied"));
        var sut = CreateSut();

        await sut.UploadAsync(new MemoryStream([1]), Key, "application/pdf");
        await sut.GetSignedUrlAsync(Key, TimeSpan.FromMinutes(5));
        await Assert.ThrowsAsync<FileStorageException>(() => sut.DeleteAsync(Key));

        Assert.Contains(_logger.Messages, m => m.Contains(Key) && m.Contains("upload"));
        Assert.Contains(_logger.Messages, m => m.Contains(Key) && m.Contains("Delete"));
        Assert.All(_logger.Messages, m =>
        {
            Assert.DoesNotContain(SecretAccessKey, m);
            Assert.DoesNotContain(AccessKeyId, m);
            Assert.DoesNotContain("X-Amz-Signature", m);
        });
    }

    private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}
