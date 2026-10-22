using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class InvoiceDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Service")]
    public Guid ServiceId { get; set; }

    public ServiceDto Service { get; set; } = default!;

    [Display(Name = "Address")]
    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    [Display(Name = "Invoice date")]
    public DateOnly InvoiceDate { get; set; }

    [Display(Name = "Period start")]
    public DateOnly? PeriodStart { get; set; }

    [Display(Name = "Period end")]
    public DateOnly? PeriodEnd { get; set; }

    [Display(Name = "Total")]
    public decimal TotalSum { get; set; }

    public ICollection<InvoiceAllocationDto> Allocations { get; set; }
        = new List<InvoiceAllocationDto>();
}