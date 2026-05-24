using Base.Domain;

namespace Domain;

public class Contact : BaseEntityUser
{
    public string FullName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string? Phone { get; set; }

    public ICollection<AddressContact> AddressContacts { get; set; }
        = new List<AddressContact>();

    public ICollection<InvoiceAllocation> InvoiceAllocations { get; set; }
        = new List<InvoiceAllocation>();

    public ICollection<ContactMonthlyStatement> MonthlyStatementContacts { get; set; }
        = new List<ContactMonthlyStatement>();
}