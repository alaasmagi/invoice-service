using Base.Domain;

namespace Domain;

public class MonthlyStatementLine : BaseEntityUser
{
    public Guid MonthlyStatementId { get; set; }

    public MonthlyStatement MonthlyStatement { get; set; } = default!;

    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = default!;

    public Guid AddressId { get; set; }

    public Address Address { get; set; } = default!;

    public string AddressName { get; set; } = default!;

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = default!;

    public string ServiceName { get; set; } = default!;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal InvoiceTotal { get; set; }

    public int ResidentCount { get; set; }

    public decimal AllocatedAmount { get; set; }
}
