namespace Contracts.Application;

public interface ISendMonthlyStatementService
{
    /// <summary>
    /// Publishes invoice mail for a single statement. A publish counts as sent only after the broker confirms.
    /// When <paramref name="allowResend"/> is false (the default, used by batch runs) a statement that was
    /// already published is skipped — the run stays idempotent. When true (an explicit user "send again")
    /// a new send attempt is published with a fresh envelope id so email-hub delivers it as a new message.
    /// </summary>
    Task<EInvoiceSendOutcome> SendAsync(Guid monthlyStatementId, Guid userId, bool allowResend = false, CancellationToken ct = default);
}

public enum EInvoiceSendOutcome
{
    Published,
    Skipped,
    Failed
}
