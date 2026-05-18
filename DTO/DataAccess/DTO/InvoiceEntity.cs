using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class InvoiceEntity : BaseEntity
{
    public Guid ServiceId { get; set; }

    public ServiceEntity Service { get; set; } = default!;

    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = default!;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal TotalSum { get; set; }

    public ICollection<InvoiceAllocationEntity> Allocations { get; set; }
        = new List<InvoiceAllocationEntity>();
}