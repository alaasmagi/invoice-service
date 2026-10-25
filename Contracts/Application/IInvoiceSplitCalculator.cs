using Domain;

namespace Contracts.Application;

/// <summary>Splits one invoice between people: manual allocations, then a service rule, then the address default, then equally.</summary>
public interface IInvoiceSplitCalculator
{
    InvoiceSplitResult Calculate(InvoiceSplitInput input);

    /// <summary>The rule that decides an invoice when it has no manual allocations: its service rule, else the address default.</summary>
    AllocationRule? ResolveRule(Invoice invoice, IEnumerable<AllocationRule> rules);
}
