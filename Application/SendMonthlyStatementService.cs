using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using System.Globalization;

namespace Application;

public class SendMonthlyStatementService(
    IMonthlyStatementRepository monthlyStatementRepository,
    IMonthlyStatementLineRepository monthlyStatementLineRepository,
    IContactRepository contactRepository,
    IMonthlyStatementSenderPaymentDetailsProvider senderPaymentDetailsProvider,
    IPublishedInvoiceEmailRepository publishedInvoiceEmailRepository,
    IMonthlyInvoiceEmailPublisher emailPublisher) : ISendMonthlyStatementService
{
    public async Task<EInvoiceSendOutcome> SendAsync(Guid monthlyStatementId, Guid userId, bool allowResend = false, CancellationToken ct = default)
    {
        // Prior confirmed sends for this statement. The count is also the highest attempt number, so the
        // next attempt is count + 1. Without allowResend an already-sent statement is skipped (batch runs
        // stay idempotent); with allowResend an explicit "send again" proceeds as a new attempt.
        var previousAttempts = await publishedInvoiceEmailRepository.CountForStatementAsync(monthlyStatementId, userId, ct);
        if (!allowResend && previousAttempts > 0)
        {
            return EInvoiceSendOutcome.Skipped;
        }

        var attempt = previousAttempts + 1;

        var statementResponse = await monthlyStatementRepository.GetByIdAsync(monthlyStatementId, userId);
        if (!statementResponse.Successful || statementResponse.Value == null)
        {
            throw new InvalidOperationException("Monthly statement was not found.");
        }

        var statement = statementResponse.Value;
        var contact = await GetContactAsync(statement.ContactId, userId);
        var senderPaymentDetails = await senderPaymentDetailsProvider.GetAsync(userId);
        MonthlyStatementSenderPaymentDetailsValidator.EnsureCanSend(senderPaymentDetails);
        var lines = await GetStatementLinesAsync(statement.Id, userId);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Monthly statement has no invoice lines to send.");
        }

        var invoiceIdentifier = InvoiceIdentifier(statement.Id);
        var content = BuildEmailContent(statement, contact, senderPaymentDetails!, lines, invoiceIdentifier);

        InvoiceEmailPublishResult result;
        try
        {
            result = await emailPublisher.PublishAsync(content, attempt, ct);
        }
        catch (Exception)
        {
            await monthlyStatementRepository.SetSendStatusAsync(statement.Id, userId, EMonthlyStatementStatus.Failed, null);
            return EInvoiceSendOutcome.Failed;
        }

        if (!result.Confirmed)
        {
            await monthlyStatementRepository.SetSendStatusAsync(statement.Id, userId, EMonthlyStatementStatus.Failed, null);
            return EInvoiceSendOutcome.Failed;
        }

        // Record only after the broker confirmed the publish. Should the app crash between confirm and
        // this write, a re-run republishes the SAME deterministic envelope id and email-hub drops it.
        await publishedInvoiceEmailRepository.AddAsync(
            new PublishedInvoiceEmail
            {
                UserId = userId,
                MonthlyStatementId = statement.Id,
                Year = statement.Year,
                Month = statement.Month,
                Attempt = attempt,
                InvoiceIdentifier = invoiceIdentifier,
                Recipient = content.Email,
                EnvelopeId = result.EnvelopeId,
                ConfirmedAt = DateTime.UtcNow
            },
            ct);

        await monthlyStatementRepository.SetSendStatusAsync(statement.Id, userId, EMonthlyStatementStatus.Sent, DateTime.UtcNow);
        return EInvoiceSendOutcome.Published;
    }

    private async Task<Contact> GetContactAsync(Guid contactId, Guid userId)
    {
        var response = await contactRepository.GetByIdAsync(contactId, userId);
        if (!response.Successful || response.Value == null)
        {
            throw new InvalidOperationException("Monthly statement contact was not found.");
        }

        return response.Value;
    }

    private async Task<List<MonthlyStatementLine>> GetStatementLinesAsync(Guid statementId, Guid userId)
    {
        var response = await monthlyStatementLineRepository.GetAllAsync(userId);
        return response.Successful && response.Value != null
            ? response.Value
                .Where(line => line.MonthlyStatementId == statementId)
                .ToList()
            : [];
    }

    private static MonthlyInvoiceEmailContent BuildEmailContent(
        MonthlyStatement statement,
        Contact contact,
        MonthlyStatementSenderPaymentDetails senderPaymentDetails,
        IReadOnlyCollection<MonthlyStatementLine> lines,
        string invoiceIdentifier)
    {
        var invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueDate = invoiceDate.AddDays(14);

        // Machine-readable values only: email-hub owns all localisation and formats per recipient.
        return new MonthlyInvoiceEmailContent
        {
            Email = contact.Email,
            FullName = contact.FullName,
            Locale = senderPaymentDetails.Locale,
            InvoiceIdentifier = invoiceIdentifier,
            InvoicePeriod = statement.Period,               // ISO year-month, e.g. 2026-06
            InvoiceDate = FormatDate(invoiceDate),          // ISO date, e.g. 2026-06-06
            InvoiceDueDate = FormatDate(dueDate),
            InvoiceTotal = FormatMoney(statement.TotalSum), // invariant decimal string, e.g. 97.87
            Currency = "EUR",
            InvoiceRows = lines
                .OrderBy(line => line.AddressName)
                .ThenBy(line => line.InvoiceDate)
                .ThenBy(line => line.ServiceName)
                .Select(line => new MonthlyInvoiceEmailRowContent
                {
                    Address = line.AddressName,
                    Service = line.ServiceName,
                    PeriodStart = FormatDate(line.PeriodStart ?? line.InvoiceDate),
                    PeriodEnd = FormatDate(line.PeriodEnd ?? line.InvoiceDate),
                    InvoiceAmount = FormatMoney(line.InvoiceTotal),
                    Fractions = line.ResidentCount,
                    InvoiceFraction = FormatMoney(line.AllocatedAmount)
                })
                .ToList(),
            PaymentFullName = senderPaymentDetails.FullName.Trim(),
            PaymentIban = senderPaymentDetails.BankIban.Trim(),
            // Opaque reconciliation key — copied character for character by email-hub. NOT ISO:
            // this is exactly what incoming-payment reconciliation matches against.
            PaymentDescription = $"{invoiceIdentifier} {PaymentReferencePeriod(statement)}"
        };
    }

    private static string InvoiceIdentifier(Guid statementId)
    {
        return statementId.ToString("N")[..7].ToUpperInvariant();
    }

    private static string PaymentReferencePeriod(MonthlyStatement statement)
    {
        // Deliberately MM.yyyy (e.g. 06.2026), not ISO — it is part of the opaque payment reference.
        return $"{statement.Month:D2}.{statement.Year:D4}";
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string FormatMoney(decimal amount)
    {
        // Invariant decimal string with two decimals; trailing zeroes preserved (e.g. 40.00).
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
