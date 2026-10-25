using Contracts.Application;
using Domain;

namespace Application.Allocation;

/// <summary>
/// Listed active contacts pay their fixed amount; the remainder is split equally between active residents not listed.
/// Nothing is allocated when the fixed amounts exceed the invoice or a remainder is left with nobody to pay it.
/// </summary>
public class FixedAllocationStrategy(EqualAllocationStrategy equal) : IAllocationStrategy
{
    public EAllocationMethod Method => EAllocationMethod.Fixed;

    public InvoiceSplitResult Split(InvoiceSplitInput input, AllocationRule? rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var invoice = input.Invoice;
        var active = input.ActiveResidentIds.Distinct().ToList();
        var fixedPayers = rule.Shares
            .Where(share => active.Contains(share.ContactId))
            .OrderBy(share => share.ContactId)
            .ToList();

        if (fixedPayers.Count == 0)
        {
            var fallback = equal.Split(input, null);
            fallback.Warnings = [new AllocationWarning { InvoiceId = invoice.Id, Kind = EAllocationWarningKind.NoListedResidentActive, RuleId = rule.Id }];
            return fallback;
        }

        var fixedTotal = fixedPayers.Sum(share => share.Value);
        if (fixedTotal > invoice.TotalSum)
        {
            return Unallocated(invoice.Id, rule.Id, EAllocationWarningKind.FixedExceedsTotal, fixedTotal);
        }

        var remainder = invoice.TotalSum - fixedTotal;
        var listed = rule.Shares.Select(share => share.ContactId).ToHashSet();
        var remainderPayers = active.Where(contactId => !listed.Contains(contactId)).ToList();
        if (remainder != 0 && remainderPayers.Count == 0)
        {
            return Unallocated(invoice.Id, rule.Id, EAllocationWarningKind.RemainderWithoutPayer, remainder);
        }

        var shares = fixedPayers
            .Select(share => new InvoiceShare { ContactId = share.ContactId, Amount = share.Value, ShareValue = share.Value })
            .ToList();
        if (remainder != 0)
        {
            shares.AddRange(MoneySplitter.SplitEvenly(remainder, remainderPayers)
                .Select(share => new InvoiceShare { ContactId = share.ContactId, Amount = share.Amount }));
        }

        return new InvoiceSplitResult
        {
            Method = EAllocationMethod.Fixed,
            RuleId = rule.Id,
            Shares = shares
        };
    }

    private static InvoiceSplitResult Unallocated(Guid invoiceId, Guid ruleId, EAllocationWarningKind kind, decimal amount)
    {
        return new InvoiceSplitResult
        {
            Method = EAllocationMethod.Fixed,
            RuleId = ruleId,
            Warnings = [new AllocationWarning { InvoiceId = invoiceId, Kind = kind, Amount = amount, RuleId = ruleId }]
        };
    }
}
