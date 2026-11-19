using System.Globalization;
using Base.Contracts.Message;
using Base.Message;
using Contracts.Application;
using Contracts.External;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public class RabbitMqInvoiceParseRequestPublisher(
    [FromKeyedServices(RabbitMqPublisherKeys.AiHub)] IBaseEventPublisher publisher,
    ILogger<RabbitMqInvoiceParseRequestPublisher> logger,
    TimeProvider timeProvider) : IInvoiceParseRequestPublisher
{
    public async Task<InvoiceParseRequestResult> PublishAsync(
        InvoiceParseCommandContent content,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        var envelope = BaseEventEnvelope<InvoiceParseCommandContent>.CreateCommand(
            MessagingContract.InvoiceSource,
            MessagingContract.InvoiceTenant,
            MessagingContract.InvoiceParseAction,
            MessagingContract.InvoiceParseContentVersion,
            content,
            expiresAt);

        // The broker drops the command once its link has expired instead of handing ai-hub a dead link.
        var timeToLive = Math.Max(1, (long)(expiresAt - timeProvider.GetUtcNow()).TotalMilliseconds);
        var result = await publisher.PublishAsync(envelope, timeToLive.ToString(CultureInfo.InvariantCulture), ct);

        // Identity only: the content carries a signed link that must never reach the logs.
        if (!result.Success)
        {
            logger.LogError(
                "Invoice parse request not confirmed (id={Id}, documentId={DocumentId}): {Reason}",
                envelope.Id, content.DocumentId, result.FailureReason);
            return InvoiceParseRequestResult.Failed(result.FailureReason ?? "Publish was not confirmed by the broker.");
        }

        logger.LogInformation(
            "Invoice parse request confirmed (id={Id}, documentId={DocumentId}).", envelope.Id, content.DocumentId);
        return InvoiceParseRequestResult.Ok();
    }
}
