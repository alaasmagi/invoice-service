using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class AddressContactDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Address")]
    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    [Display(Name = "Resident")]
    public Guid ContactId { get; set; }

    public ContactDto Contact { get; set; } = default!;

    [Display(Name = "Moved in")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "Moved out")]
    public DateOnly? EndDate { get; set; }

    public bool IsActive(DateOnly date)
    {
        return StartDate <= date
               && (EndDate == null || EndDate >= date);
    }
}