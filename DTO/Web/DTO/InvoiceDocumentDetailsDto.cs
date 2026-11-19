using Domain;

namespace DTO.DataAccess.Web.DTO;

/// <summary>An invoice document as the web UI shows it: status, timing and the outcome of each parsed row.</summary>
public class InvoiceDocumentDetailsDto
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = default!;

    public long SizeBytes { get; set; }

    public EInvoiceDocumentStatus Status { get; set; }

    public bool IsTimedOut { get; set; }

    public bool CanRetry { get; set; }

    public string? FailureReason { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? ParseRequestedAt { get; set; }

    public string? ServiceName { get; set; }

    public IReadOnlyList<InvoiceDocumentRowDto> Rows { get; set; } = [];
}

public class InvoiceDocumentRowDto
{
    public Guid Id { get; set; }

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
