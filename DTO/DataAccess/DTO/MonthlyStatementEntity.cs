using Base.Domain;
using Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class MonthlyStatementEntity : BaseEntityUserWithMetaConcurrency
{
    public Guid ContactId { get; set; }

    public ContactEntity Contact { get; set; } = default!;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSum { get; set; }

    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public ICollection<MonthlyStatementLineEntity> Lines { get; set; }
        = new List<MonthlyStatementLineEntity>();

    public string Period => $"{Year:D4}-{Month:D2}";
}
