using Base.Contracts.Message;
using Base.Message;
using Contracts.Application;

namespace Infrastructure.Messaging;

public sealed class RabbitMqMonthlyInvoiceEmailPublisher(
    IBaseEventPublisher publisher,
    InvoiceMessagingOptions options) : IMonthlyInvoiceEmailPublisher
{
    public Task PublishAsync(MonthlyInvoiceEmailContent content, CancellationToken ct = default)
    {
        var envelope = new BaseEventEnvelope<MonthlyInvoiceEmailContent>
        {
            Type = DefaultMessageTypes.Email,
            Source = options.EmailSource,
            Action = InvoiceMessageActions.MonthlyInvoice,
            Timestamp = DateTime.UtcNow,
            Content = content
        };

        return publisher.PublishAsync(envelope.Type, envelope, ct);
    }
}
