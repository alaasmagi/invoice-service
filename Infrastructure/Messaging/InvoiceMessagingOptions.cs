namespace Infrastructure.Messaging;

public sealed class InvoiceMessagingOptions
{
    public string Queue { get; init; } = default!;
    public string IdentitySource { get; init; } = default!;
    public string EmailSource { get; init; } = "invoice";
}
