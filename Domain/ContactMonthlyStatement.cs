using Base.Domain;

namespace Domain;

public class ContactMonthlyStatement : BaseEntityUser
{
    public Guid MonthlyStatementId { get; set; }

    public MonthlyStatement MonthlyStatement { get; set; } = default!;

    public Guid ContactId { get; set; }

    public Contact Contact { get; set; } = default!;

    public decimal Amount { get; set; }

    public bool EmailSent { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public bool Paid { get; set; }

    public DateTime? PaidAt { get; set; }
}