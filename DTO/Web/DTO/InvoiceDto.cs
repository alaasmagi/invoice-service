using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class InvoiceDto : BaseEntityUserWithConcurrency
{
    public Guid ServiceId { get; set; }

    public ServiceDto Service { get; set; } = default!;

    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal TotalSum { get; set; }

    public ICollection<InvoiceAllocationDto> Allocations { get; set; }
        = new List<InvoiceAllocationDto>();
}