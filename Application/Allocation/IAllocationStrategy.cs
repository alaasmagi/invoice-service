using Contracts.Application;
using Domain;

namespace Application.Allocation;

/// <summary>Calculates the shares for one allocation method. Strategies are pure and registered explicitly.</summary>
public interface IAllocationStrategy
{
    EAllocationMethod Method { get; }

    /// <param name="rule">The rule that applies, or null for the Equal fallback and Manual allocations.</param>
    InvoiceSplitResult Split(InvoiceSplitInput input, AllocationRule? rule);
}
