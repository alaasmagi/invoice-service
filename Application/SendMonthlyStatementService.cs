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
    IMonthlyInvoiceEmailPublisher emailPublisher) : ISendMonthlyStatementService
{
    private static readonly CultureInfo EstonianCulture = CultureInfo.GetCultureInfo("et-EE");

    public async Task SendAsync(Guid monthlyStatementId, Guid userId)
    {
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

        try
        {
            await emailPublisher.PublishAsync(BuildEmailContent(statement, contact, senderPaymentDetails!, lines));

            statement.Status = EMonthlyStatementStatus.Sent;
            statement.SentAt = DateTime.UtcNow;
        }
        catch
        {
            statement.Status = EMonthlyStatementStatus.Failed;
            statement.SentAt = null;
        }

        await monthlyStatementRepository.SetSendStatusAsync(statement.Id, userId, statement.Status, statement.SentAt);
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
        IReadOnlyCollection<MonthlyStatementLine> lines)
    {
        var invoiceIdentifier = InvoiceIdentifier(statement.Id);
        var invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueDate = invoiceDate.AddDays(14);
        var invoicePeriod = $"{statement.Month:D2}.{statement.Year:D4}";

        return new MonthlyInvoiceEmailContent
        {
            Email = contact.Email,
            FullName = contact.FullName,
            Locale = senderPaymentDetails.Locale,
            InvoiceIdentifier = invoiceIdentifier,
            InvoicePeriod = invoicePeriod,
            InvoiceDate = FormatDate(invoiceDate),
            InvoiceDueDate = FormatDate(dueDate),
            InvoiceTotal = FormatMoney(statement.TotalSum),
            Currency = "€",
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
            PaymentDescription = $"{invoiceIdentifier} {invoicePeriod}"
        };
    }

    private static string InvoiceIdentifier(Guid statementId)
    {
        return statementId.ToString("N")[..7].ToUpperInvariant();
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("dd.MM.yyyy", EstonianCulture);
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString("N2", EstonianCulture);
    }
}
