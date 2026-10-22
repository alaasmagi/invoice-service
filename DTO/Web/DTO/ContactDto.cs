using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class ContactDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Full name")]
    public string FullName { get; set; } = default!;

    [Display(Name = "Email")]
    public string Email { get; set; } = default!;

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public ICollection<AddressContactDto> AddressContacts { get; set; }
        = new List<AddressContactDto>();

    public ICollection<InvoiceAllocationDto> InvoiceAllocations { get; set; }
        = new List<InvoiceAllocationDto>();

    public ICollection<MonthlyStatementDto> MonthlyStatements { get; set; }
        = new List<MonthlyStatementDto>();
}