using Domain;

namespace Contracts.Application;

/// <summary>Everything needed to split one invoice. Pure data: the calculator does no I/O.</summary>
public class InvoiceSplitInput
{
    public Invoice Invoice { get; set; } = default!;

    /// <summary>Manual splits for the invoice; when any exist they win over rules.</summary>
    public IReadOnlyList<InvoiceAllocation> ManualAllocations { get; set; } = [];

    /// <summary>Candidate rules (with shares). The calculator picks the one that applies to the invoice.</summary>
    public IReadOnlyList<AllocationRule> Rules { get; set; } = [];

    /// <summary>Contacts living at the invoice address on the invoice date.</summary>
    public IReadOnlyList<Guid> ActiveResidentIds { get; set; } = [];
}

public class InvoiceShare
{
    public Guid ContactId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Effective percentage for Percentage, the fixed amount for a fixed payer; null otherwise.</summary>
    public decimal? ShareValue { get; set; }
}

public class InvoiceSplitResult
{
    public EAllocationMethod Method { get; set; }

    public Guid? RuleId { get; set; }

    public IReadOnlyList<InvoiceShare> Shares { get; set; } = [];

    public IReadOnlyList<AllocationWarning> Warnings { get; set; } = [];

    public int ParticipantCount => Shares.Count;
}

public enum EAllocationWarningKind
{
    NoActiveResidents,
    UnlistedResident,
    NoListedResidentActive,
    FixedExceedsTotal,
    RemainderWithoutPayer,
    ManualSumMismatch
}

/// <summary>
/// An invoice that could not be allocated, or was allocated in a way the user should know about.
/// Application emits codes; Web turns them into localized text.
/// </summary>
public class AllocationWarning
{
    public Guid InvoiceId { get; set; }

    public EAllocationWarningKind Kind { get; set; }

    public Guid? ContactId { get; set; }

    public string? ContactName { get; set; }

    public decimal? Amount { get; set; }

    public Guid? RuleId { get; set; }

    public Guid AddressId { get; set; }

    public string AddressName { get; set; } = "";

    public string ServiceName { get; set; } = "";

    public DateOnly InvoiceDate { get; set; }
}
