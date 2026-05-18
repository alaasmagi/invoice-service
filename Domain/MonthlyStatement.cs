using Base.Domain;

namespace Domain;

public class MonthlyStatement : BaseEntity
{
    public Guid AddressId { get; set; }

    public Address Address { get; set; } = default!;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSum { get; set; }

    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public ICollection<Invoice> Invoices { get; set; }
        = new List<Invoice>();

    public ICollection<ContactMonthlyStatement> Contacts { get; set; }
        = new List<ContactMonthlyStatement>();

    public string Period => $"{Year:D4}-{Month:D2}";
}