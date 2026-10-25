namespace Application.Allocation;

/// <summary>
/// Splits an amount into cent-exact parts proportional to weights (largest remainder method).
/// Parts always add up to the total; leftover cents go to the largest fractions, ties broken by contact id,
/// so the same input always produces the same output.
/// </summary>
public static class MoneySplitter
{
    public static IReadOnlyList<(Guid ContactId, decimal Amount)> Split(decimal total, IReadOnlyList<(Guid ContactId, decimal Weight)> weights)
    {
        if (weights.Count == 0)
        {
            return [];
        }

        var ordered = weights.OrderBy(weight => weight.ContactId).ToList();
        var weightSum = ordered.Sum(weight => weight.Weight);
        if (weightSum <= 0)
        {
            throw new ArgumentException("Weights must add up to more than zero.", nameof(weights));
        }

        var sign = total < 0 ? -1 : 1;
        var totalCents = decimal.Round(Math.Abs(total) * 100m, 0, MidpointRounding.AwayFromZero);
        var parts = ordered
            .Select((weight, index) =>
            {
                var exact = totalCents * weight.Weight / weightSum;
                var cents = decimal.Floor(exact);
                return (weight.ContactId, Index: index, Cents: cents, Fraction: exact - cents);
            })
            .ToList();

        var leftover = (int)(totalCents - parts.Sum(part => part.Cents));
        var receivers = parts
            .OrderByDescending(part => part.Fraction)
            .ThenBy(part => part.Index)
            .Take(leftover)
            .Select(part => part.Index)
            .ToHashSet();

        return parts
            .Select(part => (part.ContactId, sign * (part.Cents + (receivers.Contains(part.Index) ? 1 : 0)) / 100m))
            .ToList();
    }

    public static IReadOnlyList<(Guid ContactId, decimal Amount)> SplitEvenly(decimal total, IReadOnlyList<Guid> contactIds)
    {
        return Split(total, contactIds.Select(id => (id, 1m)).ToList());
    }
}
