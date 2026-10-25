using Domain;

namespace Contracts.Application;

public interface IAllocationRuleValidator
{
    /// <summary>Checks a rule before it is saved. Share errors refer to the rule's shares in their given order.</summary>
    Task<IReadOnlyList<AllocationRuleError>> ValidateAsync(AllocationRule rule, Guid userId);
}

public enum EAllocationRuleErrorKind
{
    AddressNotFound,
    ServiceNotFound,
    ContactNotFound,
    ValidToBeforeValidFrom,
    ManualNotAllowed,
    EqualHasShares,
    SharesRequired,
    PercentageOutOfRange,
    PercentageTotalNot100,
    FixedAmountNotPositive,
    DuplicateContact,
    ContactNotResident,
    OverlappingRule
}

public class AllocationRuleError
{
    /// <summary>Property path such as "ValidTo" or "Shares[1].Value"; null for form-level errors.</summary>
    public string? Field { get; set; }

    public EAllocationRuleErrorKind Kind { get; set; }

    public decimal? Amount { get; set; }

    public Guid? ConflictingRuleId { get; set; }
}
