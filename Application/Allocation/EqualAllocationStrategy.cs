using Contracts.Application;
using Domain;

namespace Application.Allocation;

public class EqualAllocationStrategy : IAllocationStrategy
{
    public EAllocationMethod Method => EAllocationMethod.Equal;

    public InvoiceSplitResult Split(InvoiceSplitInput input, AllocationRule? rule)
    {
        var shares = MoneySplitter.SplitEvenly(input.Invoice.TotalSum, input.ActiveResidentIds.Distinct().ToList())
            .Select(share => new InvoiceShare { ContactId = share.ContactId, Amount = share.Amount })
            .ToList();

        return new InvoiceSplitResult
        {
            Method = EAllocationMethod.Equal,
            RuleId = rule?.Id,
            Shares = shares
        };
    }
}
