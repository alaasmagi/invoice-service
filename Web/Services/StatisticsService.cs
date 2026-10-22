using DataAccess.Context;
using Domain;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Models;

namespace Web.Services;

public class StatisticsService(AppDbContext context)
{
    /// <summary>Categorical slots available before remaining services fold into "Other".</summary>
    private const int MaxNamedSeries = 7;

    public async Task<StatisticsViewModel> GetAsync(int months, Guid userId)
    {
        var current = BillingPeriod.Current;
        var periods = Enumerable.Range(0, months)
            .Select(offset => BillingPeriod.From(current.Start.AddMonths(offset - months + 1)))
            .ToList();
        var rangeStart = periods[0].Start;
        var rangeEnd = current.End;
        var previousStart = rangeStart.AddMonths(-months);

        // Invoices are counted in the month of their invoice date, so every invoice lands in exactly one month.
        var invoices = await context.Invoices
            .Where(e => e.UserId == userId && e.InvoiceDate >= previousStart && e.InvoiceDate <= rangeEnd)
            .Select(e => new { e.InvoiceDate, e.TotalSum, e.ServiceId, e.AddressId })
            .ToListAsync();
        var inRange = invoices.Where(e => e.InvoiceDate >= rangeStart).ToList();
        var previous = invoices.Where(e => e.InvoiceDate < rangeStart).ToList();

        // Series order is alphabetical over all services, so a service keeps its color whatever the range.
        var services = await context.Services.Where(e => e.UserId == userId)
            .OrderBy(e => e.Name).Select(e => new { e.Id, e.Name }).ToListAsync();
        var activeServiceIds = inRange.Select(e => e.ServiceId).ToHashSet();
        var ordered = services.Where(s => activeServiceIds.Contains(s.Id)).ToList();
        var named = ordered.Take(ordered.Count > MaxNamedSeries + 1 ? MaxNamedSeries : MaxNamedSeries + 1).ToList();
        var hasOther = ordered.Count > named.Count;

        // With up to eight services each keeps one slot for good; beyond that, slots go to the named series in range.
        var slotSource = services.Count <= MaxNamedSeries + 1 ? services : named;
        var slotByService = slotSource.Select((s, i) => (s.Id, Slot: i + 1)).ToDictionary(x => x.Id, x => x.Slot);
        int SlotFor(Guid serviceId) => named.Any(n => n.Id == serviceId) ? slotByService[serviceId] : 0;

        var series = named.Select((s, i) => new ChartSeries(i, s.Name, SlotFor(s.Id))).ToList();
        if (hasOther) series.Add(new ChartSeries(series.Count, "Other", 0));
        var seriesIndexByService = named.Select((s, i) => (s.Id, i)).ToDictionary(x => x.Id, x => x.i);
        int SeriesIndex(Guid serviceId) => seriesIndexByService.TryGetValue(serviceId, out var i) ? i : series.Count - 1;

        var columns = periods.Select(p =>
        {
            var amounts = new decimal[series.Count];
            foreach (var invoice in inRange.Where(e => BillingPeriod.From(e.InvoiceDate) == p))
            {
                amounts[SeriesIndex(invoice.ServiceId)] += invoice.TotalSum;
            }
            return new MonthColumn(p, amounts);
        }).ToList();

        var (axisMax, ticks) = NiceAxis(columns.Count == 0 ? 0 : columns.Max(c => c.Total));

        var addresses = await context.Addresses.Where(e => e.UserId == userId)
            .Select(e => new { e.Id, e.Name }).ToDictionaryAsync(e => e.Id, e => e.Name);

        var byService = services
            .Select(s => new BreakdownRow(
                s.Id, s.Name,
                inRange.Where(e => e.ServiceId == s.Id).Sum(e => e.TotalSum),
                previous.Where(e => e.ServiceId == s.Id).Sum(e => e.TotalSum),
                inRange.Count(e => e.ServiceId == s.Id),
                SlotFor(s.Id)))
            .Where(r => r.Total > 0 || r.PreviousTotal > 0)
            .OrderByDescending(r => r.Total)
            .ToList();

        var byAddress = addresses
            .Select(a => new BreakdownRow(
                a.Key, a.Value,
                inRange.Where(e => e.AddressId == a.Key).Sum(e => e.TotalSum),
                previous.Where(e => e.AddressId == a.Key).Sum(e => e.TotalSum),
                inRange.Count(e => e.AddressId == a.Key),
                1))
            .Where(r => r.Total > 0 || r.PreviousTotal > 0)
            .OrderByDescending(r => r.Total)
            .ToList();

        var firstYear = periods[0].Year;
        var firstMonth = periods[0].Month;
        var statements = await context.MonthlyStatements.Include(e => e.Contact)
            .Where(e => e.UserId == userId
                        && (e.Year > firstYear || (e.Year == firstYear && e.Month >= firstMonth))
                        && (e.Year < current.Year || (e.Year == current.Year && e.Month <= current.Month)))
            .Select(e => new { e.ContactId, e.Contact.FullName, e.TotalSum, e.Status })
            .ToListAsync();
        var byPerson = statements
            .GroupBy(e => new { e.ContactId, e.FullName })
            .Select(g => new PersonRow(
                g.Key.ContactId, g.Key.FullName, g.Count(),
                g.Sum(e => e.TotalSum),
                g.Where(e => e.Status == EMonthlyStatementStatus.Sent).Sum(e => e.TotalSum)))
            .OrderByDescending(r => r.Billed)
            .ToList();

        return new StatisticsViewModel
        {
            Months = months,
            Columns = columns,
            Series = series,
            Total = inRange.Sum(e => e.TotalSum),
            PreviousTotal = previous.Sum(e => e.TotalSum),
            InvoiceCount = inRange.Count,
            AxisMax = axisMax,
            Ticks = ticks,
            ByService = byService,
            ByAddress = byAddress,
            ByPerson = byPerson
        };
    }

    /// <summary>Rounds the axis up to a clean step (1, 2, 2.5 or 5 × 10ⁿ) with four or five ticks.</summary>
    private static (decimal Max, IReadOnlyList<decimal> Ticks) NiceAxis(decimal max)
    {
        if (max <= 0) return (100, [0, 25, 50, 75, 100]);
        var raw = (double)max / 4;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var step = new[] { 1, 2, 2.5, 5, 10 }.Select(f => f * magnitude).First(s => s >= raw);
        var axisMax = (decimal)(step * Math.Ceiling((double)max / step));
        var ticks = new List<decimal>();
        for (var t = 0m; t <= axisMax; t += (decimal)step) ticks.Add(t);
        return (axisMax, ticks);
    }
}
