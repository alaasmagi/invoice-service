using Base.Contracts.Message;
using Base.Message;
using Contracts.External;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests;

public class RabbitMqInvoiceParseRequestPublisherTests
{
    private const string SignedUrl = "https://signed.example/file.pdf?X-Amz-Signature=secret";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly RecordingPublisher _publisher = new();
    private readonly ListLogger _logger = new();

    private RabbitMqInvoiceParseRequestPublisher CreateSut() => new(_publisher, _logger, new FixedTimeProvider(Now));

    private static InvoiceParseCommandContent Content() => new()
    {
        DocumentId = Guid.NewGuid().ToString(),
        FileUrl = SignedUrl,
        FileName = "telia.pdf",
        Sha256 = new string('a', 64)
    };

    [Fact]
    public async Task Publishes_InvoiceParseCommand_ThatExpiresWithTheLink()
    {
        var expiresAt = Now.AddHours(1);

        var result = await CreateSut().PublishAsync(Content(), expiresAt);

        Assert.True(result.Confirmed);
        var envelope = _publisher.LastEnvelope!;
        Assert.Equal("invoice-service.invoice-service.invoice-parse", BaseRoutingKey.For(envelope));
        Assert.Equal(expiresAt, envelope.ExpiresAt);
        Assert.Equal("1.0", envelope.ContentVersion);
        Assert.Equal("3600000", _publisher.LastExpiration);
    }

    [Fact]
    public async Task UnconfirmedPublish_IsReportedAsFailed()
    {
        _publisher.Result = PublishResult.Failed("unroutable");

        var result = await CreateSut().PublishAsync(Content(), Now.AddHours(1));

        Assert.False(result.Confirmed);
        Assert.Equal("unroutable", result.FailureReason);
    }

    [Fact]
    public async Task SignedLink_IsNeverLogged()
    {
        await CreateSut().PublishAsync(Content(), Now.AddHours(1));
        _publisher.Result = PublishResult.Failed("unroutable");
        await CreateSut().PublishAsync(Content(), Now.AddHours(1));

        Assert.Equal(2, _logger.Messages.Count);
        Assert.All(_logger.Messages, message => Assert.DoesNotContain("signed.example", message));
    }

    private class RecordingPublisher : IBaseEventPublisher
    {
        public PublishResult Result { get; set; } = PublishResult.Ok();
        public IBaseEventEnvelope<InvoiceParseCommandContent>? LastEnvelope { get; private set; }
        public string? LastExpiration { get; private set; }

        public Task<PublishResult> PublishAsync<TContent>(
            IBaseEventEnvelope<TContent> message,
            string? expiration = null,
            CancellationToken cancellationToken = default)
        {
            LastEnvelope = message as IBaseEventEnvelope<InvoiceParseCommandContent>;
            LastExpiration = expiration;
            return Task.FromResult(Result);
        }
    }

    private class ListLogger : ILogger<RabbitMqInvoiceParseRequestPublisher>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
