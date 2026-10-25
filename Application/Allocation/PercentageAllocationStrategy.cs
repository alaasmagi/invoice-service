using Contracts.Application;
using Domain;

namespace Application.Allocation;

/// <summary>
/// Splits by the rule's percentages among listed contacts who are active residents. Percentages of contacts who
/// moved out are spread over the rest; active residents missing from the rule pay nothing and are reported.
/// </summary>
public class PercentageAllocationStrategy(EqualAllocationStrategy equal) : IAllocationStrategy
{
    public EAllocationMethod Method => EAllocationMethod.Percentage;

    public InvoiceSplitResult Split(InvoiceSplitInput input, AllocationRule? rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var active = input.ActiveResidentIds.ToHashSet();
        var participants = rule.Shares
            .Where(share => active.Contains(share.ContactId) && share.Value > 0)
            .Select(share => (share.ContactId, Weight: share.Value))
            .ToList();

        if (participants.Count == 0)
        {
            var fallback = equal.Split(input, null);
            fallback.Warnings = [new AllocationWarning { InvoiceId = input.Invoice.Id, Kind = EAllocationWarningKind.NoListedResidentActive, RuleId = rule.Id }];
            return fallback;
        }

        var weightSum = participants.Sum(participant => participant.Weight);
        var shares = MoneySplitter.Split(input.Invoice.TotalSum, participants)
            .Select(share => new InvoiceShare
            {
                ContactId = share.ContactId,
                Amount = share.Amount,
                ShareValue = decimal.Round(participants.First(p => p.ContactId == share.ContactId).Weight * 100m / weightSum, 2, MidpointRounding.AwayFromZero)
            })
            .ToList();

        var listed = rule.Shares.Select(share => share.ContactId).ToHashSet();
        var warnings = input.ActiveResidentIds
            .Distinct()
            .Where(contactId => !listed.Contains(contactId))
            .OrderBy(contactId => contactId)
            .Select(contactId => new AllocationWarning
            {
                InvoiceId = input.Invoice.Id,
                Kind = EAllocationWarningKind.UnlistedResident,
                ContactId = contactId,
                RuleId = rule.Id
            })
            .ToList();

        return new InvoiceSplitResult
        {
            Method = EAllocationMethod.Percentage,
            RuleId = rule.Id,
            Shares = shares,
            Warnings = warnings
        };
    }
}
