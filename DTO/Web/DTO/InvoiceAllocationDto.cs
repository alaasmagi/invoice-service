using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class InvoiceAllocationDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Invoice")]
    public Guid InvoiceId { get; set; }

    public InvoiceDto Invoice { get; set; } = default!;

    [Display(Name = "Contact")]
    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    [Display(Name = "Amount")]
    public decimal AllocatedSum { get; set; }

    public bool IsOverride { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}