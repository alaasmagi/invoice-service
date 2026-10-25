using Contracts.Application;
using Domain;

namespace Application.Allocation;

/// <summary>
/// Picks how an invoice is split — manual allocations, then a rule for the address and service, then the address
/// default rule, then an equal split — and hands it to the matching strategy.
/// </summary>
public class InvoiceSplitCalculator(IEnumerable<IAllocationStrategy> strategies) : IInvoiceSplitCalculator
{
    private readonly Dictionary<EAllocationMethod, IAllocationStrategy> _strategies = strategies.ToDictionary(strategy => strategy.Method);

    public InvoiceSplitResult Calculate(InvoiceSplitInput input)
    {
        if (input.ManualAllocations.Count > 0)
        {
            return _strategies[EAllocationMethod.Manual].Split(input, null);
        }

        var rule = ResolveRule(input.Invoice, input.Rules);
        if (input.ActiveResidentIds.Count == 0)
        {
            return new InvoiceSplitResult
            {
                Method = rule?.Method ?? EAllocationMethod.Equal,
                RuleId = rule?.Id,
                Warnings = [new AllocationWarning { InvoiceId = input.Invoice.Id, Kind = EAllocationWarningKind.NoActiveResidents }]
            };
        }

        return rule == null
            ? _strategies[EAllocationMethod.Equal].Split(input, null)
            : _strategies[rule.Method].Split(input, rule);
    }

    public AllocationRule? ResolveRule(Invoice invoice, IEnumerable<AllocationRule> rules)
    {
        var candidates = rules
            .Where(rule => rule.AddressId == invoice.AddressId
                           && rule.Method != EAllocationMethod.Manual
                           && rule.IsValidOn(invoice.InvoiceDate))
            .OrderByDescending(rule => rule.ValidFrom)
            .ToList();

        return candidates.FirstOrDefault(rule => rule.ServiceId == invoice.ServiceId)
               ?? candidates.FirstOrDefault(rule => rule.ServiceId == null);
    }
}
