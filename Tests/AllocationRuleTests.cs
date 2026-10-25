using Application;
using Application.Allocation;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using DTO.DataAccess.Web.Mapper;
using Moq;
using Web.Mapping;
using Xunit;

namespace Tests;

internal static class RepositoryMocks
{
    public static Mock<TRepository> Returning<TRepository, TEntity>(IEnumerable<TEntity> entities)
        where TRepository : class, IBaseRepository<TEntity>
        where TEntity : class
    {
        var mock = new Mock<TRepository>();
        mock.Setup(repository => repository.GetAllAsync(It.IsAny<Guid>()))
            .ReturnsAsync(MethodResponse<IEnumerable<TEntity>>.Success(entities.ToList()));
        return mock;
    }
}

public class AllocationRuleValidatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AddressId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid Mari = Guid.NewGuid();
    private static readonly Guid Jaan = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private static AllocationRuleValidator CreateValidator(params AllocationRule[] existingRules)
    {
        var addresses = RepositoryMocks.Returning<IAddressRepository, Address>([new Address { Id = AddressId, Name = "Tehnika", FullAddress = "Tehnika 1" }]);
        var services = RepositoryMocks.Returning<IServiceRepository, Service>([new Service { Id = ServiceId, Name = "Internet" }]);
        var contacts = RepositoryMocks.Returning<IContactRepository, Contact>(
            new[] { Mari, Jaan, Stranger }.Select(id => new Contact { Id = id, FullName = id.ToString(), Email = "x@example.com" }));
        var residencies = RepositoryMocks.Returning<IAddressContactRepository, AddressContact>(
        [
            new AddressContact { AddressId = AddressId, ContactId = Mari, StartDate = new DateOnly(2025, 1, 1) },
            new AddressContact { AddressId = AddressId, ContactId = Jaan, StartDate = new DateOnly(2025, 1, 1) }
        ]);
        var rules = new Mock<IAllocationRuleRepository>();
        rules.Setup(repository => repository.GetForUserWithSharesAsync(UserId)).ReturnsAsync(existingRules);

        return new AllocationRuleValidator(addresses.Object, services.Object, contacts.Object, residencies.Object, rules.Object);
    }

    private static AllocationRule Rule(EAllocationMethod method, Guid? serviceId = null, DateOnly? from = null, params (Guid ContactId, decimal Value)[] shares) => new()
    {
        Id = Guid.NewGuid(),
        AddressId = AddressId,
        ServiceId = serviceId,
        Method = method,
        ValidFrom = from ?? new DateOnly(2026, 1, 1),
        Shares = shares.Select(share => new AllocationRuleShare { ContactId = share.ContactId, Value = share.Value }).ToList()
    };

    private static async Task<IReadOnlyList<AllocationRuleError>> Validate(AllocationRule rule, params AllocationRule[] existing) =>
        await CreateValidator(existing).ValidateAsync(rule, UserId);

    [Fact]
    public async Task ValidPercentageRule_HasNoErrors()
    {
        Assert.Empty(await Validate(Rule(EAllocationMethod.Percentage, shares: [(Mari, 60m), (Jaan, 40m)])));
    }

    [Fact]
    public async Task Percentages_NotAddingUpTo100_AreRejected()
    {
        var error = Assert.Single(await Validate(Rule(EAllocationMethod.Percentage, shares: [(Mari, 60m), (Jaan, 30m)])));

        Assert.Equal(EAllocationRuleErrorKind.PercentageTotalNot100, error.Kind);
        Assert.Equal(90m, error.Amount);
        Assert.Null(error.Field);
    }

    [Fact]
    public async Task OverlappingRule_ForSameScope_IsRejected()
    {
        var existing = Rule(EAllocationMethod.Equal);

        var errors = await Validate(Rule(EAllocationMethod.Equal, from: new DateOnly(2026, 6, 1)), existing);

        var error = Assert.Single(errors);
        Assert.Equal(EAllocationRuleErrorKind.OverlappingRule, error.Kind);
        Assert.Equal(existing.Id, error.ConflictingRuleId);
    }

    [Fact]
    public async Task ServiceRule_MayOverlapAddressDefault()
    {
        Assert.Empty(await Validate(Rule(EAllocationMethod.Equal, ServiceId), Rule(EAllocationMethod.Equal)));
    }

    [Fact]
    public async Task EditingARule_DoesNotConflictWithItself()
    {
        var existing = Rule(EAllocationMethod.Equal);

        Assert.Empty(await Validate(existing, existing));
    }

    [Fact]
    public async Task ContactWhoNeverLivedThere_IsRejectedOnTheRow()
    {
        var error = Assert.Single(await Validate(Rule(EAllocationMethod.Fixed, shares: [(Mari, 10m), (Stranger, 5m)])));

        Assert.Equal(EAllocationRuleErrorKind.ContactNotResident, error.Kind);
        Assert.Equal("Shares[1].ContactId", error.Field);
    }

    [Fact]
    public async Task ManualMethod_IsRejected()
    {
        Assert.Contains(await Validate(Rule(EAllocationMethod.Manual)), error => error.Kind == EAllocationRuleErrorKind.ManualNotAllowed);
    }

    [Fact]
    public async Task DuplicateContact_IsRejected()
    {
        Assert.Contains(await Validate(Rule(EAllocationMethod.Fixed, shares: [(Mari, 10m), (Mari, 5m)])), error => error.Kind == EAllocationRuleErrorKind.DuplicateContact);
    }

    [Fact]
    public async Task EndBeforeStart_IsRejected()
    {
        var rule = Rule(EAllocationMethod.Equal);
        rule.ValidTo = rule.ValidFrom.AddDays(-1);

        Assert.Contains(await Validate(rule), error => error.Kind == EAllocationRuleErrorKind.ValidToBeforeValidFrom && error.Field == "ValidTo");
    }

    [Fact]
    public async Task FixedRule_WithoutShares_IsRejected()
    {
        Assert.Contains(await Validate(Rule(EAllocationMethod.Fixed)), error => error.Kind == EAllocationRuleErrorKind.SharesRequired);
    }
}

public class GenerateMonthlyStatementServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AddressId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid Mari = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Jaan = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static (GenerateMonthlyStatementService Service, List<MonthlyStatementLine> Lines) Create(
        IEnumerable<Invoice> invoices,
        IEnumerable<AddressContact> residents,
        IReadOnlyList<AllocationRule>? rules = null)
    {
        var lines = new List<MonthlyStatementLine>();
        var lineRepository = new Mock<IMonthlyStatementLineRepository>();
        lineRepository.Setup(repository => repository.CreateAsync(It.IsAny<MonthlyStatementLine>(), It.IsAny<Guid>()))
            .Callback<MonthlyStatementLine, Guid>((line, _) => lines.Add(line))
            .ReturnsAsync((MonthlyStatementLine line, Guid _) => MethodResponse<MonthlyStatementLine>.Success(line));
        var statementRepository = RepositoryMocks.Returning<IMonthlyStatementRepository, MonthlyStatement>([]);
        statementRepository.Setup(repository => repository.CreateAsync(It.IsAny<MonthlyStatement>(), It.IsAny<Guid>()))
            .ReturnsAsync((MonthlyStatement statement, Guid _) => MethodResponse<MonthlyStatement>.Success(statement));
        var ruleRepository = new Mock<IAllocationRuleRepository>();
        ruleRepository.Setup(repository => repository.GetForUserWithSharesAsync(UserId)).ReturnsAsync(rules ?? []);
        var allocationRepository = new Mock<IInvoiceAllocationRepository>();
        allocationRepository.Setup(repository => repository.GetOverridesByInvoiceIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), UserId)).ReturnsAsync([]);

        var service = new GenerateMonthlyStatementService(
            RepositoryMocks.Returning<IInvoiceRepository, Invoice>(invoices).Object,
            RepositoryMocks.Returning<IAddressRepository, Address>([new Address { Id = AddressId, Name = "Tehnika", FullAddress = "Tehnika 1" }]).Object,
            RepositoryMocks.Returning<IServiceRepository, Service>([new Service { Id = ServiceId, Name = "Electricity" }]).Object,
            RepositoryMocks.Returning<IAddressContactRepository, AddressContact>(residents).Object,
            statementRepository.Object,
            lineRepository.Object,
            RepositoryMocks.Returning<IContactRepository, Contact>([]).Object,
            ruleRepository.Object,
            allocationRepository.Object,
            InvoiceSplitCalculatorTests.CreateCalculator(),
            new Mock<IBaseUow>().Object);
        return (service, lines);
    }

    private static Invoice Invoice(decimal total) => new()
    {
        Id = Guid.NewGuid(), AddressId = AddressId, ServiceId = ServiceId, InvoiceDate = new DateOnly(2026, 3, 10), TotalSum = total
    };

    private static AddressContact Resident(Guid contactId) => new() { AddressId = AddressId, ContactId = contactId, StartDate = new DateOnly(2025, 1, 1) };

    [Fact]
    public async Task WithoutRules_SplitsEquallyAndRecordsEqualMethod()
    {
        var (service, lines) = Create([Invoice(100m)], [Resident(Mari), Resident(Jaan)]);

        var result = await service.GenerateAsync(2026, 3, UserId);

        Assert.Equal(2, result.Statements.Count);
        Assert.Empty(result.Warnings);
        Assert.All(lines, line =>
        {
            Assert.Equal(50m, line.AllocatedAmount);
            Assert.Equal(EAllocationMethod.Equal, line.AllocationMethod);
            Assert.Null(line.AllocationRuleId);
            Assert.Equal(2, line.ResidentCount);
        });
    }

    [Fact]
    public async Task WithPercentageRule_SplitsByRuleAndRecordsSnapshot()
    {
        var rule = new AllocationRule
        {
            Id = Guid.NewGuid(), AddressId = AddressId, Method = EAllocationMethod.Percentage, ValidFrom = new DateOnly(2026, 1, 1),
            Shares = [new AllocationRuleShare { ContactId = Mari, Value = 60m }, new AllocationRuleShare { ContactId = Jaan, Value = 40m }]
        };
        var (service, lines) = Create([Invoice(50m)], [Resident(Mari), Resident(Jaan)], [rule]);

        var result = await service.GenerateAsync(2026, 3, UserId);

        var mariLine = lines.Single(line => line.Id != Guid.Empty && result.Statements.Single(s => s.ContactId == Mari).Id == line.MonthlyStatementId);
        Assert.Equal(30m, mariLine.AllocatedAmount);
        Assert.Equal(EAllocationMethod.Percentage, mariLine.AllocationMethod);
        Assert.Equal(rule.Id, mariLine.AllocationRuleId);
        Assert.Equal(60m, mariLine.ShareValue);
        Assert.Equal(30m, result.Statements.Single(s => s.ContactId == Mari).TotalSum);
        Assert.Equal(20m, result.Statements.Single(s => s.ContactId == Jaan).TotalSum);
    }

    [Fact]
    public async Task InvoiceWithoutResidents_IsReportedWithItsDetails()
    {
        var invoice = Invoice(100m);
        var (service, lines) = Create([invoice], []);

        var result = await service.GenerateAsync(2026, 3, UserId);

        Assert.Empty(lines);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(EAllocationWarningKind.NoActiveResidents, warning.Kind);
        Assert.Equal(invoice.Id, warning.InvoiceId);
        Assert.Equal("Tehnika", warning.AddressName);
        Assert.Equal("Electricity", warning.ServiceName);
        Assert.Equal(invoice.InvoiceDate, warning.InvoiceDate);
    }
}

