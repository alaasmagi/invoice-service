using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class AddressContactEntity : BaseEntity
{
    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactEntity Contact { get; set; } = default!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive(DateOnly date)
    {
        return StartDate <= date
               && (EndDate == null || EndDate >= date);
    }
}