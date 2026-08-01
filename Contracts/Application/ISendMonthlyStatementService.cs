namespace Contracts.Application;

public interface ISendMonthlyStatementService
{
    /// <summary>
    /// Publishes invoice mail for a single statement. Idempotent: if the statement was already
    /// published and confirmed, it is skipped. A publish counts as sent only after the broker confirms.
    /// </summary>
    Task<EInvoiceSendOutcome> SendAsync(Guid monthlyStatementId, Guid userId, CancellationToken ct = default);
}

public enum EInvoiceSendOutcome
{
    Published,
    Skipped,
    Failed
}
