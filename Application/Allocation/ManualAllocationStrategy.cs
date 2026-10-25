using Contracts.Application;
using Domain;

namespace Application.Allocation;

/// <summary>Uses the invoice's manual allocations as they are; they must add up exactly to the invoice total.</summary>
public class ManualAllocationStrategy : IAllocationStrategy
{
    public EAllocationMethod Method => EAllocationMethod.Manual;

    public InvoiceSplitResult Split(InvoiceSplitInput input, AllocationRule? rule)
    {
        var invoice = input.Invoice;
        var shares = input.ManualAllocations
            .GroupBy(allocation => allocation.ContactId)
            .OrderBy(group => group.Key)
            .Select(group => new InvoiceShare { ContactId = group.Key, Amount = group.Sum(allocation => allocation.AllocatedSum) })
            .ToList();

        var unallocated = invoice.TotalSum - shares.Sum(share => share.Amount);
        if (unallocated != 0)
        {
            return new InvoiceSplitResult
            {
                Method = EAllocationMethod.Manual,
                Warnings = [new AllocationWarning { InvoiceId = invoice.Id, Kind = EAllocationWarningKind.ManualSumMismatch, Amount = unallocated }]
            };
        }

        return new InvoiceSplitResult
        {
            Method = EAllocationMethod.Manual,
            Shares = shares
        };
    }
}
