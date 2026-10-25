using Base.Contracts.DTO;
using Base.Domain;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;

namespace Application.Allocation;

public class AllocationRuleValidator(
    IAddressRepository addressRepository,
    IServiceRepository serviceRepository,
    IContactRepository contactRepository,
    IAddressContactRepository addressContactRepository,
    IAllocationRuleRepository allocationRuleRepository) : IAllocationRuleValidator
{
    public async Task<IReadOnlyList<AllocationRuleError>> ValidateAsync(AllocationRule rule, Guid userId)
    {
        var errors = new List<AllocationRuleError>();
        var shares = rule.Shares.ToList();

        if (!await OwnsAsync(addressRepository.GetAllAsync(userId), rule.AddressId))
        {
            errors.Add(new AllocationRuleError { Field = nameof(rule.AddressId), Kind = EAllocationRuleErrorKind.AddressNotFound });
            return errors;
        }

        if (rule.ServiceId != null && !await OwnsAsync(serviceRepository.GetAllAsync(userId), rule.ServiceId.Value))
        {
            errors.Add(new AllocationRuleError { Field = nameof(rule.ServiceId), Kind = EAllocationRuleErrorKind.ServiceNotFound });
        }

        if (rule.ValidTo != null && rule.ValidTo < rule.ValidFrom)
        {
            errors.Add(new AllocationRuleError { Field = nameof(rule.ValidTo), Kind = EAllocationRuleErrorKind.ValidToBeforeValidFrom });
        }

        errors.AddRange(ValidateShares(rule.Method, shares));
        errors.AddRange(await ValidateContactsAsync(rule, shares, userId));
        errors.AddRange(await ValidateOverlapAsync(rule, userId));
        return errors;
    }

    private static IEnumerable<AllocationRuleError> ValidateShares(EAllocationMethod method, IReadOnlyList<AllocationRuleShare> shares)
    {
        switch (method)
        {
            case EAllocationMethod.Manual:
                yield return new AllocationRuleError { Field = "Method", Kind = EAllocationRuleErrorKind.ManualNotAllowed };
                yield break;
            case EAllocationMethod.Equal:
                if (shares.Count > 0) yield return new AllocationRuleError { Kind = EAllocationRuleErrorKind.EqualHasShares };
                yield break;
        }

        if (shares.Count == 0)
        {
            yield return new AllocationRuleError { Kind = EAllocationRuleErrorKind.SharesRequired };
            yield break;
        }

        for (var i = 0; i < shares.Count; i++)
        {
            var value = shares[i].Value;
            if (method == EAllocationMethod.Percentage && (value <= 0 || value > 100))
            {
                yield return new AllocationRuleError { Field = ShareField(i, nameof(AllocationRuleShare.Value)), Kind = EAllocationRuleErrorKind.PercentageOutOfRange };
            }
            else if (method == EAllocationMethod.Fixed && value <= 0)
            {
                yield return new AllocationRuleError { Field = ShareField(i, nameof(AllocationRuleShare.Value)), Kind = EAllocationRuleErrorKind.FixedAmountNotPositive };
            }
        }

        var total = shares.Sum(share => share.Value);
        if (method == EAllocationMethod.Percentage && decimal.Round(total, 2) != 100m)
        {
            yield return new AllocationRuleError { Kind = EAllocationRuleErrorKind.PercentageTotalNot100, Amount = total };
        }
    }

    private async Task<IEnumerable<AllocationRuleError>> ValidateContactsAsync(AllocationRule rule, IReadOnlyList<AllocationRuleShare> shares, Guid userId)
    {
        var errors = new List<AllocationRuleError>();
        if (shares.Count == 0)
        {
            return errors;
        }

        var contactsResponse = await contactRepository.GetAllAsync(userId);
        var contactIds = contactsResponse.Successful && contactsResponse.Value != null
            ? contactsResponse.Value.Select(contact => contact.Id).ToHashSet()
            : [];
        var residenciesResponse = await addressContactRepository.GetAllAsync(userId);
        var residencies = residenciesResponse.Successful && residenciesResponse.Value != null
            ? residenciesResponse.Value.Where(residency => residency.AddressId == rule.AddressId).ToList()
            : [];
        var ruleEnd = rule.ValidTo ?? DateOnly.MaxValue;

        var seen = new HashSet<Guid>();
        for (var i = 0; i < shares.Count; i++)
        {
            var contactId = shares[i].ContactId;
            var field = ShareField(i, nameof(AllocationRuleShare.ContactId));
            if (!contactIds.Contains(contactId))
            {
                errors.Add(new AllocationRuleError { Field = field, Kind = EAllocationRuleErrorKind.ContactNotFound });
            }
            else if (!seen.Add(contactId))
            {
                errors.Add(new AllocationRuleError { Field = field, Kind = EAllocationRuleErrorKind.DuplicateContact });
            }
            else if (!residencies.Any(residency => residency.ContactId == contactId && residency.IsActiveDuring(rule.ValidFrom, ruleEnd)))
            {
                errors.Add(new AllocationRuleError { Field = field, Kind = EAllocationRuleErrorKind.ContactNotResident });
            }
        }

        return errors;
    }

    private async Task<IEnumerable<AllocationRuleError>> ValidateOverlapAsync(AllocationRule rule, Guid userId)
    {
        var rules = await allocationRuleRepository.GetForUserWithSharesAsync(userId);
        var conflict = rules
            .Where(other => other.Id != rule.Id
                            && other.AddressId == rule.AddressId
                            && other.ServiceId == rule.ServiceId
                            && other.Overlaps(rule.ValidFrom, rule.ValidTo))
            .OrderBy(other => other.ValidFrom)
            .FirstOrDefault();

        return conflict == null
            ? []
            : [new AllocationRuleError { Kind = EAllocationRuleErrorKind.OverlappingRule, ConflictingRuleId = conflict.Id }];
    }

    private static async Task<bool> OwnsAsync<T>(Task<IMethodResponse<IEnumerable<T>>> query, Guid id)
        where T : BaseEntityUser
    {
        var response = await query;
        return response.Successful && response.Value != null && response.Value.Any(entity => entity.Id == id);
    }

    private static string ShareField(int index, string property) => $"Shares[{index}].{property}";
}
