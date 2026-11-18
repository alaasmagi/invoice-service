namespace Infrastructure.Messaging;

public class InvoiceMessagingOptions
{
    /// <summary>This service's own consumer queue, e.g. <c>invoice-service</c>.</summary>
    public string Queue { get; init; } = default!;

    public string NotificationHubExchange { get; init; } = default!;

    public string AiHubExchange { get; init; } = default!;
}
