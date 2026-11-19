using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class InvoiceDocumentEntity : BaseEntityUserWithMeta
{
    [MaxLength(512)]
    public string StorageKey { get; set; } = default!;

    [MaxLength(255)]
    public string FileName { get; set; } = default!;

    public long SizeBytes { get; set; }

    [MaxLength(64)]
    public string Sha256 { get; set; } = default!;

    public EInvoiceDocumentStatus Status { get; set; }

    [MaxLength(1024)]
    public string? FailureReason { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? ParseRequestedAt { get; set; }

    public DateTime? ParseExpiresAt { get; set; }

    [MaxLength(256)]
    public string? ServiceName { get; set; }

    public ICollection<InvoiceDocumentRowEntity> Rows { get; set; } = new List<InvoiceDocumentRowEntity>();
}