public class AllocationRuleMappingTests
{
    [Fact]
    public void WebDtoMapper_CarriesRuleNavigations()
    {
        var shareEntityMapper = new AllocationRuleShareEntityMapper();
        var mapper = new WebDtoMapper(
            new AddressEntityMapper(), new AddressDtoMapper(),
            new AddressContactEntityMapper(), new AddressContactDtoMapper(),
            new ContactEntityMapper(), new ContactDtoMapper(),
            new InvoiceEntityMapper(), new InvoiceDtoMapper(),
            new InvoiceAllocationEntityMapper(), new InvoiceAllocationDtoMapper(),
            new MonthlyStatementEntityMapper(), new MonthlyStatementDtoMapper(),
            new MonthlyStatementLineEntityMapper(), new MonthlyStatementLineDtoMapper(),
            new ServiceEntityMapper(), new ServiceDtoMapper(),
            new AllocationRuleEntityMapper(shareEntityMapper), new AllocationRuleDtoMapper(new AllocationRuleShareDtoMapper()));

        var contact = new ContactEntity { Id = Guid.NewGuid(), FullName = "Mari", Email = "mari@example.com" };
        var entity = new AllocationRuleEntity
        {
            Id = Guid.NewGuid(),
            AddressId = Guid.NewGuid(),
            Address = new AddressEntity { Name = "Tehnika", FullAddress = "Tehnika 1" },
            ServiceId = Guid.NewGuid(),
            Service = new ServiceEntity { Name = "Internet" },
            Method = EAllocationMethod.Percentage,
            ValidFrom = new DateOnly(2026, 11, 1),
            ValidTo = new DateOnly(2027, 1, 31),
            Shares = [new AllocationRuleShareEntity { Id = Guid.NewGuid(), ContactId = contact.Id, Contact = contact, Value = 60m }]
        };

        var dto = mapper.ToDto(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(EAllocationMethod.Percentage, dto.Method);
        Assert.Equal(entity.ValidFrom, dto.ValidFrom);
        Assert.Equal(entity.ValidTo, dto.ValidTo);
        Assert.Equal("Tehnika", dto.Address.Name);
        Assert.Equal("Internet", dto.Service!.Name);
        var share = Assert.Single(dto.Shares);
        Assert.Equal(60m, share.Value);
        Assert.Equal("Mari", share.Contact!.FullName);

        var back = mapper.ToEntity(dto);
        Assert.Equal(entity.ServiceId, back.ServiceId);
        Assert.Equal(contact.Id, Assert.Single(back.Shares).ContactId);
    }
}
