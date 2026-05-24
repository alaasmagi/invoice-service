using Base.Domain;
using Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class MonthlyStatementEntity : BaseEntityUserWithMetaConcurrency
{
    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = default!;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSum { get; set; }

    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public ICollection<InvoiceEntity> Invoices { get; set; }
        = new List<InvoiceEntity>();

    public ICollection<ContactMonthlyStatementEntity> Contacts { get; set; }
        = new List<ContactMonthlyStatementEntity>();

    public string Period => $"{Year:D4}-{Month:D2}";
}