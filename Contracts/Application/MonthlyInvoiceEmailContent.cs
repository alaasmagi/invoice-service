namespace Contracts.Application;

public sealed class MonthlyInvoiceEmailContent
{
    public string Email { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string Locale { get; init; } = "et";
    public string InvoiceIdentifier { get; init; } = default!;
    public string InvoicePeriod { get; init; } = default!;
    public string InvoiceDate { get; init; } = default!;
    public string InvoiceDueDate { get; init; } = default!;
    public string InvoiceTotal { get; init; } = default!;
    public string Currency { get; init; } = "€";
    public IReadOnlyList<MonthlyInvoiceEmailRowContent> InvoiceRows { get; init; } = [];
    public string PaymentFullName { get; init; } = default!;
    public string PaymentIban { get; init; } = default!;
    public string PaymentDescription { get; init; } = default!;
}

public sealed class MonthlyInvoiceEmailRowContent
{
    public string Address { get; init; } = default!;
    public string Service { get; init; } = default!;
    public string PeriodStart { get; init; } = default!;
    public string PeriodEnd { get; init; } = default!;
    public string InvoiceAmount { get; init; } = default!;
    public int Fractions { get; init; }
    public string InvoiceFraction { get; init; } = default!;
}
