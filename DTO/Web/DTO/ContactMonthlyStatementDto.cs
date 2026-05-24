using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class ContactMonthlyStatementDto : BaseEntityUserWithConcurrency
{
    public Guid MonthlyStatementId { get; set; }

    public MonthlyStatementDto MonthlyStatement { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    public decimal Amount { get; set; }

    public bool EmailSent { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public bool Paid { get; set; }

    public DateTime? PaidAt { get; set; }
}