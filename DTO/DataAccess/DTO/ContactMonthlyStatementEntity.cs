using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class ContactMonthlyStatementEntity : BaseEntity
{
    public Guid MonthlyStatementId { get; set; }

    public MonthlyStatementEntity MonthlyStatement { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactEntity Contact { get; set; } = default!;

    public decimal Amount { get; set; }

    public bool EmailSent { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public bool Paid { get; set; }

    public DateTime? PaidAt { get; set; }
}