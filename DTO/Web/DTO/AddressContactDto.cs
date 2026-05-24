using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class AddressContactDto : BaseEntityUserWithConcurrency
{
    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive(DateOnly date)
    {
        return StartDate <= date
               && (EndDate == null || EndDate >= date);
    }
}