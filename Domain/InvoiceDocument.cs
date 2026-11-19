using Base.Domain;

namespace Domain;

/// <summary>An uploaded invoice PDF kept in object storage and the state of its AI parsing.</summary>
public class InvoiceDocument : BaseEntityUser
{
    public string StorageKey { get; set; } = default!;

    public string FileName { get; set; } = default!;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = default!;

    public EInvoiceDocumentStatus Status { get; set; } = EInvoiceDocumentStatus.Uploaded;

    public string? FailureReason { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? ParseRequestedAt { get; set; }

    public DateTime? ParseExpiresAt { get; set; }

    public string? ServiceName { get; set; }

    public ICollection<InvoiceDocumentRow> Rows { get; set; } = new List<InvoiceDocumentRow>();

    public bool IsParseTimedOut(DateTime utcNow)
    {
        return Status == EInvoiceDocumentStatus.Processing
               && ParseExpiresAt != null
               && ParseExpiresAt <= utcNow;
    }

    public bool CanRetry(DateTime utcNow)
    {
        return Status is EInvoiceDocumentStatus.Uploaded or EInvoiceDocumentStatus.Failed
               || IsParseTimedOut(utcNow);
    }
}
