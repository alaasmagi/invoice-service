using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Mapping;
using Web.Models;

namespace Web.Services;

public class PeriodOverviewService(AppDbContext context, WebDtoMapper mapper)
{
    public async Task<PeriodOverview> GetAsync(BillingPeriod period, Guid userId)
    {
        var invoices = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .Where(e => e.UserId == userId)
            .InPeriod(period)
            .OrderByDescending(e => e.InvoiceDate)
            .ToListAsync();

        var residents = await context.AddressContacts
            .Where(e => e.UserId == userId)
            .ToListAsync();
        var withoutResidents = invoices
            .Where(invoice => !residents.Any(r => r.AddressId == invoice.AddressId && r.IsActive(invoice.InvoiceDate)))
            .ToList();

        var statements = await context.MonthlyStatements.Include(e => e.Contact)
            .Where(e => e.UserId == userId && e.Year == period.Year && e.Month == period.Month)
            .OrderBy(e => e.Contact.FullName)
            .ToListAsync();

        var lineTotals = await context.MonthlyStatementLines
            .Where(e => e.UserId == userId && e.MonthlyStatement.Year == period.Year && e.MonthlyStatement.Month == period.Month)
            .Select(e => new { e.InvoiceId, e.InvoiceTotal })
            .Distinct()
            .ToListAsync();
        var lineTotalByInvoice = lineTotals.GroupBy(e => e.InvoiceId).ToDictionary(g => g.Key, g => g.First().InvoiceTotal);

        var allocatable = invoices.Except(withoutResidents).ToList();
        var pending = allocatable.Count(invoice =>
                          !lineTotalByInvoice.TryGetValue(invoice.Id, out var total) || total != invoice.TotalSum)
                      + lineTotalByInvoice.Keys.Count(id => invoices.All(invoice => invoice.Id != id));

        var previous = period.Previous;
        var previousPairs = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .Where(e => e.UserId == userId)
            .InPeriod(previous)
            .ToListAsync();
        var missing = previousPairs
            .Where(prev => !invoices.Any(cur => cur.AddressId == prev.AddressId && cur.ServiceId == prev.ServiceId))
            .DistinctBy(prev => (prev.AddressId, prev.ServiceId))
            .OrderBy(prev => prev.Address.Name).ThenBy(prev => prev.Service.Name)
            .Select(prev => (mapper.ToDto(prev.Address), mapper.ToDto(prev.Service)))
            .ToList();

        return new PeriodOverview
        {
            Period = period,
            Invoices = invoices.Select(mapper.ToDto).ToList(),
            InvoicesWithoutResidents = withoutResidents.Select(mapper.ToDto).ToList(),
            MissingInvoices = missing,
            Statements = statements.Select(mapper.ToDto).ToList(),
            PendingInvoiceCount = pending
        };
    }
}
