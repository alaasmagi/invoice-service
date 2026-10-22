using System.Globalization;
using DTO.DataAccess.DataAccess.DTO;

namespace Web.Helpers;

/// <summary>A billing month, passed around in URLs as "yyyy-MM".</summary>
public readonly record struct BillingPeriod(int Year, int Month) : IComparable<BillingPeriod>
{
    public static BillingPeriod Current
    {
        get
        {
            var today = DateTime.UtcNow;
            return new BillingPeriod(today.Year, today.Month);
        }
    }

    public string Key => $"{Year:D4}-{Month:D2}";

    public string Label => Start.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public DateOnly Start => new(Year, Month, 1);

    public DateOnly End => Start.AddMonths(1).AddDays(-1);

    public BillingPeriod Previous => From(Start.AddMonths(-1));

    public static BillingPeriod From(DateOnly date) => new(date.Year, date.Month);

    public static bool TryParse(string? value, out BillingPeriod period)
    {
        period = default;
        if (string.IsNullOrWhiteSpace(value)
            || !DateOnly.TryParseExact(value + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        period = From(date);
        return true;
    }

    /// <summary>The last <paramref name="count"/> months (newest first) merged with any extra periods that have data.</summary>
    public static List<BillingPeriod> Options(IEnumerable<BillingPeriod> withData, int count = 18)
    {
        var current = Current;
        return Enumerable.Range(0, count)
            .Select(offset => From(current.Start.AddMonths(-offset)))
            .Concat(withData)
            .Distinct()
            .OrderDescending()
            .ToList();
    }

    public int CompareTo(BillingPeriod other) => Start.CompareTo(other.Start);

    public override string ToString() => Key;
}

public static class InvoicePeriodQueryExtensions
{
    /// <summary>
    /// Same matching rule as monthly statement generation: the invoice date falls in the month,
    /// or the billed period overlaps it.
    /// </summary>
    public static IQueryable<InvoiceEntity> InPeriod(this IQueryable<InvoiceEntity> query, BillingPeriod period)
    {
        var monthStart = period.Start;
        var monthEnd = period.End;
        return query.Where(e =>
            (e.InvoiceDate >= monthStart && e.InvoiceDate <= monthEnd)
            || (e.PeriodStart != null && e.PeriodEnd != null && e.PeriodStart.Value <= monthEnd && e.PeriodEnd.Value >= monthStart)
            || (e.PeriodStart != null && e.PeriodEnd == null && e.PeriodStart.Value >= monthStart && e.PeriodStart.Value <= monthEnd)
            || (e.PeriodStart == null && e.PeriodEnd != null && e.PeriodEnd.Value >= monthStart && e.PeriodEnd.Value <= monthEnd));
    }
}
