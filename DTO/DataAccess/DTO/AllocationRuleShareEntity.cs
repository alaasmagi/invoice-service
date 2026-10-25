using Base.Domain;

namespace DTO.DataAccess.DataAccess.DTO;

public class AllocationRuleShareEntity : BaseEntityUserWithMetaConcurrency
{
    public Guid AllocationRuleId { get; set; }

    public AllocationRuleEntity AllocationRule { get; set; } = default!;

    public Guid ContactId { get; set; }

    public ContactEntity Contact { get; set; } = default!;

    public decimal Value { get; set; }
}
