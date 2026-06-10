using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class SendMonthlyStatementService(
    IMonthlyStatementRepository monthlyStatementRepository,
    IMonthlyStatementLineRepository monthlyStatementLineRepository,
    IContactRepository contactRepository,
    IEmailSender emailSender) : ISendMonthlyStatementService
{
    public async Task SendAsync(Guid monthlyStatementId, Guid userId)
    {
        var statementResponse = await monthlyStatementRepository.GetByIdAsync(monthlyStatementId, userId);
        if (!statementResponse.Successful || statementResponse.Value == null)
        {
            throw new InvalidOperationException("Monthly statement was not found.");
        }

        var statement = statementResponse.Value;
        var contact = await GetContactAsync(statement.ContactId, userId);
        var lines = await GetStatementLinesAsync(statement.Id, userId);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Monthly statement has no invoice lines to send.");
        }

        try
        {
            await emailSender.SendMonthlyStatementEmailAsync(new MonthlyStatementEmail
            {
                ToEmail = contact.Email,
                ContactName = contact.FullName,
                Period = statement.Period,
                TotalAmount = statement.TotalSum,
                Lines = lines
                    .OrderBy(line => line.AddressName)
                    .ThenBy(line => line.InvoiceDate)
                    .ThenBy(line => line.ServiceName)
                    .Select(line => new MonthlyStatementEmailLine
                    {
                        AddressName = line.AddressName,
                        ServiceName = line.ServiceName,
                        InvoiceDate = line.InvoiceDate,
                        PeriodStart = line.PeriodStart,
                        PeriodEnd = line.PeriodEnd,
                        InvoiceTotal = line.InvoiceTotal,
                        ResidentCount = line.ResidentCount,
                        ContactAmount = line.AllocatedAmount
                    })
                    .ToList()
            });

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
}
