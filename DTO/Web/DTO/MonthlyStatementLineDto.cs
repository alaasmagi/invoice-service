using Base.Domain;

namespace DTO.DataAccess.Web.DTO;

public class MonthlyStatementLineDto : BaseEntityUserWithConcurrency
{
    public Guid MonthlyStatementId { get; set; }

    public MonthlyStatementDto MonthlyStatement { get; set; } = default!;

    public Guid InvoiceId { get; set; }

    public InvoiceDto Invoice { get; set; } = default!;

    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    public string AddressName { get; set; } = default!;

    public Guid ServiceId { get; set; }

    public ServiceDto Service { get; set; } = default!;

    public string ServiceName { get; set; } = default!;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal InvoiceTotal { get; set; }

    public int ResidentCount { get; set; }

    public decimal AllocatedAmount { get; set; }
}
