namespace Contracts.Application;

/// <summary>An invoice PDF's content as read by ai-hub: one service and its charges per address.</summary>
public class PdfInvoiceParsedDocument
{
    public string ServiceName { get; init; } = default!;
    public DateOnly? InvoiceDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public List<PdfInvoiceParsedRow> Rows { get; } = [];
}

public class PdfInvoiceParsedRow
{
    public string AddressText { get; init; } = default!;
    public decimal? Amount { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
}
