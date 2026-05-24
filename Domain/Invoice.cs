using Base.Domain;

namespace Domain;

public class Invoice : BaseEntityUser
{
    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = default!;

    public Guid AddressId { get; set; }

    public Address Address { get; set; } = default!;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal TotalSum { get; set; }

    public ICollection<InvoiceAllocation> Allocations { get; set; }
        = new List<InvoiceAllocation>();
}