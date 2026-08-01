using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class PublishedInvoiceEmailEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MonthlyStatementId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string InvoiceIdentifier { get; set; } = default!;
    public string Recipient { get; set; } = default!;
    public string EnvelopeId { get; set; } = default!;
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}
