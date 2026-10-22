using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class AddressDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Name")]
    public string Name { get; set; } = default!;
    [Display(Name = "Full address")]
    public string FullAddress { get; set; } = default!;
    
    public ICollection<AddressContactDto> AddressContacts { get; set; }
        = new List<AddressContactDto>();

    public ICollection<InvoiceDto> Invoices { get; set; }
        = new List<InvoiceDto>();

    public ICollection<MonthlyStatementDto> MonthlyStatements { get; set; }
        = new List<MonthlyStatementDto>();
}