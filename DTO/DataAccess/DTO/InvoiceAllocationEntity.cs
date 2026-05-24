using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class InvoiceAllocationEntity : BaseEntityUserWithMetaConcurrency
{
    public Guid InvoiceId { get; set; }

    public InvoiceEntity Invoice { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactEntity Contact { get; set; } = default!;

    public decimal AllocatedSum { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}