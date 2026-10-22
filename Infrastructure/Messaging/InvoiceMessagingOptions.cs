namespace Infrastructure.Messaging;

public class InvoiceMessagingOptions
{
    /// <summary>The consumer queue this service reads identity events from, e.g. <c>invoice.users</c>.</summary>
    public string Queue { get; init; } = default!;
}
