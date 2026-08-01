using Application;
using Base.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public sealed class SendPeriodInvoiceEmailsServiceTests
{
    [Fact]
    public async Task SendPeriod_RunTwiceForSamePeriod_PublishesEachRecipientOnce()
    {
        var userId = Guid.NewGuid();
        var statement = new MonthlyStatement
        {
            Id = Guid.NewGuid(),
            ContactId = Guid.NewGuid(),
            Year = 2026,
            Month = 6,
            TotalSum = 97.87m,
            Status = EMonthlyStatementStatus.ReadyToSend
        };
        var contact = new Contact { Id = statement.ContactId, FullName = "Recipient", Email = "user@example.com" };
        var line = new MonthlyStatementLine
        {
            Id = Guid.NewGuid(),
            MonthlyStatementId = statement.Id,
            AddressName = "Leesi - Tiiu",
            ServiceName = "Enefit elektriteenus",
            InvoiceDate = new DateOnly(2026, 5, 1),
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 31),
            InvoiceTotal = 97.87m,
            ResidentCount = 1,
            AllocatedAmount = 97.87m
        };

        var statementRepo = new Mock<IMonthlyStatementRepository>();
        statementRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ReturnsAsync(MethodResponse<IEnumerable<MonthlyStatement>>.Success([statement]));
        statementRepo.Setup(r => r.GetByIdAsync(statement.Id, It.IsAny<Guid>()))
            .ReturnsAsync(MethodResponse<MonthlyStatement>.Success(statement));
        statementRepo.Setup(r => r.SetSendStatusAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<EMonthlyStatementStatus>(), It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        var lineRepo = new Mock<IMonthlyStatementLineRepository>();
        lineRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ReturnsAsync(MethodResponse<IEnumerable<MonthlyStatementLine>>.Success([line]));

        var contactRepo = new Mock<IContactRepository>();
        contactRepo.Setup(r => r.GetByIdAsync(contact.Id, It.IsAny<Guid>()))
            .ReturnsAsync(MethodResponse<Contact>.Success(contact));

        var senderProvider = new Mock<IMonthlyStatementSenderPaymentDetailsProvider>();
        senderProvider.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new MonthlyStatementSenderPaymentDetails
            {
                FullName = "Aleksander Laasmägi",
                Locale = "et",
                BankIban = "EE621010011679940224"
            });

        var publishedRepo = new InMemoryPublishedRepo();
        var publisher = new CountingPublisher();

        var sendService = new SendMonthlyStatementService(
            statementRepo.Object, lineRepo.Object, contactRepo.Object, senderProvider.Object, publishedRepo, publisher);
        var batch = new SendPeriodInvoiceEmailsService(
            statementRepo.Object, sendService, NullLogger<SendPeriodInvoiceEmailsService>.Instance);

        var first = await batch.SendPeriodAsync(2026, 6, userId);
        var second = await batch.SendPeriodAsync(2026, 6, userId);

        // The whole point: a second run for the same period sends nothing.
        Assert.Equal(1, publisher.Calls);
        Assert.Equal(new SendPeriodResult(1, 1, 0, 0), first);
        Assert.Equal(new SendPeriodResult(1, 0, 1, 0), second);
    }

    private sealed class CountingPublisher : IMonthlyInvoiceEmailPublisher
    {
        public int Calls { get; private set; }

        public Task<InvoiceEmailPublishResult> PublishAsync(MonthlyInvoiceEmailContent content, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(InvoiceEmailPublishResult.Ok($"id-{content.Email}"));
        }
    }

    private sealed class InMemoryPublishedRepo : IPublishedInvoiceEmailRepository
    {
        private readonly HashSet<(Guid Statement, Guid User)> _sent = [];

        public Task<bool> ExistsForStatementAsync(Guid monthlyStatementId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_sent.Contains((monthlyStatementId, userId)));

        public Task AddAsync(PublishedInvoiceEmail record, CancellationToken ct = default)
        {
            _sent.Add((record.MonthlyStatementId, record.UserId));
            return Task.CompletedTask;
        }
    }
}
