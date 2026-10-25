using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess.Web.DTO;

public class AllocationRuleDto : BaseEntityUserWithConcurrency
{
    [Display(Name = "Address")]
    public Guid AddressId { get; set; }

    public AddressDto Address { get; set; } = default!;

    [Display(Name = "Applies to")]
    public Guid? ServiceId { get; set; }

    public ServiceDto? Service { get; set; }

    [Display(Name = "Split method")]
    public EAllocationMethod Method { get; set; } = EAllocationMethod.Equal;

    [Display(Name = "Valid from")]
    public DateOnly ValidFrom { get; set; }

    [Display(Name = "Valid until")]
    public DateOnly? ValidTo { get; set; }

    public List<AllocationRuleShareDto> Shares { get; set; } = [];

    public bool IsValidOn(DateOnly date)
    {
        return ValidFrom <= date
               && (ValidTo == null || ValidTo >= date);
    }
}
