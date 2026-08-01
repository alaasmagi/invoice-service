namespace Contracts.Application;

public interface IMonthlyInvoiceEmailPublisher
{
    /// <summary>
    /// Publishes one invoice email command (one recipient) to email-hub.commands and waits for a
    /// bounded broker confirmation. The envelope id is derived deterministically from the invoice
    /// identity so re-running a batch produces the same id and email-hub drops the repeat.
    /// </summary>
    Task<InvoiceEmailPublishResult> PublishAsync(MonthlyInvoiceEmailContent content, CancellationToken ct = default);
}
