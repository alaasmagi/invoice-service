using Base.Domain;

namespace Domain;

/// <summary>One person's part of an allocation rule: a percentage or a fixed amount, depending on the rule's method.</summary>
public class AllocationRuleShare : BaseEntityUser
{
    public Guid AllocationRuleId { get; set; }

    public AllocationRule AllocationRule { get; set; } = default!;

    public Guid ContactId { get; set; }

    public Contact Contact { get; set; } = default!;

    public decimal Value { get; set; }
}
