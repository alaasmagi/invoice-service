using Base.Domain;

namespace Domain;

public class AddressContact : BaseEntityUser
{
    public Guid AddressId { get; set; }

    public Address Address { get; set; } = default!;

    public Guid ContactId { get; set; }

    public Contact Contact { get; set; } = default!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive(DateOnly date)
    {
        return StartDate <= date
               && (EndDate == null || EndDate >= date);
    }

    public bool IsActiveDuring(DateOnly periodStart, DateOnly periodEnd)
    {
        return StartDate <= periodEnd
               && (EndDate == null || EndDate >= periodStart);
    }
}
