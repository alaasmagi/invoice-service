using Web.Helpers;

namespace Web.Models;

public class StatisticsViewModel
{
    public static readonly IReadOnlyList<int> MonthOptions = [6, 12, 24];

    public int Months { get; init; }

    /// <summary>Months in range, oldest first; the last one is the current (possibly incomplete) month.</summary>
    public IReadOnlyList<MonthColumn> Columns { get; init; } = [];

    /// <summary>Chart series in fixed slot order (stable per service, never by rank).</summary>
    public IReadOnlyList<ChartSeries> Series { get; init; } = [];

    public decimal Total { get; init; }
    public decimal PreviousTotal { get; init; }
    public int InvoiceCount { get; init; }
    public decimal AveragePerMonth => Columns.Count == 0 ? 0 : Total / Columns.Count;
    public MonthColumn? HighestMonth => Columns.Where(c => c.Total > 0).MaxBy(c => c.Total);

    /// <summary>Change vs the previous range of equal length, or null when there's nothing to compare with.</summary>
    public decimal? ChangePercent => PreviousTotal == 0 ? null : (Total - PreviousTotal) / PreviousTotal * 100;

    public decimal AxisMax { get; init; }
    public IReadOnlyList<decimal> Ticks { get; init; } = [];

    public IReadOnlyList<BreakdownRow> ByService { get; init; } = [];
    public IReadOnlyList<BreakdownRow> ByAddress { get; init; } = [];
    public IReadOnlyList<PersonRow> ByPerson { get; init; } = [];

    public bool HasData => InvoiceCount > 0;
}

/// <summary>A chart series; Slot is 1–7 for the categorical palette, 0 for the folded "Other" series.</summary>
public record ChartSeries(int Index, string Name, int Slot);

public record MonthColumn(BillingPeriod Period, IReadOnlyList<decimal> Amounts)
{
    public decimal Total => Amounts.Sum();
}

public record BreakdownRow(Guid? Id, string Name, decimal Total, decimal PreviousTotal, int InvoiceCount, int Slot)
{
    public decimal? ChangePercent => PreviousTotal == 0 ? null : (Total - PreviousTotal) / PreviousTotal * 100;
}

public record PersonRow(Guid ContactId, string Name, int StatementCount, decimal Billed, decimal Sent)
{
    public decimal Outstanding => Billed - Sent;
}
