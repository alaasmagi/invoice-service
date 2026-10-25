using Application.Allocation;
using Contracts.Application;
using Domain;
using Xunit;

namespace Tests;

public class MoneySplitterTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    [Fact]
    public void SplitEvenly_ThreeWays_AddsUpExactlyAndGivesExtraCentToFirstContact()
    {
        var shares = MoneySplitter.SplitEvenly(100m, [C, A, B]);

        Assert.Equal(100m, shares.Sum(share => share.Amount));
        Assert.Equal([(A, 33.34m), (B, 33.33m), (C, 33.33m)], shares);
    }

    [Fact]
    public void Split_IsDeterministic()
    {
        var first = MoneySplitter.Split(10m, [(A, 33.33m), (B, 33.33m), (C, 33.34m)]);
        var second = MoneySplitter.Split(10m, [(C, 33.34m), (B, 33.33m), (A, 33.33m)]);

        Assert.Equal(10m, first.Sum(share => share.Amount));
        Assert.Equal(first, second);
    }

    [Fact]
    public void Split_SingleParticipant_GetsEverything()
    {
        Assert.Equal([(A, 12.34m)], MoneySplitter.SplitEvenly(12.34m, [A]));
    }

    [Fact]
    public void Split_ZeroTotal_GivesZeroShares()
    {
        Assert.All(MoneySplitter.SplitEvenly(0m, [A, B]), share => Assert.Equal(0m, share.Amount));
    }

    [Fact]
    public void Split_NegativeTotal_AddsUpExactly()
    {
        var shares = MoneySplitter.SplitEvenly(-10m, [A, B, C]);

        Assert.Equal(-10m, shares.Sum(share => share.Amount));
    }

    [Fact]
    public void Split_NoParticipants_ReturnsEmpty()
    {
        Assert.Empty(MoneySplitter.SplitEvenly(10m, []));
    }
}

public class InvoiceSplitCalculatorTests
{
    private static readonly Guid AddressId = Guid.NewGuid();
    private static readonly Guid ElectricityId = Guid.NewGuid();
    private static readonly Guid InternetId = Guid.NewGuid();
    private static readonly Guid Mari = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Jaan = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Kati = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Peeter = Guid.Parse("00000000-0000-0000-0000-000000000004");

    public static InvoiceSplitCalculator CreateCalculator()
    {
        var equal = new EqualAllocationStrategy();
        return new InvoiceSplitCalculator([equal, new PercentageAllocationStrategy(equal), new FixedAllocationStrategy(equal), new ManualAllocationStrategy()]);
    }

    private static Invoice Invoice(decimal total, Guid? serviceId = null, DateOnly? date = null) => new()
    {
        Id = Guid.NewGuid(),
        AddressId = AddressId,
        ServiceId = serviceId ?? ElectricityId,
        InvoiceDate = date ?? new DateOnly(2026, 11, 15),
        TotalSum = total
    };

    private static AllocationRule Rule(EAllocationMethod method, Guid? serviceId = null, DateOnly? from = null, DateOnly? to = null, params (Guid ContactId, decimal Value)[] shares) => new()
    {
        Id = Guid.NewGuid(),
        AddressId = AddressId,
        ServiceId = serviceId,
        Method = method,
        ValidFrom = from ?? new DateOnly(2026, 1, 1),
        ValidTo = to,
        Shares = shares.Select(share => new AllocationRuleShare { Id = Guid.NewGuid(), ContactId = share.ContactId, Value = share.Value }).ToList()
    };

    private static InvoiceSplitResult Calculate(Invoice invoice, IReadOnlyList<AllocationRule> rules, IReadOnlyList<Guid> residents, IReadOnlyList<InvoiceAllocation>? manual = null) =>
        CreateCalculator().Calculate(new InvoiceSplitInput
        {
            Invoice = invoice,
            Rules = rules,
            ActiveResidentIds = residents,
            ManualAllocations = manual ?? []
        });

    private static decimal AmountOf(InvoiceSplitResult result, Guid contactId) => result.Shares.Single(share => share.ContactId == contactId).Amount;

