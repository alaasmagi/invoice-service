using Application;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using Moq;
using Xunit;

namespace Tests;

public class ParsedInvoiceImporterTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DocumentId = Guid.NewGuid();

    private static readonly Service Telia = new() { Id = Guid.NewGuid(), UserId = UserId, Name = "Telia IKT teenused" };

    private static readonly Address Tehnika = new()
    {
        Id = Guid.NewGuid(), UserId = UserId, Name = "Tallinn - Tehnika", FullAddress = "Tehnika 12-3, Tallinn"
    };

    private static readonly Address Leesi = new()
    {
        Id = Guid.NewGuid(), UserId = UserId, Name = "Leesi - Tiiu", FullAddress = "Tiiu tee 5, Leesi"
    };

    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<IAddressRepository> _addresses = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<ICreateInvoiceService> _createInvoice = new();
    private readonly List<Invoice> _created = [];

    public ParsedInvoiceImporterTests()
    {
        _services.Setup(r => r.GetAllForUserAsync(UserId)).ReturnsAsync([Telia]);
        _addresses.Setup(r => r.GetAllForUserAsync(UserId)).ReturnsAsync([Tehnika, Leesi]);
        _createInvoice
            .Setup(s => s.CreateAsync(It.IsAny<Invoice>(), UserId))
            .ReturnsAsync((Invoice invoice, Guid _) =>
            {
                _created.Add(invoice);
                return invoice;
            });
    }

    private ParsedInvoiceImporter CreateSut() =>
        new(_services.Object, _addresses.Object, _invoices.Object, _createInvoice.Object);

    private static PdfInvoiceParsedDocument Parsed(string serviceName, params PdfInvoiceParsedRow[] rows)
    {
        var document = new PdfInvoiceParsedDocument
        {
            ServiceName = serviceName,
            InvoiceDate = new DateOnly(2026, 11, 5),
            PeriodStart = new DateOnly(2026, 10, 1),
            PeriodEnd = new DateOnly(2026, 10, 31)
        };
        document.Rows.AddRange(rows);
        return document;
    }

    private static PdfInvoiceParsedRow Row(string? addressText, decimal? amount = 40m) =>
        new() { AddressText = addressText!, Amount = amount };

    [Theory]
    [InlineData("Tallinn - Tehnika")]
    [InlineData("Tehnika 12-3, Tallinn")]
    [InlineData("Tehnika")]
    public async Task MatchedRow_CreatesInvoiceLinkedToDocument(string addressText)
    {
        var rows = await CreateSut().ImportAsync(Parsed("Telia", Row(addressText)), UserId, DocumentId);

        var row = Assert.Single(rows);
        var invoice = Assert.Single(_created);
        Assert.Equal(EInvoiceDocumentRowOutcome.Created, row.Outcome);
        Assert.Equal(invoice.Id, row.InvoiceId);
        Assert.Equal(Tehnika.Id, invoice.AddressId);
        Assert.Equal(Telia.Id, invoice.ServiceId);
        Assert.Equal(DocumentId, invoice.InvoiceDocumentId);
        Assert.Equal(40m, invoice.TotalSum);
        Assert.Equal(new DateOnly(2026, 11, 5), invoice.InvoiceDate);
    }

    [Fact]
    public async Task RowDates_OverrideDocumentDates()
    {
        var row = Row("Tehnika");
        var parsed = Parsed("Telia", new PdfInvoiceParsedRow
        {
            AddressText = row.AddressText, Amount = row.Amount, PeriodStart = new DateOnly(2026, 9, 15)
        });

        await CreateSut().ImportAsync(parsed, UserId, DocumentId);

        Assert.Equal(new DateOnly(2026, 9, 15), _created[0].PeriodStart);
        Assert.Equal(new DateOnly(2026, 10, 31), _created[0].PeriodEnd);
    }

    [Fact]
    public async Task UnknownService_LeavesEveryRowUnmatched_AndCreatesNothing()
    {
        var rows = await CreateSut().ImportAsync(Parsed("Enefit", Row("Tehnika"), Row("Leesi")), UserId, DocumentId);

        Assert.All(rows, row => Assert.Equal(EInvoiceDocumentRowOutcome.Unmatched, row.Outcome));
        Assert.Contains("Enefit", rows[0].Reason);
        Assert.Empty(_created);
    }

    [Fact]
    public async Task UnknownAddress_IsUnmatched_WhileOtherRowsAreCreated()
    {
        var rows = await CreateSut().ImportAsync(Parsed("Telia", Row("Tehnika"), Row("Mustamäe")), UserId, DocumentId);

        Assert.Equal(EInvoiceDocumentRowOutcome.Created, rows[0].Outcome);
        Assert.Equal(EInvoiceDocumentRowOutcome.Unmatched, rows[1].Outcome);
        Assert.Equal("No matching address was found.", rows[1].Reason);
        Assert.Single(_created);
    }

    [Fact]
    public async Task AmbiguousAddress_IsUnmatched()
    {
        _addresses.Setup(r => r.GetAllForUserAsync(UserId)).ReturnsAsync(
        [
            new Address { Id = Guid.NewGuid(), UserId = UserId, Name = "Tehnika A", FullAddress = "Tehnika 1" },
            new Address { Id = Guid.NewGuid(), UserId = UserId, Name = "Tehnika B", FullAddress = "Tehnika 2" }
        ]);

        var rows = await CreateSut().ImportAsync(Parsed("Telia", Row("Tehnika")), UserId, DocumentId);

        Assert.Equal(EInvoiceDocumentRowOutcome.Unmatched, rows[0].Outcome);
        Assert.Equal("Multiple matching addresses were found.", rows[0].Reason);
    }

    [Fact]
    public async Task OnlyTheUsersRecordsAreConsidered()
    {
        var otherUser = Guid.NewGuid();
        _services.Setup(r => r.GetAllForUserAsync(otherUser)).ReturnsAsync([]);
        _addresses.Setup(r => r.GetAllForUserAsync(otherUser)).ReturnsAsync([]);

        var rows = await CreateSut().ImportAsync(Parsed("Telia", Row("Tehnika")), otherUser, DocumentId);

        Assert.Equal(EInvoiceDocumentRowOutcome.Unmatched, rows[0].Outcome);
        _services.Verify(r => r.GetAllForUserAsync(UserId), Times.Never);
        Assert.Empty(_created);
    }

    [Fact]
    public async Task EquivalentExistingInvoice_IsDuplicate()
    {
        _invoices
            .Setup(r => r.ExistsEquivalentAsync(UserId, Telia.Id, Tehnika.Id, new DateOnly(2026, 11, 5),
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), 40m))
            .ReturnsAsync(true);

        var rows = await CreateSut().ImportAsync(Parsed("Telia", Row("Tehnika"), Row("Leesi")), UserId, DocumentId);

        Assert.Equal(EInvoiceDocumentRowOutcome.Duplicate, rows[0].Outcome);
        Assert.Equal(EInvoiceDocumentRowOutcome.Created, rows[1].Outcome);
        Assert.Single(_created);
    }

    [Theory]
    [InlineData(null, 40.0)]
    [InlineData("Tehnika", null)]
    public async Task RowWithMissingData_IsInvalid(string? addressText, double? amount)
    {
        var rows = await CreateSut().ImportAsync(
            Parsed("Telia", Row(addressText, amount == null ? null : (decimal)amount.Value)), UserId, DocumentId);

        Assert.Equal(EInvoiceDocumentRowOutcome.Invalid, rows[0].Outcome);
        Assert.Empty(_created);
    }

    [Fact]
    public async Task MissingInvoiceDate_IsInvalid()
    {
        var parsed = new PdfInvoiceParsedDocument { ServiceName = "Telia" };
        parsed.Rows.Add(Row("Tehnika"));

        var rows = await CreateSut().ImportAsync(parsed, UserId, DocumentId);

        Assert.Equal(EInvoiceDocumentRowOutcome.Invalid, rows[0].Outcome);
    }

    [Fact]
    public async Task Rows_KeepParsedOrderAndDocumentLink()
    {
        var rows = await CreateSut().ImportAsync(
            Parsed("Telia", Row("Tehnika"), Row("Mustamäe"), Row("Leesi")), UserId, DocumentId);

        Assert.Equal([1, 2, 3], rows.Select(row => row.Position));
        Assert.All(rows, row => Assert.Equal(DocumentId, row.InvoiceDocumentId));
        Assert.Equal(["Tehnika", "Mustamäe", "Leesi"], rows.Select(row => row.AddressText));
    }
}
