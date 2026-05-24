using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class InvoiceAllocationDto : BaseEntityUserWithConcurrency
{
    public Guid InvoiceId { get; set; }

    public InvoiceDto Invoice { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    public decimal AllocatedSum { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}