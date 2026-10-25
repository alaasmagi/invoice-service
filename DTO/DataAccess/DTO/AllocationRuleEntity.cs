using Base.Domain;
using Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class AllocationRuleEntity : BaseEntityUserWithMetaConcurrency
{
    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = default!;

    public Guid? ServiceId { get; set; }

    public ServiceEntity? Service { get; set; }

    public EAllocationMethod Method { get; set; } = EAllocationMethod.Equal;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public ICollection<AllocationRuleShareEntity> Shares { get; set; }
        = new List<AllocationRuleShareEntity>();
}
