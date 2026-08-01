namespace Contracts.Application;

/// <summary>
/// Publishes invoice mail for every sendable monthly statement in a period. Idempotent end to end:
/// a publish is treated as done only after the broker confirms it and is recorded locally, so
/// running the batch twice for the same period sends nothing the second time.
/// </summary>
public interface ISendPeriodInvoiceEmailsService
{
    Task<SendPeriodResult> SendPeriodAsync(int year, int month, Guid userId, CancellationToken ct = default);
}

/// <summary>Summary of a period batch run.</summary>
public sealed record SendPeriodResult(int Total, int Published, int Skipped, int Failed);
