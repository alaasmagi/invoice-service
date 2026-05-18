using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class ContactDto : BaseEntity
{
    public string FullName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string? Phone { get; set; }

    public ICollection<AddressContactDto> AddressContacts { get; set; }
        = new List<AddressContactDto>();

    public ICollection<InvoiceAllocationDto> InvoiceAllocations { get; set; }
        = new List<InvoiceAllocationDto>();

    public ICollection<ContactMonthlyStatementDto> MonthlyStatementContacts { get; set; }
        = new List<ContactMonthlyStatementDto>();
}