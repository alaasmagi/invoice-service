using Base.Contracts.Message;
using Contracts.Application;
using Contracts.External;

namespace Infrastructure.Messaging;

/// <summary>Hands ai-hub's <c>invoice-parsed</c> events to the application; resolved per message by the routing consumer.</summary>
public class InvoiceParsedEventHandler(IInvoiceParseResultService resultService) : IBaseEventHandler<InvoiceParsedContent>
{
    public Task HandleAsync(IBaseEventEnvelope<InvoiceParsedContent> @event, CancellationToken cancellationToken = default)
    {
        EnsureSupportedVersion(@event.Id, @event.ContentVersion);
        return resultService.ApplyParsedAsync(@event.Content);
    }

    internal static void EnsureSupportedVersion(string id, string contentVersion)
    {
        // Thrown, so the consumer rejects the message: a newer major version cannot be read safely.
        if (!MessagingContract.IsContentVersionSupported(contentVersion))
        {
            throw new InvalidOperationException($"Rejecting ai-hub event {id}: unsupported contentVersion '{contentVersion}'.");
        }
    }
}

/// <summary>Hands ai-hub's <c>invoice-parse-failed</c> events to the application.</summary>
public class InvoiceParseFailedEventHandler(IInvoiceParseResultService resultService) : IBaseEventHandler<InvoiceParseFailedContent>
{
    public Task HandleAsync(IBaseEventEnvelope<InvoiceParseFailedContent> @event, CancellationToken cancellationToken = default)
    {
        InvoiceParsedEventHandler.EnsureSupportedVersion(@event.Id, @event.ContentVersion);
        return resultService.ApplyFailedAsync(@event.Content);
    }
}
