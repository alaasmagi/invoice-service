using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class AddressEntity : BaseEntity
{
    public string Name { get; set; } = default!;
    public string FullAddress { get; set; } = default!;
    
    public ICollection<AddressContactEntity> AddressContacts { get; set; }
        = new List<AddressContactEntity>();

    public ICollection<InvoiceEntity> Invoices { get; set; }
        = new List<InvoiceEntity>();

    public ICollection<MonthlyStatementEntity> MonthlyStatements { get; set; }
        = new List<MonthlyStatementEntity>();
}