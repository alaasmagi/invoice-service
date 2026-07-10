using Base.Contracts.Message;
using Base.Keycloak.Events;
using Base.Message;
using Base.Message.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class UserEventConsumer : RabbitMqConsumerBase<UserEventContent>
{
    public UserEventConsumer(
        RabbitMqConnectionManager connectionManager,
        RabbitMqOptions options,
        IBaseEventHandler<UserEventContent> handler,
        ILogger<UserEventConsumer> logger,
        InvoiceMessagingOptions invoiceOptions)
        : base(
            connectionManager,
            options,
            handler,
            logger,
            invoiceOptions.Queue,
            DefaultMessageTypes.User)
    {
    }
}
