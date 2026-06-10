using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class MonthlyStatementLineEntity : BaseEntityUserWithMetaConcurrency
{
    public Guid MonthlyStatementId { get; set; }

    public MonthlyStatementEntity MonthlyStatement { get; set; } = default!;

    public Guid InvoiceId { get; set; }

    public InvoiceEntity Invoice { get; set; } = default!;

    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = default!;

    public string AddressName { get; set; } = default!;

    public Guid ServiceId { get; set; }

    public ServiceEntity Service { get; set; } = default!;

    public string ServiceName { get; set; } = default!;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal InvoiceTotal { get; set; }

    public int ResidentCount { get; set; }

    public decimal AllocatedAmount { get; set; }
}
