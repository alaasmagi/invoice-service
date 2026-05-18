using Base.Domain;

namespace Domain;

public class AddressContact : BaseEntity
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
}