    [Fact]
    public void NoRule_SplitsEquallyAsBefore()
    {
        var result = Calculate(Invoice(100m), [], [Mari, Jaan, Kati]);

        Assert.Equal(EAllocationMethod.Equal, result.Method);
        Assert.Null(result.RuleId);
        Assert.Equal(100m, result.Shares.Sum(share => share.Amount));
        Assert.Equal(3, result.ParticipantCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void NoActiveResidents_IsNotAllocated()
    {
        var result = Calculate(Invoice(100m), [], []);

        Assert.Empty(result.Shares);
        Assert.Equal(EAllocationWarningKind.NoActiveResidents, Assert.Single(result.Warnings).Kind);
    }

    [Fact]
    public void Percentage_SplitsSixtyForty()
    {
        var rule = Rule(EAllocationMethod.Percentage, shares: [(Mari, 60m), (Jaan, 40m)]);

        var result = Calculate(Invoice(82.10m), [rule], [Mari, Jaan]);

        Assert.Equal(EAllocationMethod.Percentage, result.Method);
        Assert.Equal(rule.Id, result.RuleId);
        Assert.Equal(49.26m, AmountOf(result, Mari));
        Assert.Equal(32.84m, AmountOf(result, Jaan));
        Assert.Equal(60m, result.Shares.Single(share => share.ContactId == Mari).ShareValue);
    }

    [Fact]
    public void Percentage_MovedOutContact_ScalesUpTheOthers()
    {
        var rule = Rule(EAllocationMethod.Percentage, shares: [(Mari, 50m), (Jaan, 25m), (Kati, 25m)]);

        var result = Calculate(Invoice(90m), [rule], [Mari, Jaan]);

        Assert.Equal(60m, AmountOf(result, Mari));
        Assert.Equal(30m, AmountOf(result, Jaan));
        Assert.Equal(66.67m, result.Shares.Single(share => share.ContactId == Mari).ShareValue);
        Assert.Equal(33.33m, result.Shares.Single(share => share.ContactId == Jaan).ShareValue);
    }

    [Fact]
    public void Percentage_UnlistedResident_PaysNothingAndIsReported()
    {
        var rule = Rule(EAllocationMethod.Percentage, shares: [(Mari, 60m), (Jaan, 40m)]);

        var result = Calculate(Invoice(50m), [rule], [Mari, Jaan, Peeter]);

        Assert.DoesNotContain(result.Shares, share => share.ContactId == Peeter);
        Assert.Equal(50m, result.Shares.Sum(share => share.Amount));
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(EAllocationWarningKind.UnlistedResident, warning.Kind);
        Assert.Equal(Peeter, warning.ContactId);
    }

    [Fact]
    public void Percentage_NoListedResidentActive_FallsBackToEqual()
    {
        var rule = Rule(EAllocationMethod.Percentage, shares: [(Mari, 100m)]);

        var result = Calculate(Invoice(40m), [rule], [Jaan, Kati]);

        Assert.Equal(EAllocationMethod.Equal, result.Method);
        Assert.Null(result.RuleId);
        Assert.Equal(20m, AmountOf(result, Jaan));
        Assert.Equal(EAllocationWarningKind.NoListedResidentActive, Assert.Single(result.Warnings).Kind);
    }

    [Fact]
    public void Fixed_ChargesFixedAmountAndSplitsRemainder()
    {
        var rule = Rule(EAllocationMethod.Fixed, InternetId, shares: [(Jaan, 10m)]);

        var result = Calculate(Invoice(40m, InternetId), [rule], [Mari, Jaan, Kati]);

        Assert.Equal(EAllocationMethod.Fixed, result.Method);
        Assert.Equal(10m, AmountOf(result, Jaan));
        Assert.Equal(15m, AmountOf(result, Mari));
        Assert.Equal(15m, AmountOf(result, Kati));
        Assert.Equal(10m, result.Shares.Single(share => share.ContactId == Jaan).ShareValue);
        Assert.Null(result.Shares.Single(share => share.ContactId == Mari).ShareValue);
    }

    [Fact]
    public void Fixed_AmountsExceedTotal_IsNotAllocated()
    {
        var rule = Rule(EAllocationMethod.Fixed, shares: [(Jaan, 30m), (Mari, 20m)]);

        var result = Calculate(Invoice(40m), [rule], [Mari, Jaan]);

        Assert.Empty(result.Shares);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(EAllocationWarningKind.FixedExceedsTotal, warning.Kind);
        Assert.Equal(50m, warning.Amount);
    }

    [Fact]
    public void Fixed_RemainderWithoutPayer_IsNotAllocated()
    {
        var rule = Rule(EAllocationMethod.Fixed, shares: [(Jaan, 10m)]);

        var result = Calculate(Invoice(40m), [rule], [Jaan]);

        Assert.Empty(result.Shares);
        Assert.Equal(EAllocationWarningKind.RemainderWithoutPayer, Assert.Single(result.Warnings).Kind);
    }

    [Fact]
    public void Manual_OverridesRules()
    {
        var invoice = Invoice(100m);
        var rule = Rule(EAllocationMethod.Percentage, shares: [(Mari, 50m), (Jaan, 50m)]);
        var manual = new List<InvoiceAllocation>
        {
            new() { InvoiceId = invoice.Id, ContactId = Mari, AllocatedSum = 70m, IsOverride = true },
            new() { InvoiceId = invoice.Id, ContactId = Peeter, AllocatedSum = 30m, IsOverride = true }
        };

        var result = Calculate(invoice, [rule], [Mari, Jaan], manual);

        Assert.Equal(EAllocationMethod.Manual, result.Method);
        Assert.Null(result.RuleId);
        Assert.Equal(70m, AmountOf(result, Mari));
        Assert.Equal(30m, AmountOf(result, Peeter));
    }

    [Fact]
    public void Manual_SumMismatch_IsNotAllocated()
    {
        var invoice = Invoice(100m);
        var manual = new List<InvoiceAllocation>
        {
            new() { InvoiceId = invoice.Id, ContactId = Mari, AllocatedSum = 60m, IsOverride = true },
            new() { InvoiceId = invoice.Id, ContactId = Jaan, AllocatedSum = 30m, IsOverride = true }
        };

        var result = Calculate(invoice, [], [Mari, Jaan], manual);

        Assert.Empty(result.Shares);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(EAllocationWarningKind.ManualSumMismatch, warning.Kind);
        Assert.Equal(10m, warning.Amount);
    }

    [Fact]
    public void ServiceRule_BeatsAddressDefault()
    {
        var addressDefault = Rule(EAllocationMethod.Equal);
        var electricity = Rule(EAllocationMethod.Percentage, ElectricityId, shares: [(Mari, 100m)]);

        var result = Calculate(Invoice(10m, ElectricityId), [addressDefault, electricity], [Mari, Jaan]);

        Assert.Equal(electricity.Id, result.RuleId);
        Assert.Equal(10m, AmountOf(result, Mari));
    }

    [Fact]
    public void AddressDefault_AppliesToOtherServices()
    {
        var addressDefault = Rule(EAllocationMethod.Percentage, shares: [(Mari, 75m), (Jaan, 25m)]);
        var electricity = Rule(EAllocationMethod.Equal, ElectricityId);

        var result = Calculate(Invoice(100m, InternetId), [addressDefault, electricity], [Mari, Jaan]);

        Assert.Equal(addressDefault.Id, result.RuleId);
        Assert.Equal(75m, AmountOf(result, Mari));
    }

    [Fact]
    public void RuleOutsideValidity_IsIgnored()
    {
        var future = Rule(EAllocationMethod.Percentage, from: new DateOnly(2026, 11, 1), shares: [(Mari, 100m)]);

        var result = Calculate(Invoice(10m, date: new DateOnly(2026, 10, 15)), [future], [Mari, Jaan]);

        Assert.Equal(EAllocationMethod.Equal, result.Method);
        Assert.Null(result.RuleId);
    }

    [Fact]
    public void RuleForAnotherAddress_IsIgnored()
    {
        var other = Rule(EAllocationMethod.Percentage, shares: [(Mari, 100m)]);
        other.AddressId = Guid.NewGuid();

        var result = Calculate(Invoice(10m), [other], [Mari, Jaan]);

        Assert.Null(result.RuleId);
    }
}
