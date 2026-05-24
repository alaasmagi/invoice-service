using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class ContactEntity : BaseEntityUserWithMetaConcurrency
{
    public string FullName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string? Phone { get; set; }

    public ICollection<AddressContactEntity> AddressContacts { get; set; }
        = new List<AddressContactEntity>();

    public ICollection<InvoiceAllocationEntity> InvoiceAllocations { get; set; }
        = new List<InvoiceAllocationEntity>();

    public ICollection<InvoiceAllocationEntity> MonthlyStatementContacts { get; set; }
        = new List<InvoiceAllocationEntity>();
}