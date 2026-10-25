using Base.Domain;

namespace Domain;

/// <summary>
/// How invoices of one address (optionally only one service) are split between its residents during a date range.
/// </summary>
public class AllocationRule : BaseEntityUser
{
    public Guid AddressId { get; set; }

    public Address Address { get; set; } = default!;

    /// <summary>Null means the rule applies to every service at the address.</summary>
    public Guid? ServiceId { get; set; }

    public Service? Service { get; set; }

    public EAllocationMethod Method { get; set; } = EAllocationMethod.Equal;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public ICollection<AllocationRuleShare> Shares { get; set; }
        = new List<AllocationRuleShare>();

    public bool IsValidOn(DateOnly date)
    {
        return ValidFrom <= date
               && (ValidTo == null || ValidTo >= date);
    }

    public bool Overlaps(DateOnly from, DateOnly? to)
    {
        return ValidFrom <= (to ?? DateOnly.MaxValue)
               && (ValidTo ?? DateOnly.MaxValue) >= from;
    }
}
