namespace Contracts.Application;

public interface IMonthlyInvoiceEmailPublisher
{
    /// <summary>
    /// Publishes one invoice email command (one recipient) to email-hub.commands and waits for a
    /// bounded broker confirmation. The envelope id is derived deterministically from the invoice
    /// identity plus the <paramref name="attempt"/> number, so re-publishing the SAME attempt produces
    /// the same id (email-hub drops the repeat), while an explicit resend uses a higher attempt and is
    /// delivered as a new message.
    /// </summary>
    Task<InvoiceEmailPublishResult> PublishAsync(MonthlyInvoiceEmailContent content, int attempt, CancellationToken ct = default);
}
