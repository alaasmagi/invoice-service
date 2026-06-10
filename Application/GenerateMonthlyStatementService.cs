using Base.Contracts.DataAccess;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application;

public class GenerateMonthlyStatementService(
    IInvoiceRepository invoiceRepository,
    IAddressRepository addressRepository,
    IServiceRepository serviceRepository,
    IAddressContactRepository addressContactRepository,
    IMonthlyStatementRepository monthlyStatementRepository,
    IMonthlyStatementLineRepository monthlyStatementLineRepository,
    IBaseUow uow) : IGenerateMonthlyStatementService
{
    public async Task<IReadOnlyList<MonthlyStatement>> GenerateAsync(int year, int month, Guid userId)
    {
        var invoices = await GetMonthInvoicesAsync(year, month, userId);
        if (invoices.Count == 0)
        {
            return [];
        }

        var addresses = await GetAddressesAsync(userId);
        var services = await GetServicesAsync(userId);
        var addressContacts = await GetAddressContactsAsync(userId);
        var existingStatements = await GetExistingStatementsAsync(year, month, userId);

        await monthlyStatementLineRepository.RemoveByStatementIdsAsync(existingStatements.Select(statement => statement.Id).ToList(), userId);

        var generatedLines = new Dictionary<Guid, List<MonthlyStatementLine>>();
        foreach (var invoice in invoices)
        {
            var activeContacts = GetActiveContactsForInvoice(invoice, addressContacts);
            if (activeContacts.Count == 0)
            {
                Console.WriteLine($"Invoice {invoice.Id} for {invoice.InvoiceDate:yyyy-MM-dd} could not be allocated because address {invoice.AddressId} has no active contacts.");
                continue;
            }

            var shares = SplitEvenly(invoice.TotalSum, activeContacts.Select(contact => contact.ContactId).ToList());
            foreach (var (contactId, amount) in shares)
            {
                if (!generatedLines.TryGetValue(contactId, out var lines))
                {
                    lines = [];
                    generatedLines[contactId] = lines;
                }

                lines.Add(new MonthlyStatementLine
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    InvoiceId = invoice.Id,
                    AddressId = invoice.AddressId,
                    AddressName = addresses.TryGetValue(invoice.AddressId, out var address) ? address.Name : "Unknown address",
                    ServiceId = invoice.ServiceId,
                    ServiceName = services.TryGetValue(invoice.ServiceId, out var service) ? service.Name : "Unknown service",
                    InvoiceDate = invoice.InvoiceDate,
                    PeriodStart = invoice.PeriodStart,
                    PeriodEnd = invoice.PeriodEnd,
                    InvoiceTotal = invoice.TotalSum,
                    ResidentCount = activeContacts.Count,
                    AllocatedAmount = amount
                });
            }
        }

        var generatedStatements = new List<MonthlyStatement>();
        var obsoleteStatementIds = existingStatements
            .Where(statement => !generatedLines.ContainsKey(statement.ContactId))
            .Select(statement => statement.Id)
            .ToList();
        await monthlyStatementRepository.RemoveByIdsAsync(obsoleteStatementIds, userId);

        foreach (var (contactId, lines) in generatedLines)
        {
            var statement = existingStatements.FirstOrDefault(existing => existing.ContactId == contactId)
                ?? new MonthlyStatement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ContactId = contactId,
                    Year = year,
                    Month = month,
                    CreatedAt = DateTime.UtcNow
                };

            statement.UserId = userId;
            statement.ContactId = contactId;
            statement.Year = year;
            statement.Month = month;
            statement.TotalSum = lines.Sum(line => line.AllocatedAmount);
            statement.Status = EMonthlyStatementStatus.ReadyToSend;
            statement.SentAt = null;

            if (existingStatements.Any(existing => existing.Id == statement.Id))
            {
                await monthlyStatementRepository.UpdateAsync(statement.Id, statement, string.Empty, userId);
            }
            else
            {
                await monthlyStatementRepository.CreateAsync(statement, userId);
            }

            foreach (var line in lines)
            {
                line.MonthlyStatementId = statement.Id;
                await monthlyStatementLineRepository.CreateAsync(line, userId);
            }

            generatedStatements.Add(statement);
        }

        await uow.SaveChangesAsync();
        return generatedStatements
            .OrderBy(statement => statement.ContactId)
            .ToList();
    }

    private async Task<List<Invoice>> GetMonthInvoicesAsync(int year, int month, Guid userId)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var invoicesResponse = await invoiceRepository.GetAllAsync(userId);
        return invoicesResponse.Successful && invoicesResponse.Value != null
            ? invoicesResponse.Value
                .Where(invoice => IsInvoiceInMonth(invoice, monthStart, monthEnd))
                .OrderBy(invoice => invoice.InvoiceDate)
                .ThenBy(invoice => invoice.Id)
                .ToList()
            : [];
    }

    private static bool IsInvoiceInMonth(Invoice invoice, DateOnly monthStart, DateOnly monthEnd)
    {
        var invoiceDateIsInMonth = invoice.InvoiceDate >= monthStart && invoice.InvoiceDate <= monthEnd;
        if (invoice.PeriodStart.HasValue || invoice.PeriodEnd.HasValue)
        {
            var periodStart = invoice.PeriodStart ?? invoice.PeriodEnd!.Value;
            var periodEnd = invoice.PeriodEnd ?? invoice.PeriodStart!.Value;
            var periodOverlapsMonth = periodStart <= monthEnd && periodEnd >= monthStart;
            return invoiceDateIsInMonth || periodOverlapsMonth;
        }

        return invoiceDateIsInMonth;
    }

    private async Task<List<AddressContact>> GetAddressContactsAsync(Guid userId)
    {
        var response = await addressContactRepository.GetAllAsync(userId);
        return response.Successful && response.Value != null
            ? response.Value.ToList()
            : [];
    }

    private async Task<List<MonthlyStatement>> GetExistingStatementsAsync(int year, int month, Guid userId)
    {
        var response = await monthlyStatementRepository.GetAllAsync(userId);
        return response.Successful && response.Value != null
            ? response.Value
                .Where(statement => statement.Year == year && statement.Month == month)
                .ToList()
            : [];
    }

    private static List<AddressContact> GetActiveContactsForInvoice(Invoice invoice, IEnumerable<AddressContact> addressContacts)
    {
        return addressContacts
            .Where(addressContact => addressContact.AddressId == invoice.AddressId
                                     && addressContact.IsActive(invoice.InvoiceDate))
            .GroupBy(addressContact => addressContact.ContactId)
            .Select(group => group.OrderBy(addressContact => addressContact.StartDate).First())
            .OrderBy(addressContact => addressContact.ContactId)
            .ToList();
    }

    private static Dictionary<Guid, decimal> SplitEvenly(decimal total, IReadOnlyList<Guid> contactIds)
    {
        var shares = new Dictionary<Guid, decimal>();
        if (contactIds.Count == 0)
        {
            return shares;
        }

        var baseAmount = decimal.Round(total / contactIds.Count, 2, MidpointRounding.AwayFromZero);
        var allocated = 0m;
        for (var i = 0; i < contactIds.Count; i++)
        {
            var amount = i == contactIds.Count - 1
                ? total - allocated
                : baseAmount;

            shares[contactIds[i]] = amount;
            allocated += amount;
        }

        return shares;
    }

    private async Task<Dictionary<Guid, Address>> GetAddressesAsync(Guid userId)
    {
        var response = await addressRepository.GetAllAsync(userId);
        return response.Successful && response.Value != null
            ? response.Value.ToDictionary(entity => entity.Id)
            : new Dictionary<Guid, Address>();
    }

    private async Task<Dictionary<Guid, Service>> GetServicesAsync(Guid userId)
    {
        var response = await serviceRepository.GetAllAsync(userId);
        return response.Successful && response.Value != null
            ? response.Value.ToDictionary(entity => entity.Id)
            : new Dictionary<Guid, Service>();
    }
}
