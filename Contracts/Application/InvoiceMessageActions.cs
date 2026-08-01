namespace Contracts.Application;

public static class InvoiceMessageActions
{
    // Single hyphenated segment; the routing-key builder rejects dots.
    public const string Invoice = MessagingContract.InvoiceAction;
}
