using Contracts.Application;
using Contracts.DataAccess;
using Microsoft.Extensions.Logging;

namespace Application;

public class SendPeriodInvoiceEmailsService(
    IMonthlyStatementRepository monthlyStatementRepository,
    ISendMonthlyStatementService sendMonthlyStatementService,
    ILogger<SendPeriodInvoiceEmailsService> logger) : ISendPeriodInvoiceEmailsService
{
    public async Task<SendPeriodResult> SendPeriodAsync(int year, int month, Guid userId, CancellationToken ct = default)
    {
        var response = await monthlyStatementRepository.GetAllAsync(userId);
        var statements = response.Successful && response.Value != null
            ? response.Value.Where(statement => statement.Year == year && statement.Month == month).ToList()
            : [];

        var published = 0;
        var skipped = 0;
        var failed = 0;

        // One message per recipient; each send is individually idempotent via the published-record
        // table, so the whole batch is safe to re-run. A single bad statement never aborts the batch.
        foreach (var statement in statements)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // Batch runs stay idempotent: never resend a statement already published this period.
                switch (await sendMonthlyStatementService.SendAsync(statement.Id, userId, allowResend: false, ct))
                {
                    case EInvoiceSendOutcome.Published:
                        published++;
                        break;
                    case EInvoiceSendOutcome.Skipped:
                        skipped++;
                        break;
                    case EInvoiceSendOutcome.Failed:
                        failed++;
                        break;
                }
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(
                    exception,
                    "Failed to send invoice mail for statement {StatementId} in {Year}-{Month:D2}.",
                    statement.Id, year, month);
            }
        }

        logger.LogInformation(
            "Period {Year}-{Month:D2} batch: {Published} published, {Skipped} already sent, {Failed} failed of {Total}.",
            year, month, published, skipped, failed, statements.Count);

        return new SendPeriodResult(statements.Count, published, skipped, failed);
    }
}
