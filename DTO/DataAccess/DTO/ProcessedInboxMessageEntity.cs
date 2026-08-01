using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class ProcessedInboxMessageEntity : BaseEntity
{
    public string EnvelopeId { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string Action { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
