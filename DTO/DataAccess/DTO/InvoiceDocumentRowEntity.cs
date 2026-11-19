using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class InvoiceDocumentRowEntity : BaseEntityWithMeta
{
    public Guid InvoiceDocumentId { get; set; }

    public InvoiceDocumentEntity InvoiceDocument { get; set; } = default!;

    public int Position { get; set; }

    [MaxLength(512)]
    public string? AddressText { get; set; }

    public decimal? Amount { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public EInvoiceDocumentRowOutcome Outcome { get; set; }

    [MaxLength(512)]
    public string? Reason { get; set; }

    public Guid? InvoiceId { get; set; }

    public InvoiceEntity? Invoice { get; set; }
}
