using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class MonthlyStatementDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Person")]
    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    [Display(Name = "Year")]
    public int Year { get; set; }

    [Display(Name = "Month")]
    public int Month { get; set; }

    [Display(Name = "Total (€)")]
    public decimal TotalSum { get; set; }

    [Display(Name = "Status")]
    public EMonthlyStatementStatus Status { get; set; }
        = EMonthlyStatementStatus.Draft;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    [Display(Name = "Sent at")]
    public DateTime? SentAt { get; set; }

    public ICollection<MonthlyStatementLineDto> Lines { get; set; }
        = new List<MonthlyStatementLineDto>();

    public string Period => $"{Year:D4}-{Month:D2}";
}
