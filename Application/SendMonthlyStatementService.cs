using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class SendMonthlyStatementService(
    IMonthlyStatementRepository monthlyStatementRepository,
    IContactMonthlyStatementRepository contactMonthlyStatementRepository,
    IContactRepository contactRepository,
    IEmailSender emailSender,
    IBaseUow uow) : ISendMonthlyStatementService
{
    public async Task SendAsync(Guid monthlyStatementId, Guid userId)
    {
        var statementResponse = await monthlyStatementRepository.GetByIdAsync(monthlyStatementId, userId);
        if (!statementResponse.Successful || statementResponse.Value == null)
        {
            throw new InvalidOperationException("Monthly statement was not found.");
        }

        var statement = statementResponse.Value;
        var contactStatementsResponse = await contactMonthlyStatementRepository.GetAllAsync(userId);
        var contactStatements = contactStatementsResponse.Successful && contactStatementsResponse.Value != null
            ? contactStatementsResponse.Value
                .Where(contactStatement => contactStatement.MonthlyStatementId == monthlyStatementId)
                .ToList()
            : new List<ContactMonthlyStatement>();

        var contactsResponse = await contactRepository.GetAllAsync(userId);
        var contactsById = contactsResponse.Successful && contactsResponse.Value != null
            ? contactsResponse.Value.ToDictionary(contact => contact.Id)
            : new Dictionary<Guid, Contact>();

        var sentCount = 0;
        foreach (var contactStatement in contactStatements)
        {
            if (!contactsById.TryGetValue(contactStatement.ContactId, out var contact))
            {
                continue;
            }

            try
            {
                await emailSender.SendMonthlyStatementEmailAsync(
                    contact.Email,
                    contact.FullName,
                    contactStatement.Amount,
                    statement.Period);

                contactStatement.EmailSent = true;
                contactStatement.EmailSentAt = DateTime.UtcNow;
                sentCount++;
            }
            catch
            {
                contactStatement.EmailSent = false;
            }

            await contactMonthlyStatementRepository.UpdateAsync(contactStatement.Id, contactStatement, string.Empty, userId);
        }

        statement.Status = sentCount == contactStatements.Count
            ? EMonthlyStatementStatus.Sent
            : sentCount == 0
                ? EMonthlyStatementStatus.Failed
                : EMonthlyStatementStatus.PartiallySent;
        statement.SentAt = sentCount > 0 ? DateTime.UtcNow : null;

        await monthlyStatementRepository.UpdateAsync(statement.Id, statement, string.Empty, userId);
        await uow.SaveChangesAsync();
    }
}
