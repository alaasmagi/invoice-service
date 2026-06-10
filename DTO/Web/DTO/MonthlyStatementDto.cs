using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class MonthlyStatementDto : BaseEntityUserWithConcurrency
{
    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSum { get; set; }

    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public ICollection<MonthlyStatementLineDto> Lines { get; set; }
        = new List<MonthlyStatementLineDto>();

    public string Period => $"{Year:D4}-{Month:D2}";
}
