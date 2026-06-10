using Base.Domain;

namespace Domain;

public class MonthlyStatement : BaseEntityUser
{
    public Guid ContactId { get; set; }

    public Contact Contact { get; set; } = default!;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSum { get; set; }

    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public ICollection<MonthlyStatementLine> Lines { get; set; }
        = new List<MonthlyStatementLine>();

    public string Period => $"{Year:D4}-{Month:D2}";
}
