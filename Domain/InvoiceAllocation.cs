using Base.Domain;

namespace Domain;

public class InvoiceAllocation : BaseEntity
{
    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = default!;

    public Guid ContactId { get; set; }

    public Contact Contact { get; set; } = default!;

    public decimal AllocatedSum { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}