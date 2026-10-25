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
    IContactRepository contactRepository,
    IAllocationRuleRepository allocationRuleRepository,
    IInvoiceAllocationRepository invoiceAllocationRepository,
    IInvoiceSplitCalculator splitCalculator,
    IBaseUow uow) : IGenerateMonthlyStatementService
{
    public async Task<MonthlyStatementGenerationResult> GenerateAsync(int year, int month, Guid userId)
    {
        var invoices = await GetMonthInvoicesAsync(year, month, userId);
        if (invoices.Count == 0)
        {
            return new MonthlyStatementGenerationResult();
        }

        var addresses = await GetAddressesAsync(userId);
        var services = await GetServicesAsync(userId);
        var addressContacts = await GetAddressContactsAsync(userId);
        var existingStatements = await GetExistingStatementsAsync(year, month, userId);
        var rulesByAddress = (await allocationRuleRepository.GetForUserWithSharesAsync(userId))
            .ToLookup(rule => rule.AddressId);
        var manualByInvoice = (await invoiceAllocationRepository.GetOverridesByInvoiceIdsAsync(invoices.Select(invoice => invoice.Id).ToList(), userId))
            .ToLookup(allocation => allocation.InvoiceId);
        var warnings = new List<AllocationWarning>();

        await monthlyStatementLineRepository.RemoveByStatementIdsAsync(existingStatements.Select(statement => statement.Id).ToList(), userId);

        var generatedLines = new Dictionary<Guid, List<MonthlyStatementLine>>();
        foreach (var invoice in invoices)
        {
            var addressName = addresses.TryGetValue(invoice.AddressId, out var address) ? address.Name : "Unknown address";
            var serviceName = services.TryGetValue(invoice.ServiceId, out var service) ? service.Name : "Unknown service";
            var split = splitCalculator.Calculate(new InvoiceSplitInput
            {
                Invoice = invoice,
                ManualAllocations = manualByInvoice[invoice.Id].ToList(),
                Rules = rulesByAddress[invoice.AddressId].ToList(),
                ActiveResidentIds = GetActiveContactIdsForInvoice(invoice, addressContacts)
            });

            foreach (var warning in split.Warnings)
            {
                warning.AddressId = invoice.AddressId;
                warning.AddressName = addressName;
                warning.ServiceName = serviceName;
                warning.InvoiceDate = invoice.InvoiceDate;
                warnings.Add(warning);
            }

            foreach (var share in split.Shares)
            {
                if (!generatedLines.TryGetValue(share.ContactId, out var lines))
                {
                    lines = [];
                    generatedLines[share.ContactId] = lines;
                }

                lines.Add(new MonthlyStatementLine
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    InvoiceId = invoice.Id,
                    AddressId = invoice.AddressId,
                    AddressName = addressName,
                    ServiceId = invoice.ServiceId,
                    ServiceName = serviceName,
                    InvoiceDate = invoice.InvoiceDate,
                    PeriodStart = invoice.PeriodStart,
                    PeriodEnd = invoice.PeriodEnd,
                    InvoiceTotal = invoice.TotalSum,
                    ResidentCount = split.ParticipantCount,
                    AllocatedAmount = share.Amount,
                    AllocationMethod = split.Method,
                    AllocationRuleId = split.RuleId,
                    ShareValue = share.ShareValue
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
        var statements = generatedStatements
            .OrderBy(statement => statement.ContactId)
            .ToList();
        await AddContactNamesAsync(warnings, userId);
        return new MonthlyStatementGenerationResult { Statements = statements, Warnings = warnings };
    }

    private async Task AddContactNamesAsync(List<AllocationWarning> warnings, Guid userId)
    {
        if (!warnings.Any(warning => warning.ContactId != null))
        {
            return;
        }

        var response = await contactRepository.GetAllAsync(userId);
        var names = response.Successful && response.Value != null
            ? response.Value.ToDictionary(contact => contact.Id, contact => contact.FullName)
            : new Dictionary<Guid, string>();
        foreach (var warning in warnings)
        {
            if (warning.ContactId is { } contactId && names.TryGetValue(contactId, out var name))
            {
                warning.ContactName = name;
            }
        }
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

    private static List<Guid> GetActiveContactIdsForInvoice(Invoice invoice, IEnumerable<AddressContact> addressContacts)
    {
        return addressContacts
            .Where(addressContact => addressContact.AddressId == invoice.AddressId
                                     && addressContact.IsActive(invoice.InvoiceDate))
            .Select(addressContact => addressContact.ContactId)
            .Distinct()
            .OrderBy(contactId => contactId)
            .ToList();
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
