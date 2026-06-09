using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class GenerateMonthlyStatementService(
    IInvoiceRepository invoiceRepository,
    IInvoiceAllocationRepository invoiceAllocationRepository,
    IMonthlyStatementRepository monthlyStatementRepository,
    IContactMonthlyStatementRepository contactMonthlyStatementRepository,
    IBaseUow uow) : IGenerateMonthlyStatementService
{
    public async Task<MonthlyStatement?> GenerateAsync(Guid addressId, int year, int month, Guid userId)
    {
        var invoicesResponse = await invoiceRepository.GetAllAsync(userId);
        var invoices = invoicesResponse.Successful && invoicesResponse.Value != null
            ? invoicesResponse.Value
                .Where(invoice => invoice.AddressId == addressId
                                  && invoice.InvoiceDate.Year == year
                                  && invoice.InvoiceDate.Month == month)
                .ToList()
            : new List<Invoice>();

        if (invoices.Count == 0)
        {
            return null;
        }

        var statementsResponse = await monthlyStatementRepository.GetAllAsync(userId);
        var statement = statementsResponse.Successful && statementsResponse.Value != null
            ? statementsResponse.Value.FirstOrDefault(existing =>
                existing.AddressId == addressId && existing.Year == year && existing.Month == month)
            : null;

        var isNew = statement == null;
        statement ??= new MonthlyStatement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddressId = addressId,
            Year = year,
            Month = month,
            CreatedAt = DateTime.UtcNow
        };

        statement.UserId = userId;
        statement.TotalSum = invoices.Sum(invoice => invoice.TotalSum);
        statement.Status = EMonthlyStatementStatus.ReadyToSend;

        if (isNew)
        {
            await monthlyStatementRepository.CreateAsync(statement, userId);
        }
        else
        {
            await monthlyStatementRepository.UpdateAsync(statement.Id, statement, string.Empty, userId);
        }

        await UpsertContactStatementsAsync(statement, invoices.Select(invoice => invoice.Id).ToHashSet(), userId);
        await uow.SaveChangesAsync();

        return statement;
    }

    private async Task UpsertContactStatementsAsync(MonthlyStatement statement, ISet<Guid> invoiceIds, Guid userId)
    {
        var allocationsResponse = await invoiceAllocationRepository.GetAllAsync(userId);
        var allocationTotals = allocationsResponse.Successful && allocationsResponse.Value != null
            ? allocationsResponse.Value
                .Where(allocation => invoiceIds.Contains(allocation.InvoiceId))
                .GroupBy(allocation => allocation.ContactId)
                .ToDictionary(group => group.Key, group => group.Sum(allocation => allocation.AllocatedSum))
            : new Dictionary<Guid, decimal>();

        var contactStatementsResponse = await contactMonthlyStatementRepository.GetAllAsync(userId);
        var existingStatements = contactStatementsResponse.Successful && contactStatementsResponse.Value != null
            ? contactStatementsResponse.Value
                .Where(contactStatement => contactStatement.MonthlyStatementId == statement.Id)
                .ToDictionary(contactStatement => contactStatement.ContactId)
            : new Dictionary<Guid, ContactMonthlyStatement>();

        foreach (var (contactId, amount) in allocationTotals)
        {
            if (existingStatements.TryGetValue(contactId, out var contactStatement))
            {
                contactStatement.Amount = amount;
                await contactMonthlyStatementRepository.UpdateAsync(contactStatement.Id, contactStatement, string.Empty, userId);
                continue;
            }

            await contactMonthlyStatementRepository.CreateAsync(new ContactMonthlyStatement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MonthlyStatementId = statement.Id,
                ContactId = contactId,
                Amount = amount
            }, userId);
        }
    }
}
