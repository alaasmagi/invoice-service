namespace Contracts.Application;

public sealed class MonthlyStatementEmail
{
    public string ToEmail { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string Period { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<MonthlyStatementEmailLine> Lines { get; init; } = [];
}

public sealed class MonthlyStatementEmailLine
{
    public string AddressName { get; init; } = default!;
    public string ServiceName { get; init; } = default!;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public decimal InvoiceTotal { get; init; }
    public int ResidentCount { get; init; }
    public decimal ContactAmount { get; init; }
}
