using Base.Domain;

namespace Domain;

/// <summary>One address charge parsed from an invoice document and what happened to it.</summary>
public class InvoiceDocumentRow : BaseEntity
{
    public Guid InvoiceDocumentId { get; set; }

    public int Position { get; set; }

    public string? AddressText { get; set; }

    public decimal? Amount { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public EInvoiceDocumentRowOutcome Outcome { get; set; }

    public string? Reason { get; set; }

    public Guid? InvoiceId { get; set; }
}
