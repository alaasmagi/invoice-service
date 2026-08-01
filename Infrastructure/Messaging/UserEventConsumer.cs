using Base.Contracts.Message;
using Base.Keycloak.Events;
using Base.Message.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

/// <summary>
/// Consumes identity-hub user events from the pre-declared <c>invoice.users</c> queue
/// (bound to identity-hub.events with identity-hub.invoice.#). The shared base does manual acks with
/// bounded prefetch and never declares or binds topology. On a handler exception it nacks WITHOUT
/// requeue, so validation/permanent failures dead-letter for a human to see.
/// </summary>
public sealed class UserEventConsumer : RabbitMqConsumerBase<UserEventContent>
{
    public UserEventConsumer(
        RabbitMqConnectionManager connectionManager,
        IBaseEventHandler<UserEventContent> handler,
        ILogger<UserEventConsumer> logger,
        InvoiceMessagingOptions invoiceOptions)
        : base(connectionManager, handler, logger, invoiceOptions.Queue)
    {
    }
}
