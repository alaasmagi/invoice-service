using Base.Domain;

namespace Domain;

public class Address : BaseEntity
{
    public string Name { get; set; } = default!;
    public string FullAddress { get; set; } = default!;
    
    public ICollection<AddressContact> AddressContacts { get; set; }
        = new List<AddressContact>();

    public ICollection<Invoice> Invoices { get; set; }
        = new List<Invoice>();

    public ICollection<MonthlyStatement> MonthlyStatements { get; set; }
        = new List<MonthlyStatement>();
}